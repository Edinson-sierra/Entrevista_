using Newtonsoft.Json;
using System;
using System.Configuration;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Entrevista.Services
{
    public interface IIAService
    {
        Task<string> PreguntarAsync(string prompt);
    }

    public class IAService : IIAService
    {
        private readonly string _apiKey = "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";

        public async Task<string> PreguntarAsync(string prompt)
        {
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _apiKey);

                var body = new
                {
                    model = "openai/gpt-oss-20b",
                    messages = new[]
                    {
                    new { role = "user", content = prompt }
                }
                };

                var json = JsonConvert.SerializeObject(body);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await client.PostAsync(
                    "https://api.groq.com/openai/v1/chat/completions",
                    content
                );

                var responseText = await response.Content.ReadAsStringAsync();

                // 🔥 AQUÍ ESTÁ LA CLAVE
                if (!response.IsSuccessStatusCode)
                {
                    throw new Exception($"Error IA REAL: {response.StatusCode} - {responseText}");
                }

                dynamic data = JsonConvert.DeserializeObject(responseText);

                return data.choices[0].message.content.ToString();
            }
        }
    }

}