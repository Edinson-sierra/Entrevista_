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
    public interface IIAService
    {
        Task<string> GenerarPreguntaAsync(string tema, string dificultad, List<MensajeViewModel> historial);
        Task<(int puntaje, string feedback)> EvaluarRespuestaAsync(string pregunta, string respuesta, string dificultad);
        Task<string> GenerarResultadoFinalAsync(List<MensajeViewModel> historial);
        Task<string> GenerarPlanAsync(string resultadoFinal);
    }

    public class IAService : IIAService
    {
        // 🔐 CONFIGURACIÓN SEGURA
        private readonly string API_KEY = ConfigurationManager.AppSettings["GroqApiKey"];
        private const string API_URL = "https://api.groq.com/openai/v1/chat/completions";

        // ⚡ MODELOS (fallback automático)
        private readonly string[] MODELOS = {
            "llama-3.3-70b-versatile",
            "llama-3.1-8b-instant",
            "openai/gpt-oss-20b"
        };

        private static readonly HttpClient _http = new HttpClient();

        // 🎯 TEMPERATURAS
        private const double TEMP_PREGUNTA = 0.75;
        private const double TEMP_EVAL = 0.2;
        private const double TEMP_REPORTE = 0.4;

        public IAService()
        {
            if (string.IsNullOrWhiteSpace(API_KEY))
                throw new InvalidOperationException("Configura GroqApiKey en Web.config");
        }

        // =====================================================
        // 🧠 GENERAR PREGUNTA (INTELIGENTE)
        // =====================================================
        public async Task<string> GenerarPreguntaAsync(string tema, string dificultad, List<MensajeViewModel> historial)
        {
            string debilidades = ExtraerDebilidades(historial);
            int numero = historial.Count(m => m.Tipo == "IA") + 1;

            var mensajes = new List<object>
            {
                new {
                    role = "system",
                    content = $@"
Eres un entrevistador técnico senior.

Tema: {tema}
Dificultad: {dificultad}

Reglas:
- Solo UNA pregunta
- No repetir historial
- Nivel real de entrevista técnica, diferenciando los niveles correctamente, viendo a junior como basico, medio profesional, senior como un nivel alto 
- Enfócate en debilidades: {debilidades}

Pregunta #{numero} de 5"
                }
            };

            foreach (var msg in historial.Skip(Math.Max(0, historial.Count - 8)))
            {
                mensajes.Add(new
                {
                    role = msg.Tipo == "IA" ? "assistant" : "user",
                    content = msg.Texto
                });
            }

            mensajes.Add(new { role = "user", content = "Haz la siguiente pregunta." });

            return await LlamarIA(mensajes, TEMP_PREGUNTA);
        }

        // =====================================================
        // 📊 EVALUAR RESPUESTA
        // =====================================================
        public async Task<(int puntaje, string feedback)> EvaluarRespuestaAsync(string pregunta, string respuesta, string dificultad)
        {
            var mensajes = new object[]
            {
                new {
                    role = "system",
                    content = $@"
Evalúa esta respuesta técnica.

Devuelve JSON:
{{""puntaje"":0-10,""feedback"":""texto""}}"
                },
                new {
                    role = "user",
                    content = $"Pregunta: {pregunta}\nRespuesta: {respuesta}"
                }
            };

            var json = await LlamarIA(mensajes, TEMP_EVAL);

            try
            {
                var limpio = json.Replace("```json", "").Replace("```", "").Trim();
                dynamic data = JsonConvert.DeserializeObject(limpio);

                int puntaje = Math.Max(0, Math.Min(10, (int)data.puntaje));
                string feedback = data.feedback;

                return (puntaje, feedback);
            }
            catch
            {
                return (5, json);
            }
        }

        // =====================================================
        // 📈 RESULTADO FINAL
        // =====================================================
        public async Task<string> GenerarResultadoFinalAsync(List<MensajeViewModel> historial)
        {
            string texto = string.Join("\n\n", historial.Select(x =>
                x.Tipo == "IA" ? $"ENTREVISTADOR: {x.Texto}" : $"CANDIDATO: {x.Texto}"
            ));

            var mensajes = new object[]
            {
                new {
                    role = "system",
                    content = "Analiza la entrevista y da diagnóstico profesional en HTML."
                },
                new {
                    role = "user",
                    content = texto
                }
            };

            return await LlamarIA(mensajes, TEMP_REPORTE);
        }

        // =====================================================
        // 🎯 PLAN DE ENTRENAMIENTO — Devuelve JSON de calendario
        // Reemplaza el método GenerarPlanAsync en IAService.cs
        // =====================================================
        public async Task<string> GenerarPlanAsync(string resultadoFinal)
        {
            // Calcular fecha de inicio del plan (mañana)
            var fechaInicio = DateTime.Now.AddDays(1);

            var mensajes = new object[]
            {
        new {
            role = "system",
            content = @"
Eres un coach técnico experto. Analiza el resultado de la entrevista y genera un plan
de estudio PERSONALIZADO solo para los conceptos en los que el candidato tuvo bajo desempeño.
 
REGLAS CRÍTICAS:
- Si el candidato respondió correctamente un tema (puntaje >= 7), NO lo incluyas en el plan.
- Solo incluye temas donde haya brechas reales de conocimiento.
- Si no hay brechas (todos los temas >= 7), devuelve dias: [] y resumen explicando que el desempeño fue excelente.
- Máximo 14 días, mínimo 1.
- Los días deben estar ordenados de mayor a menor prioridad (primero lo más crítico).
- Las fechas usan formato dd/MM/yyyy.
- Responde ÚNICAMENTE con el JSON, sin texto adicional, sin bloques markdown.
 
ESTRUCTURA EXACTA del JSON:
{
  ""resumen"": ""Descripción breve de qué se detectó y el enfoque del plan"",
  ""dias"": [
    {
      ""fecha"": ""dd/MM/yyyy"",
      ""titulo"": ""Título corto del día de estudio"",
      ""nivel"": ""alto|medio|bajo"",
      ""duracion_estimada"": ""2-3 horas"",
      ""recomendacion"": ""Consejo específico para este día"",
      ""temas"": [
        {
          ""nombre"": ""Nombre del concepto"",
          ""dificultad"": ""alta|media|baja"",
          ""descripcion"": ""Qué debe estudiar y por qué lo necesita"",
          ""recursos"": [""Recurso 1"", ""Recurso 2""]
        }
      ]
    }
  ]
}"
        },
        new {
            role = "user",
            content = $"Fecha de inicio del plan: {fechaInicio:dd/MM/yyyy}\n\nResultado de la entrevista:\n{resultadoFinal}"
        }
            };

            return await LlamarIA(mensajes, TEMP_REPORTE);
        }

        // =====================================================
        // 🚀 CORE IA (CON FALLBACK)
        // =====================================================
        private async Task<string> LlamarIA(object mensajes, double temp)
        {
            foreach (var modelo in MODELOS)
            {
                try
                {
                    var body = new
                    {
                        model = modelo,
                        messages = mensajes,
                        temperature = temp,
                        max_tokens = 1200
                    };

                    var content = new StringContent(
                        JsonConvert.SerializeObject(body),
                        Encoding.UTF8,
                        "application/json"
                    );

                    _http.DefaultRequestHeaders.Clear();
                    _http.DefaultRequestHeaders.Add("Authorization", $"Bearer {API_KEY}");

                    var response = await _http.PostAsync(API_URL, content);
                    var json = await response.Content.ReadAsStringAsync();

                    if (!response.IsSuccessStatusCode)
                        continue;

                    dynamic data = JsonConvert.DeserializeObject(json);
                    return data.choices[0].message.content.ToString();
                }
                catch { }
            }

            return "❌ Error al conectar con la IA.";
        }

        // =====================================================
        // 🧠 EXTRA: DETECTAR DEBILIDADES
        // =====================================================
        private string ExtraerDebilidades(List<MensajeViewModel> historial)
        {
            return string.Join(", ",
                historial
                .Where(x => x.Tipo == "IA" && x.Texto.Contains("mejorar"))
                .Take(2)
                .Select(x => x.Texto)
            );
        }
    }
}