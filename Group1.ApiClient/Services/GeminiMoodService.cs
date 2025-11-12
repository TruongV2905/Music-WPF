using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace Group1.MusicApp.Services
{
    public class GeminiMoodService
    {
        private readonly string _apiKey;
        private readonly HttpClient _httpClient;

        public GeminiMoodService(string apiKey)
        {
            _apiKey = apiKey;
            _httpClient = new HttpClient();
        }

        // 🧠 1️⃣ Phân tích mood (trả về nhãn: sad, happy, relax, energetic, neutral)
        public async Task<string> AnalyzeMoodAsync(string userText)
        {
            string endpoint =
                $"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.0-flash:generateContent?key={_apiKey}";

            var requestBody = new
            {
                contents = new[]
                {
                    new
                    {
                        parts = new[]
                        {
                            new
                            {
                                text = $"Người dùng nói: \"{userText}\". " +
                                       "Hãy xác định tâm trạng tổng quát và trả về 1 từ duy nhất trong: sad, happy, relax, energetic, neutral."
                            }
                        }
                    }
                }
            };

            string json = Newtonsoft.Json.JsonConvert.SerializeObject(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var resp = await _httpClient.PostAsync(endpoint, content);
            resp.EnsureSuccessStatusCode();

            var result = JObject.Parse(await resp.Content.ReadAsStringAsync());
            return result["candidates"]?[0]?["content"]?["parts"]?[0]?["text"]?.ToString()?.Trim()?.ToLower() ?? "neutral";
        }

        // 💬 2️⃣ Sinh phản hồi thân mật (AI nói động viên)
        public async Task<string> GenerateResponseAsync(string userPrompt)
        {
            string endpoint =
                $"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.0-flash:generateContent?key={_apiKey}";

            var requestBody = new
            {
                contents = new[]
                {
                    new
                    {
                        parts = new[]
                        {
                            new
                            {
                                text = userPrompt
                            }
                        }
                    }
                }
            };

            string json = Newtonsoft.Json.JsonConvert.SerializeObject(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var resp = await _httpClient.PostAsync(endpoint, content);
            resp.EnsureSuccessStatusCode();

            var result = JObject.Parse(await resp.Content.ReadAsStringAsync());
            return result["candidates"]?[0]?["content"]?["parts"]?[0]?["text"]?.ToString()?.Trim() ?? "(Không có phản hồi)";
        }
    }
}
