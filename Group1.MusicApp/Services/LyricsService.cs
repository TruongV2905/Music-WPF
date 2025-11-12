// Services/LyricsService.cs
using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace Group1.MusicApp.Services
{
    public class LyricsService
    {
        private readonly HttpClient _http = new();

        public async Task<string> GetLyricsAsync(string artist, string title)
        {
            // Làm sạch chuỗi để API khớp tốt hơn
            string Clean(string s)
            {
                if (string.IsNullOrWhiteSpace(s)) return "";
                var t = s;
                // Bỏ (feat. ...), (Remix) ...
                var idx = t.IndexOf("(", StringComparison.Ordinal);
                if (idx > 0) t = t.Substring(0, idx);
                t = t.Replace("&", "and").Replace(" x ", " ").Trim();
                return t;
            }

            artist = Clean(artist);
            title = Clean(title);

            try
            {
                string url = $"https://api.lyrics.ovh/v1/{Uri.EscapeDataString(artist)}/{Uri.EscapeDataString(title)}";
                string json = await _http.GetStringAsync(url);
                using var doc = JsonDocument.Parse(json);
                return doc.RootElement.GetProperty("lyrics").GetString() ?? "❌ Không tìm thấy lời bài hát.";
            }
            catch
            {
                return "❌ Không tìm thấy lời bài hát.";
            }
        }
    }
}
