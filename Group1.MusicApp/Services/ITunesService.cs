// Services/ITunesService.cs
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Group1.MusicApp.Models;

namespace Group1.MusicApp.Services
{
    public class ITunesService
    {
        private readonly HttpClient _http = new();

        public async Task<List<Track>> SearchTracksAsync(string keyword, int limit = 20, int offset = 0, string country = "US")
        {
            // iTunes: không cần API key, dùng được ngay
            string url =
                $"https://itunes.apple.com/search?term={Uri.EscapeDataString(keyword)}&entity=song&limit={limit}&offset={offset}&country={country}";
            var json = await _http.GetStringAsync(url);

            using var doc = JsonDocument.Parse(json);
            var results = new List<Track>();
            if (!doc.RootElement.TryGetProperty("results", out var items)) return results;

            foreach (var item in items.EnumerateArray())
            {
                // Bắt buộc phải có previewUrl mới phát được bằng MediaElement
                if (!item.TryGetProperty("previewUrl", out var previewEl) || previewEl.ValueKind == JsonValueKind.Null) continue;

                string id = item.TryGetProperty("trackId", out var idEl) ? idEl.GetRawText() : Guid.NewGuid().ToString();
                string name = item.TryGetProperty("trackName", out var nameEl) ? nameEl.GetString() ?? "" : "";
                string artist = item.TryGetProperty("artistName", out var artistEl) ? artistEl.GetString() ?? "" : "";
                string album = item.TryGetProperty("collectionName", out var colEl) ? colEl.GetString() ?? "" : "";
                string preview = previewEl.GetString() ?? "";
                int durationMs = item.TryGetProperty("trackTimeMillis", out var durEl) ? durEl.GetInt32() : 0;

                string image = "";
                if (item.TryGetProperty("artworkUrl100", out var artEl))
                {
                    var u = artEl.GetString() ?? "";
                    // Lấy ảnh to hơn: 100x100bb -> 600x600bb
                    image = u.Replace("100x100bb", "600x600bb");
                }

                DateTime? releaseDate = null;
                if (item.TryGetProperty("releaseDate", out var rdEl))
                {
                    if (DateTime.TryParse(rdEl.GetString(), out var dt)) releaseDate = dt;
                }

                bool isExplicit = false;
                if (item.TryGetProperty("trackExplicitness", out var exEl))
                {
                    isExplicit = (exEl.GetString() ?? "").Equals("explicit", StringComparison.OrdinalIgnoreCase);
                }

                results.Add(new Track
                {
                    Id = id.Trim('"'),      // trackId là số -> để nguyên dạng chuỗi
                    Name = name,
                    ArtistName = artist,
                    AlbumName = album,
                    AlbumImageUrl = image,
                    DurationMs = durationMs,
                    PreviewUrl = preview,
                    ReleaseDate = releaseDate,
                    IsExplicit = isExplicit,
                    Popularity = 0, // iTunes không trả popularity
                    Genres = new List<string>(),
                    AudioFeatures = null
                });
            }

            return results;
        }

        public async Task<Track?> GetTrackByIdAsync(string id, string country = "US")
        {
            var url = $"https://itunes.apple.com/lookup?id={Uri.EscapeDataString(id)}&country={country}";
            var json = await _http.GetStringAsync(url);

            using var doc = JsonDocument.Parse(json);
            if (!doc.RootElement.TryGetProperty("results", out var items)) return null;
            if (items.GetArrayLength() == 0) return null;

            var item = items[0];

            if (!item.TryGetProperty("previewUrl", out var previewEl) || previewEl.ValueKind == JsonValueKind.Null) return null;

            string name = item.TryGetProperty("trackName", out var nameEl) ? nameEl.GetString() ?? "" : "";
            string artist = item.TryGetProperty("artistName", out var artistEl) ? artistEl.GetString() ?? "" : "";
            string album = item.TryGetProperty("collectionName", out var colEl) ? colEl.GetString() ?? "" : "";
            string preview = previewEl.GetString() ?? "";
            int durationMs = item.TryGetProperty("trackTimeMillis", out var durEl) ? durEl.GetInt32() : 0;

            string image = "";
            if (item.TryGetProperty("artworkUrl100", out var artEl))
            {
                var u = artEl.GetString() ?? "";
                image = u.Replace("100x100bb", "600x600bb");
            }

            DateTime? releaseDate = null;
            if (item.TryGetProperty("releaseDate", out var rdEl))
            {
                if (DateTime.TryParse(rdEl.GetString(), out var dt)) releaseDate = dt;
            }

            bool isExplicit = false;
            if (item.TryGetProperty("trackExplicitness", out var exEl))
            {
                isExplicit = (exEl.GetString() ?? "").Equals("explicit", StringComparison.OrdinalIgnoreCase);
            }

            return new Track
            {
                Id = id,
                Name = name,
                ArtistName = artist,
                AlbumName = album,
                AlbumImageUrl = image,
                DurationMs = durationMs,
                PreviewUrl = preview,
                ReleaseDate = releaseDate,
                IsExplicit = isExplicit,
                Popularity = 0
            };
        }
    }
}
