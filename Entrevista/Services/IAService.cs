using Entrevista.ViewModels;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Entrevista.Services
{
    // ====================================================================
    // MODELOS INTERNOS DE EVALUACIÓN
    // ====================================================================

    /// <summary>
    /// Resultado extendido de la evaluación de una respuesta.
    /// Incluye puntaje, feedback, nivel detectado y categoría de la pregunta.
    /// </summary>
    public class EvaluacionResultado
    {
        /// <summary>Puntaje de 0 a 10.</summary>
        public int Puntaje { get; set; }

        /// <summary>Feedback constructivo sobre la respuesta.</summary>
        public string Feedback { get; set; }

        /// <summary>Nivel detectado: "junior", "mid" o "senior".</summary>
        public string Nivel { get; set; }

        /// <summary>Categoría de la pregunta: "teoria", "practica", "algoritmos", "arquitectura".</summary>
        public string Categoria { get; set; }

        /// <summary>Consejo concreto de cómo mejorar la respuesta.</summary>
        public string Mejora { get; set; }
    }

    // ====================================================================
    // INTERFAZ
    // ====================================================================

    /// <summary>
    /// Contrato del servicio de inteligencia artificial para entrevistas.
    /// </summary>
    public interface IIAService
    {
        /// <summary>
        /// Genera la siguiente pregunta adaptada al historial y puntajes acumulados.
        /// La dificultad se ajusta automáticamente según el rendimiento.
        /// </summary>
        Task<string> GenerarPreguntaAsync(
            string tema,
            string dificultadBase,
            List<MensajeViewModel> historial,
            List<int> puntajesAcumulados);

        /// <summary>
        /// Evalúa la respuesta del candidato. Retorna evaluación extendida.
        /// </summary>
        Task<EvaluacionResultado> EvaluarRespuestaAsync(
            string pregunta,
            string respuesta,
            string dificultadActual);

        /// <summary>Genera el análisis global de la entrevista completa en HTML.</summary>
        Task<string> GenerarResultadoFinalAsync(List<MensajeViewModel> historial);

        /// <summary>Genera el plan de entrenamiento personalizado en HTML.</summary>
        Task<string> GenerarPlanAsync(string analisis);
    }

    // ====================================================================
    // IMPLEMENTACIÓN
    // ====================================================================

    /// <summary>
    /// Implementación del servicio de IA usando la API de Groq.
    ///
    /// CARACTERÍSTICAS PRINCIPALES:
    ///   - Dificultad adaptativa: sube o baja según puntajes del candidato
    ///   - Evaluación extendida: puntaje + nivel + categoría + mejora
    ///   - Fallback automático entre modelos si hay error/rate-limit
    ///   - Prompts estructurados con reglas explícitas para consistencia
    ///
    /// MODELOS (orden de preferencia):
    ///   1. llama-3.3-70b-versatile  → máxima calidad
    ///   2. llama-3.1-8b-instant     → rápido, fallback
    ///
    /// CONFIGURACIÓN requerida en Web.config:
    ///   &lt;add key="GroqApiKey" value="gsk_..." /&gt;
    /// </summary>
    public class IAService : IIAService
    {
        // ────────────────────────────────────────────────────────────
        // Configuración de API
        // ────────────────────────────────────────────────────────────

        private const string API_URL = "https://api.groq.com/openai/v1/chat/completions";

        private static readonly string[] MODELOS = {
            "llama-3.3-70b-versatile",
            "llama-3.1-8b-instant"
        };

        // Temperaturas por tipo de operación
        private const double TEMP_PREGUNTA = 0.80;   // Creatividad en preguntas
        private const double TEMP_EVAL = 0.15;   // Consistencia en evaluación
        private const double TEMP_ANALISIS = 0.40;   // Balance en análisis

        private const int MAX_TOKENS = 1400;

        // ────────────────────────────────────────────────────────────
        // Umbrales de dificultad adaptativa
        // ────────────────────────────────────────────────────────────

        /// <summary>Promedio mínimo para subir a dificultad media.</summary>
        private const double UMBRAL_SUBIR_A_MEDIA = 7.0;

        /// <summary>Promedio mínimo para subir a dificultad alta.</summary>
        private const double UMBRAL_SUBIR_A_ALTA = 8.5;

        /// <summary>Promedio máximo antes de bajar a dificultad baja.</summary>
        private const double UMBRAL_BAJAR_A_BAJA = 3.5;

        /// <summary>Promedio máximo antes de bajar de alta a media.</summary>
        private const double UMBRAL_BAJAR_A_MEDIA = 5.0;

        // ────────────────────────────────────────────────────────────
        // HttpClient estático (reutilizar entre requests)
        // ────────────────────────────────────────────────────────────

        private static readonly HttpClient _http = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(45)
        };

        private readonly string _apiKey;

        // ────────────────────────────────────────────────────────────
        // Constructor
        // ────────────────────────────────────────────────────────────

        public IAService()
        {
            _apiKey = ConfigurationManager.AppSettings["GroqApiKey"];

            if (string.IsNullOrWhiteSpace(_apiKey))
                throw new InvalidOperationException(
                    "Falta GroqApiKey en Web.config → <add key=\"GroqApiKey\" value=\"gsk_...\" />"
                );
        }

        // ====================================================================
        // GENERAR PREGUNTA ADAPTATIVA
        // ====================================================================

        /// <summary>
        /// Genera la siguiente pregunta técnica.
        ///
        /// DIFICULTAD ADAPTATIVA:
        ///   La dificultad real enviada a la IA puede diferir de la dificultadBase
        ///   elegida por el usuario, ajustándose según los puntajes acumulados:
        ///
        ///   dificultadBase = "Alta" + promedio < 3.5 → se baja a "Baja"
        ///   dificultadBase = "Baja" + promedio > 8.5 → se sube a "Alta"
        ///   etc.
        ///
        ///   Esto garantiza que la entrevista siempre sea retadora pero alcanzable.
        /// </summary>
        public async Task<string> GenerarPreguntaAsync(
            string tema,
            string dificultadBase,
            List<MensajeViewModel> historial,
            List<int> puntajesAcumulados)
        {
            // Calcular dificultad real según rendimiento
            string dificultadAdaptada = AdaptarDificultad(dificultadBase, puntajesAcumulados);

            int numeroPregunta = historial.Count(m => m.EsDeIA) + 1;

            // Construir mensaje del sistema con contexto completo
            var mensajes = new List<object>
            {
                new {
                    role    = "system",
                    content = ConstruirSystemPregunta(
                        tema,
                        dificultadBase,
                        dificultadAdaptada,
                        puntajesAcumulados,
                        numeroPregunta
                    )
                }
            };

            // Agregar historial reciente (máximo 10 mensajes para no exceder tokens)
            foreach (var msg in historial.Skip(Math.Max(0, historial.Count - 10)))
            {
                mensajes.Add(new
                {
                    role = msg.EsDeIA ? "assistant" : "user",
                    content = msg.Texto
                });
            }

            mensajes.Add(new { role = "user", content = "Genera la siguiente pregunta técnica." });

            return await LlamarIA(mensajes, TEMP_PREGUNTA);
        }

        // ====================================================================
        // EVALUAR RESPUESTA EXTENDIDA
        // ====================================================================

        /// <summary>
        /// Evalúa la respuesta del candidato con criterios extendidos.
        ///
        /// RETORNA:
        ///   - puntaje   : 0–10
        ///   - feedback  : evaluación constructiva
        ///   - nivel     : "junior" | "mid" | "senior"
        ///   - categoria : "teoria" | "practica" | "algoritmos" | "arquitectura"
        ///   - mejora    : qué debería decir/agregar para una respuesta perfecta
        /// </summary>
        public async Task<EvaluacionResultado> EvaluarRespuestaAsync(
            string pregunta,
            string respuesta,
            string dificultadActual)
        {
            var mensajes = new object[]
            {
                new {
                    role    = "system",
                    content = ConstruirSystemEvaluacion(dificultadActual)
                },
                new {
                    role    = "user",
                    content = $"Pregunta: {pregunta}\n\nRespuesta del candidato: {respuesta}"
                }
            };

            string resultadoIA = await LlamarIA(mensajes, TEMP_EVAL);
            return ParsearEvaluacionExtendida(resultadoIA);
        }

        // ====================================================================
        // RESULTADO FINAL
        // ====================================================================

        /// <summary>
        /// Analiza la entrevista completa y genera un diagnóstico en HTML.
        /// Incluye nivel, fortalezas, debilidades y decisión de contratación.
        /// </summary>
        public async Task<string> GenerarResultadoFinalAsync(List<MensajeViewModel> historial)
        {
            string conversacion = string.Join("\n\n", historial.Select(m =>
                m.EsDeIA
                    ? $"ENTREVISTADOR: {m.Texto}"
                    : $"CANDIDATO: {m.Texto}"
            ));

            var mensajes = new object[]
            {
                new { role = "system", content = SYSTEM_RESULTADO_FINAL },
                new { role = "user",   content = $"Entrevista completa:\n\n{conversacion}" }
            };

            return await LlamarIA(mensajes, TEMP_ANALISIS);
        }

        // ====================================================================
        // PLAN DE ENTRENAMIENTO
        // ====================================================================

        /// <summary>
        /// Genera un plan de entrenamiento personalizado de 7 días en HTML.
        /// </summary>
        public async Task<string> GenerarPlanAsync(string resultadoFinal)
        {
            var fechaInicio = DateTime.Now.AddDays(1);

            var mensajes = new object[]
            {
        new {
            role = "system",
            content = @"
Eres un coach técnico experto en entrevistas de software.
 
Analiza el feedback de la entrevista e identifica SOLO los temas donde el candidato tuvo debilidades reales.
 
REGLAS CRÍTICAS:
- Si un tema tuvo puntaje >= 7, NO lo incluyas. El candidato ya lo sabe.
- Solo incluye temas con brechas reales de conocimiento.
- Si no hay debilidades (todo >= 7), devuelve dias:[] y un resumen positivo.
- Máximo 10 días de estudio, mínimo 1.
- Ordena los días: primero los temas más críticos (puntaje más bajo).
- Las fechas deben ser consecutivas desde la fecha de inicio, saltando fines de semana.
- Responde ÚNICAMENTE con JSON válido, sin texto adicional, sin bloques markdown, sin explicaciones.
 
ESTRUCTURA JSON EXACTA (no cambies los nombres de los campos):
{
  ""resumen"": ""Descripción breve de qué necesita mejorar y el enfoque del plan"",
  ""dias"": [
    {
      ""fecha"": ""dd/MM/yyyy"",
      ""dia_semana"": ""Lunes"",
      ""titulo"": ""Título corto del día (ej: Fundamentos de SQL)"",
      ""nivel"": ""alto"",
      ""duracion_estimada"": ""2 horas"",
      ""recomendacion"": ""Consejo práctico específico para este día"",
      ""temas"": [
        {
          ""nombre"": ""Nombre del concepto a estudiar"",
          ""dificultad"": ""alta"",
          ""descripcion"": ""Qué debe estudiar y por qué lo necesita mejorar"",
          ""recursos"": [""Recurso o ejercicio recomendado 1"", ""Recurso 2""]
        }
      ]
    }
  ]
}"
        },
        new {
            role = "user",
            content = $"Fecha de inicio del plan: {fechaInicio:dd/MM/yyyy} ({fechaInicio:dddd}, en español)\n\nFeedback de la entrevista:\n{resultadoFinal}"
        }
            };

            return await LlamarIA(mensajes, TEMP_ANALISIS);
        }

        // ====================================================================
        // LÓGICA ADAPTATIVA — métodos públicos de utilidad
        // ====================================================================

        /// <summary>
        /// Calcula la dificultad adaptada según el rendimiento acumulado.
        /// Exportado como método estático para que el controlador pueda
        /// mostrar la dificultad actual en la UI sin llamar a la IA.
        /// </summary>
        public static string AdaptarDificultad(string dificultadBase, List<int> puntajes)
        {
            if (puntajes == null || !puntajes.Any())
                return dificultadBase;

            double promedio = puntajes.Average();

            switch (dificultadBase.ToLower())
            {
                case "baja":
                case "fácil":
                case "facil":
                    if (promedio >= UMBRAL_SUBIR_A_ALTA) return "Alta";
                    if (promedio >= UMBRAL_SUBIR_A_MEDIA) return "Media";
                    return dificultadBase;

                case "media":
                    if (promedio >= UMBRAL_SUBIR_A_ALTA) return "Alta";
                    if (promedio <= UMBRAL_BAJAR_A_BAJA) return "Baja";
                    return dificultadBase;

                case "alta":
                    if (promedio <= UMBRAL_BAJAR_A_BAJA) return "Baja";
                    if (promedio <= UMBRAL_BAJAR_A_MEDIA) return "Media";
                    return dificultadBase;

                default:
                    return dificultadBase;
            }
        }

        /// <summary>
        /// Calcula el nivel técnico estimado según los puntajes acumulados.
        /// </summary>
        public static string CalcularNivelActual(List<int> puntajes)
        {
            if (puntajes == null || !puntajes.Any()) return "Por evaluar";

            double promedio = puntajes.Average();

            if (promedio >= 8.5) return "Senior";
            if (promedio >= 6.5) return "Mid+";
            if (promedio >= 5.0) return "Mid";
            if (promedio >= 3.5) return "Junior+";
            return "Junior";
        }

        // ====================================================================
        // MOTOR CENTRAL — LlamarIA con fallback
        // ====================================================================

        /// <summary>
        /// Ejecuta la llamada HTTP a la API de Groq.
        /// Si el modelo principal falla (rate limit, error de red, etc.),
        /// intenta con el siguiente modelo de la lista.
        /// </summary>
        private async Task<string> LlamarIA(object mensajes, double temperatura)
        {
            foreach (var modelo in MODELOS)
            {
                try
                {
                    var body = new
                    {
                        model = modelo,
                        messages = mensajes,
                        temperature = temperatura,
                        max_tokens = MAX_TOKENS
                    };

                    var json = JsonConvert.SerializeObject(body);
                    var content = new StringContent(json, Encoding.UTF8, "application/json");

                    // Limpiar headers (HttpClient estático puede tener headers previos)
                    _http.DefaultRequestHeaders.Clear();
                    _http.DefaultRequestHeaders.Add("Authorization", $"Bearer {_apiKey}");

                    var response = await _http.PostAsync(API_URL, content);
                    var responseText = await response.Content.ReadAsStringAsync();

                    if (!response.IsSuccessStatusCode)
                    {
                        System.Diagnostics.Debug.WriteLine(
                            $"[IAService] {modelo} → {(int)response.StatusCode}: {responseText}"
                        );
                        continue;
                    }

                    dynamic data = JsonConvert.DeserializeObject(responseText);
                    return data.choices[0].message.content.ToString();
                }
                catch (TaskCanceledException)
                {
                    System.Diagnostics.Debug.WriteLine($"[IAService] Timeout: {modelo}");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[IAService] Error {modelo}: {ex.Message}");
                }
            }

            return "No fue posible conectar con la IA. Intenta de nuevo.";
        }

        // ====================================================================
        // PARSEO DE EVALUACIÓN EXTENDIDA
        // ====================================================================

        /// <summary>
        /// Parsea el JSON extendido de evaluación.
        /// Maneja variaciones de formato que la IA puede retornar.
        /// Si el parseo falla, retorna valores por defecto coherentes.
        /// </summary>
        private static EvaluacionResultado ParsearEvaluacionExtendida(string respuesta)
        {
            try
            {
                // Limpiar markdown y extraer bloque JSON
                var limpio = respuesta
                    .Replace("```json", "")
                    .Replace("```", "")
                    .Trim();

                int ini = limpio.IndexOf('{');
                int fin = limpio.LastIndexOf('}');

                if (ini >= 0 && fin > ini)
                    limpio = limpio.Substring(ini, fin - ini + 1);

                dynamic data = JsonConvert.DeserializeObject(limpio);

                int puntaje = Math.Max(0, Math.Min(10, (int)data.puntaje));

                return new EvaluacionResultado
                {
                    Puntaje = puntaje,
                    Feedback = (string)data.feedback ?? "Evaluación procesada.",
                    Nivel = NormalizarNivel((string)data.nivel),
                    Categoria = NormalizarCategoria((string)data.categoria),
                    Mejora = (string)data.mejora ?? "Amplía tu respuesta con más detalles técnicos."
                };
            }
            catch
            {
                // Fallback: parsear intento simple o usar texto como feedback
                return new EvaluacionResultado
                {
                    Puntaje = 5,
                    Feedback = respuesta.Length > 400
                        ? respuesta.Substring(0, 400) + "..."
                        : respuesta,
                    Nivel = "mid",
                    Categoria = "teoria",
                    Mejora = "Intenta incluir ejemplos concretos y detalles de implementación."
                };
            }
        }

        /// <summary>Normaliza el nivel retornado por la IA a valores válidos del sistema.</summary>
        private static string NormalizarNivel(string nivel)
        {
            if (string.IsNullOrWhiteSpace(nivel)) return "mid";

            switch (nivel.ToLower().Trim())
            {
                case "senior": case "avanzado": case "experto": return "senior";
                case "mid": case "medio": case "intermedio": return "mid";
                default: return "junior";
            }
        }

        /// <summary>Normaliza la categoría retornada por la IA.</summary>
        private static string NormalizarCategoria(string categoria)
        {
            if (string.IsNullOrWhiteSpace(categoria)) return "teoria";

            switch (categoria.ToLower().Trim())
            {
                case "practica": case "práctica": case "codigo": return "practica";
                case "algoritmos": case "algoritmo": case "estructuras": return "algoritmos";
                case "arquitectura": case "diseño": case "sistema": return "arquitectura";
                default: return "teoria";
            }
        }

        // ====================================================================
        // SYSTEM PROMPTS — Builders y constantes
        // ====================================================================

        /// <summary>
        /// Construye el system prompt para generación de preguntas con
        /// contexto de dificultad adaptativa y progreso de la sesión.
        /// </summary>
        private static string ConstruirSystemPregunta(
            string tema,
            string dificultadBase,
            string dificultadAdaptada,
            List<int> puntajes,
            int numeroPregunta)
        {
            string estadoRendimiento = "Sin datos previos.";

            if (puntajes != null && puntajes.Any())
            {
                double prom = puntajes.Average();
                string tendencia = puntajes.Count >= 2
                    ? (puntajes.Last() > puntajes[puntajes.Count - 2] ? "↗ mejorando" : "↘ bajando")
                    : "→ estable";

                estadoRendimiento = $"Promedio: {prom:0.0}/10 | Último puntaje: {puntajes.Last()}/10 | Tendencia: {tendencia}";
            }

            bool dificultadAjustada = !string.Equals(
                dificultadBase, dificultadAdaptada,
                StringComparison.OrdinalIgnoreCase
            );

            return $@"Eres un entrevistador técnico senior con 15 años de experiencia.

CONFIGURACIÓN DE SESIÓN:
- Tema: {tema}
- Dificultad base (elegida por usuario): {dificultadBase}
- Dificultad adaptada (ajustada por rendimiento): {dificultadAdaptada}{(dificultadAjustada ? " ← AJUSTADA AUTOMÁTICAMENTE" : "")}
- Pregunta número: {numeroPregunta} de 5
- Rendimiento actual: {estadoRendimiento}

ESCALA DE DIFICULTAD:
- Baja  → Conceptos fundamentales, definiciones, sintaxis básica, ejemplos simples
- Media → Implementación real, patrones de diseño, comparación de enfoques, casos de uso
- Alta  → Arquitectura de sistemas, optimización, trade-offs, diseño a escala, problemas complejos

INSTRUCCIONES ESTRICTAS:
1. Formula UNA sola pregunta técnica, clara y específica.
2. NO repitas ninguna pregunta que ya aparezca en el historial.
3. NO respondas la pregunta ni des pistas en el enunciado.
4. NO uses introducciones — ve directo a la pregunta.
5. Usa la dificultad ADAPTADA ({dificultadAdaptada}), no la base.
6. Mezcla tipos de pregunta: teórica, práctica, de diseño, de código.
7. Basa las preguntas en escenarios reales de trabajo, no en trivias.
8. La pregunta #{numeroPregunta} debe ser de tipo: {TipoPreguntaPorNumero(numeroPregunta)}.";
        }

        /// <summary>Define el tipo de pregunta recomendado según su número en la sesión.</summary>
        private static string TipoPreguntaPorNumero(int numero)
        {
            switch (numero)
            {
                case 1: return "conceptual/teórica (calentamiento)";
                case 2: return "práctica de implementación";
                case 3: return "escenario real o caso de uso";
                case 4: return "comparación o decisión de diseño";
                case 5: return "desafiante integradora (combina conceptos previos)";
                default: return "técnica según dificultad";
            }
        }

        /// <summary>System prompt de evaluación extendida.</summary>
        private static string ConstruirSystemEvaluacion(string dificultad)
        {
            return $@"Eres un evaluador técnico senior objetivo y justo.

Contexto: dificultad de la entrevista es '{dificultad}'.

Criterios de evaluación:
1. Precisión técnica (¿es correcta la información?)
2. Completitud (¿cubre los aspectos clave?)
3. Claridad de explicación
4. Profundidad apropiada para dificultad '{dificultad}'
5. Uso de ejemplos o casos reales

Escala de puntaje:
  0–3  → Respuesta incorrecta o muy incompleta
  4–5  → Respuesta parcial con errores o lagunas importantes
  6–7  → Respuesta correcta pero superficial
  8–9  → Respuesta completa y bien explicada
  10   → Respuesta excepcional con detalles avanzados

RESPONDE ÚNICAMENTE con este JSON (sin markdown, sin texto fuera del JSON):
{{
  ""puntaje"": <0-10>,
  ""nivel"": ""<junior|mid|senior>"",
  ""categoria"": ""<teoria|practica|algoritmos|arquitectura>"",
  ""feedback"": ""<evaluación constructiva en español, 2-3 oraciones>"",
  ""mejora"": ""<qué debería agregar para una respuesta perfecta, 1-2 oraciones>""
}}";
        }

        private const string SYSTEM_RESULTADO_FINAL = @"Eres un evaluador técnico senior. Analiza la entrevista y genera un diagnóstico completo.

RESPONDE SOLO CON HTML LIMPIO (sin markdown, sin bloques de código):

<h3>🏆 Nivel técnico estimado</h3>
<p><strong>Junior / Mid / Senior</strong> — justificación basada en las respuestas observadas.</p>

<h3>✅ Fortalezas identificadas</h3>
<ul>
  <li>Fortaleza concreta observada en alguna respuesta específica</li>
  <li>Otra fortaleza con ejemplo</li>
</ul>

<h3>⚠️ Áreas de mejora</h3>
<ul>
  <li>Área técnica específica donde el candidato mostró limitaciones</li>
  <li>Otra área con contexto</li>
</ul>

<h3>🎯 Decisión de contratación</h3>
<p><strong>✅ Contratar / ⚠️ Condicional / ❌ No contratar</strong> — razón específica y qué condiciones aplicarían.</p>

<h3>💬 Comentario general</h3>
<p>Resumen ejecutivo de 2-3 oraciones sobre el candidato.</p>";

        private const string SYSTEM_PLAN = @"Eres un coach técnico senior especializado en preparación de entrevistas de software.

Genera un plan de entrenamiento personalizado basado en el diagnóstico.

RESPONDE SOLO CON HTML LIMPIO (sin markdown):

<h3>🔎 Diagnóstico rápido</h3>
<ul>
  <li>Principal brecha técnica identificada</li>
  <li>Segunda área de mejora</li>
</ul>

<h3>📚 Recursos prioritarios</h3>
<ul>
  <li><strong>Tema:</strong> descripción + recurso específico (libro, curso, documentación oficial)</li>
  <li><strong>Tema 2:</strong> descripción + recurso</li>
</ul>

<h3>💻 Ejercicios prácticos</h3>
<ul>
  <li>Ejercicio concreto con plataforma sugerida (LeetCode, HackerRank, Exercism, etc.) y nivel</li>
  <li>Otro ejercicio</li>
</ul>

<h3>🗓️ Plan de 7 días</h3>
<ol>
  <li><strong>Día 1:</strong> actividad concreta y medible (2-3h)</li>
  <li><strong>Día 2:</strong> actividad concreta y medible (2-3h)</li>
  <li><strong>Día 3:</strong> actividad concreta y medible (2-3h)</li>
  <li><strong>Día 4:</strong> actividad concreta y medible (2-3h)</li>
  <li><strong>Día 5:</strong> actividad concreta y medible (2-3h)</li>
  <li><strong>Día 6:</strong> práctica integradora con proyecto pequeño</li>
  <li><strong>Día 7:</strong> simulacro de entrevista completo + revisión</li>
</ol>

<h3>🎯 Consejo clave</h3>
<p>Un consejo específico, motivador y accionable para la próxima entrevista real.</p>";
    }
}