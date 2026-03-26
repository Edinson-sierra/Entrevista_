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
        Task<string> GenerarPreguntaAsync(string tema, List<MensajeViewModel> historial, List<int> puntajes);
        Task<(int puntaje, string feedback, string nivel, string categoria)> EvaluarRespuestaAsync(string pregunta, string respuesta);
        Task<string> GenerarResultadoFinalAsync(List<MensajeViewModel> historial);
        Task<string> GenerarPlanAsync(string resultadoFinal);
        bool EntrevistaTerminada(List<int> puntajes);
    }

    public class IAService : IIAService
    {
        private readonly string API_KEY = ConfigurationManager.AppSettings["GroqApiKey"];
        private const string API_URL = "https://api.groq.com/openai/v1/chat/completions";

        private readonly string[] MODELOS = {
            "llama-3.3-70b-versatile",
            "llama-3.1-8b-instant",
            "openai/gpt-oss-20b"
        };

        private static readonly HttpClient _http = new HttpClient();

        private const double TEMP_PREGUNTA = 0.75;
        private const double TEMP_EVAL = 0.2;
        private const double TEMP_REPORTE = 0.4;

        public IAService()
        {
            if (string.IsNullOrWhiteSpace(API_KEY))
                throw new InvalidOperationException("Configura GroqApiKey en Web.config");
        }

        // =====================================================
        // 🧠 GENERAR PREGUNTA ADAPTATIVA
        // =====================================================
        public async Task<string> GenerarPreguntaAsync(
            string tema,
            List<MensajeViewModel> historial,
            List<int> puntajes)
        {
            string dificultad = CalcularDificultad(puntajes);
            int numero = historial.Count(m => m.Tipo == "IA") + 1;

            var mensajes = new List<object>
            {
                new {
                    role = "system",
                    content = $@"
Eres un entrevistador técnico exigente.

Tema: {tema}
Dificultad: {dificultad}

Reglas:
- Haz UNA sola pregunta
- No repetir preguntas
- Si falla → baja dificultad
- Si acierta → sube dificultad
- Mezcla teoría y práctica
- Escenarios reales

Pregunta #{numero} de 5"
                }
            };

            foreach (var msg in historial.Skip(Math.Max(0, historial.Count - 10)))
            {
                mensajes.Add(new
                {
                    role = msg.Tipo == "IA" ? "assistant" : "user",
                    content = msg.Texto
                });
            }

            mensajes.Add(new { role = "user", content = "Continúa la entrevista." });

            return await LlamarIA(mensajes, TEMP_PREGUNTA);
        }

        // =====================================================
        // 📊 EVALUAR RESPUESTA (PRO)
        // =====================================================
        public async Task<(int puntaje, string feedback, string nivel, string categoria)> EvaluarRespuestaAsync(
            string pregunta, string respuesta)
        {
            var mensajes = new object[]
            {
                new {
                    role = "system",
                    content = @"
Eres un entrevistador técnico senior.

Evalúa la respuesta.

Devuelve SOLO JSON:

{
  ""puntaje"": 0-10,
  ""nivel"": ""junior|mid|senior"",
  ""categoria"": ""teoria|practica|algoritmos|arquitectura"",
  ""feedback"": ""explicación clara"",
  ""mejora"": ""cómo mejorar""
}"
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
                return (puntaje, data.feedback.ToString(), data.nivel.ToString(), data.categoria.ToString());
            }
            catch
            {
                return (5, json, "junior", "teoria");
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
                    content = @"
Eres un entrevistador senior.

Devuelve HTML con:
- Nivel
- Fortalezas
- Debilidades
- Decisión (Contratar / No contratar)
- Justificación"
                },
                new { role = "user", content = texto }
            };

            return await LlamarIA(mensajes, TEMP_REPORTE);
        }

        // =====================================================
        // 🎯 PLAN DE ENTRENAMIENTO
        // =====================================================
        public async Task<string> GenerarPlanAsync(string resultadoFinal)
        {
            var fechaInicio = DateTime.Now.AddDays(1);

            var mensajes = new object[]
            {
                new {
                    role = "system",
                    content = "Genera plan en JSON basado en debilidades."
                },
                new {
                    role = "user",
                    content = $"Inicio: {fechaInicio:dd/MM/yyyy}\n{resultadoFinal}"
                }
            };

            return await LlamarIA(mensajes, TEMP_REPORTE);
        }

        // =====================================================
        // 🧠 DIFICULTAD DINÁMICA
        // =====================================================
        private string CalcularDificultad(List<int> puntajes)
        {
            if (!puntajes.Any()) return "basico";

            double promedio = puntajes.Average();

            if (promedio < 4) return "basico";
            if (promedio < 7) return "intermedio";
            return "avanzado";
        }

        // =====================================================
        // 🧠 CONTROL ENTREVISTA
        // =====================================================
        public bool EntrevistaTerminada(List<int> puntajes)
        {
            if (puntajes.Count >= 5) return true;

            if (puntajes.Count >= 3 && puntajes.Average() < 3)
                return true;

            return false;
        }

        // =====================================================
        // 🚀 CORE IA
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
    }
}