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
        Task<string> GenerarPlanAsync(string resultado);

    }

    public class IAService : IIAService
    {
        private readonly string _apiKey;
        private readonly string _model = "openai/gpt-oss-20b";
        private readonly string _url = "https://api.groq.com/openai/v1/chat/completions";

        public IAService()
        {
            _apiKey = ConfigurationManager.AppSettings["GroqApiKey"];
        }
        // 🔥 GENERAR PREGUNTA
        public async Task<string> GenerarPreguntaAsync(string tema, string dificultad, List<MensajeViewModel> historial)
        {
            var mensajes = new List<object>();

            mensajes.Add(new
            {
                role = "system",
                content = $@"
                Eres un entrevistador técnico experto en {tema}.

                Dificultad: {dificultad}

                Reglas:
                - Haz UNA sola pregunta
                - No repitas preguntas
                - Adapta la dificultad:
                    Fácil → conceptos básicos
                    Media → implementación
                    Difícil → arquitectura / optimización
                - No des respuestas
                "
            });

            foreach (var h in historial)
            {
                mensajes.Add(new
                {
                    role = h.Tipo == "Usuario" ? "user" : "assistant",
                    content = h.Texto
                });
            }

            return await EjecutarIA(mensajes);
        }

        // 🔥 EVALUAR RESPUESTA
        public async Task<(int puntaje, string feedback)> EvaluarRespuestaAsync(
            string pregunta,
            string respuesta,
            string dificultad)
        {
            var mensajes = new[]
            {
                new {
                    role = "system",
                    content = $@"
                    Evalúa la respuesta de un candidato.

                    Dificultad: {dificultad}

                    Devuelve SOLO JSON:
                    {{ ""puntaje"": 0-10, ""feedback"": ""texto corto"" }}
                    "
                },
                new {
                    role = "user",
                    content = $"Pregunta: {pregunta}\nRespuesta: {respuesta}"
                }
            };

            var result = await EjecutarIA(mensajes);

            try
            {
                dynamic data = JsonConvert.DeserializeObject(result);

                return ((int)data.puntaje, (string)data.feedback);
            }
            catch
            {
                return (5, "No se pudo evaluar correctamente");
            }
        }

        // 🔥 RESULTADO FINAL
        public async Task<string> GenerarResultadoFinalAsync(List<MensajeViewModel> historial)
        {
            var texto = string.Join("\n", historial.Select(x => $"{x.Tipo}: {x.Texto}"));

            var mensajes = new[]
            {
                new {
                    role = "system",
                    content = @"
                    Eres un evaluador técnico senior.

                    Analiza la entrevista completa y devuelve:
                    - Fortalezas
                    - Debilidades
                    - Nivel del candidato
                    "
                },
                new {
                    role = "user",
                    content = texto
                }
            };

            return await EjecutarIA(mensajes);
        }

        // 🔥 PLAN DE ENTRENAMIENTO
        public async Task<string> GenerarPlanAsync(string resultado)
        {
            var mensajes = new[]
            {
                new {
                    role = "system",
                    content = @"
                   Eres un coach técnico senior especializado en entrevistas de software.

                    Con base en estas observaciones:
                    {observaciones}

                    Genera un plan de entrenamiento PROFESIONAL con formato HTML limpio.

                    Estructura obligatoria:

                    <h3>🔎 Diagnóstico</h3>
                    <ul>
                    <li>Debilidad 1</li>
                    <li>Debilidad 2</li>
                    </ul>

                    <h3>📚 Temas a estudiar</h3>
                    <ul>
                    <li>Tema 1</li>
                    <li>Tema 2</li>
                    </ul>

                    <h3>💻 Ejercicios prácticos</h3>
                    <ul>
                    <li>Ejercicio 1</li>
                    <li>Ejercicio 2</li>
                    </ul>

                    <h3>🗓️ Plan de 7 días</h3>
                    <ol>
                    <li>Día 1: ...</li>
                    <li>Día 2: ...</li>
                    </ol>

                    <h3>🎯 Recomendaciones finales</h3>
                    <p>Consejo profesional claro.</p>

                    IMPORTANTE:
                    - No uses markdown
                    - Solo HTML limpio
                    - Claro, corto y profesional
                    "
                },
                new {
                    role = "user",
                    content = resultado
                }
            };

            return await EjecutarIA(mensajes);
        }

        // 🔥 MÉTODO CENTRAL
        private async Task<string> EjecutarIA(object mensajes)
        {
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _apiKey);

                var body = new
                {
                    model = _model,
                    messages = mensajes,
                    temperature = 0.7
                };

                var json = JsonConvert.SerializeObject(body);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await client.PostAsync(_url, content);
                var responseText = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                    throw new Exception($"Error IA: {responseText}");

                dynamic data = JsonConvert.DeserializeObject(responseText);

                return data.choices[0].message.content.ToString();
            }
        }
    }

}