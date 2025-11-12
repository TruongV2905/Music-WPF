// FILE: Group1.ApiClient/MusicAPI.cs
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using DotNetEnv;
using Group1.MusicApp.Models;

namespace Group1.ApiClient
{
    public class MusicAPI
    {
        private readonly string _clientId;
        private readonly string _clientSecret;
        private readonly HttpClient _httpClient;

        // App token (client_credentials) chuyên dùng cho search / get track / album / artist / audio-features
        private string _appAccessToken = "";
        private DateTime _appExpiresAt = DateTime.MinValue;

        public MusicAPI(string clientId, string clientSecret)
        {
            _clientId = clientId;
            _clientSecret = clientSecret;

            try { Env.TraversePath().Load(); } catch { /* ignore */ }

            _httpClient = new HttpClient
            {
                BaseAddress = new Uri("https://api.spotify.com/v1/")
            };
        }

        // ==================== TOKEN: APP (client_credentials) ====================
        private async Task<string> GetAppAccessTokenAsync()
        {
            if (!string.IsNullOrEmpty(_appAccessToken) && DateTime.UtcNow < _appExpiresAt)
                return _appAccessToken;

            var tokenUrl = "https://accounts.spotify.com/api/token";
            using var http = new HttpClient();
            var body = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("grant_type", "client_credentials")
            });

            var basic = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_clientId}:{_clientSecret}"));
            var req = new HttpRequestMessage(HttpMethod.Post, tokenUrl) { Content = body };
            req.Headers.Authorization = new AuthenticationHeaderValue("Basic", basic);

            var rsp = await http.SendAsync(req);
            rsp.EnsureSuccessStatusCode();

            var json = await rsp.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            _appAccessToken = root.GetProperty("access_token").GetString() ?? "";
            var expiresIn = root.GetProperty("expires_in").GetInt32();
            _appExpiresAt = DateTime.UtcNow.AddSeconds(expiresIn - 30);

            return _appAccessToken;
        }

        // ==================== API CALLS (dùng APP TOKEN) ====================
        public async Task<string> GetNewReleasesAsync(int limit = 10)
        {
            var token = await GetAppAccessTokenAsync();
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await _httpClient.GetAsync($"browse/new-releases?limit={Math.Min(50, Math.Max(1, limit))}");
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync();
        }

        // Raw search (nếu cần lấy json gốc)
        public async Task<string> SearchRawAsync(string query, string type = "track", int limit = 10, int offset = 0, string? market = null)
        {
            var token = await GetAppAccessTokenAsync();
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            int actualLimit = Math.Min(limit, 50);
            string url = $"search?q={Uri.EscapeDataString(query)}&type={type}&limit={actualLimit}&offset={offset}";
            if (!string.IsNullOrWhiteSpace(market)) url += $"&market={market}";

            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync();
        }

        /// <summary>
        /// Search KHÔNG lọc preview (trả về cả bài không có preview_url) — dùng cho UI mặc định.
        /// </summary>
        public async Task<List<Track>> SearchTracksAsync(string query, int limit = 20, int offset = 0, string? market = "US")
        {
            var token = await GetAppAccessTokenAsync();
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            int actualLimit = Math.Min(50, Math.Max(1, limit));
            string url = $"search?q={Uri.EscapeDataString(query)}&type=track&limit={actualLimit}&offset={offset}";
            if (!string.IsNullOrWhiteSpace(market)) url += $"&market={market}";

            var resp = await _httpClient.GetAsync(url);
            resp.EnsureSuccessStatusCode();

            string json = await resp.Content.ReadAsStringAsync();
            return ParseTracks(json, onlyWithPreview: false);
        }

        /// <summary>
        /// Giữ lại phiên bản chỉ lấy các bài có preview_url (nếu bạn muốn có nút "Only preview").
        /// </summary>
        public async Task<List<Track>> SearchTracksWithPreviewAsync(string query, int limit = 20, int offset = 0, string? market = "US")
        {
            var token = await GetAppAccessTokenAsync();
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            int actualLimit = Math.Min(50, Math.Max(1, limit));
            string url = $"search?q={Uri.EscapeDataString(query)}&type=track&limit={actualLimit}&offset={offset}";
            if (!string.IsNullOrWhiteSpace(market)) url += $"&market={market}";

            var resp = await _httpClient.GetAsync(url);
            resp.EnsureSuccessStatusCode();

            string json = await resp.Content.ReadAsStringAsync();
            return ParseTracks(json, onlyWithPreview: true);
        }

        public async Task<string> GetTrackAsync(string trackId)
        {
            var token = await GetAppAccessTokenAsync();
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await _httpClient.GetAsync($"tracks/{trackId}");
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync();
        }

        public async Task<string> GetAudioFeaturesAsync(string trackId)
        {
            var token = await GetAppAccessTokenAsync();
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await _httpClient.GetAsync($"audio-features/{trackId}");
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync();
        }

        public async Task<string> GetArtistAsync(string artistId)
        {
            var token = await GetAppAccessTokenAsync();
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await _httpClient.GetAsync($"artists/{artistId}");
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync();
        }

        public async Task<string> GetAlbumAsync(string albumId)
        {
            var token = await GetAppAccessTokenAsync();
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await _httpClient.GetAsync($"albums/{albumId}");
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync();
        }

        // ==================== LYRICS (3rd-party; giữ nguyên) ====================
        public async Task<string> GetLyricsAsync(string artist, string title)
        {
            using var client = new HttpClient();
            try
            {
                string url = $"https://api.lyrics.ovh/v1/{Uri.EscapeDataString(artist)}/{Uri.EscapeDataString(title)}";
                var response = await client.GetAsync(url);
                response.EnsureSuccessStatusCode();

                string json = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);

                return doc.RootElement.GetProperty("lyrics").GetString() ?? "❌ Không tìm thấy lời bài hát.";
            }
            catch
            {
                return "❌ Không tìm thấy lời bài hát.";
            }
        }

        // ==================== PARSER ====================
        private static List<Track> ParseTracks(string json, bool onlyWithPreview)
        {
            using var doc = JsonDocument.Parse(json);
            var results = new List<Track>();

            if (!doc.RootElement.TryGetProperty("tracks", out var tracksNode)) return results;
            if (!tracksNode.TryGetProperty("items", out var itemsNode)) return results;

            foreach (var item in itemsNode.EnumerateArray())
            {
                string? previewUrl = null;
                if (item.TryGetProperty("preview_url", out var p) && p.ValueKind != JsonValueKind.Null)
                    previewUrl = p.GetString();

                if (onlyWithPreview && string.IsNullOrWhiteSpace(previewUrl))
                    continue;

                string id = item.TryGetProperty("id", out var idNode) ? (idNode.GetString() ?? "") : "";
                string name = item.TryGetProperty("name", out var nameNode) ? (nameNode.GetString() ?? "") : "";
                string artistName = "";
                try { artistName = item.GetProperty("artists")[0].GetProperty("name").GetString() ?? ""; } catch { }

                string albumName = "";
                string imageUrl = "";
                DateTime? releaseDate = null;
                try
                {
                    var album = item.GetProperty("album");
                    albumName = album.GetProperty("name").GetString() ?? "";
                    if (album.TryGetProperty("images", out var imgs) && imgs.GetArrayLength() > 0)
                    {
                        imageUrl = imgs[0].GetProperty("url").GetString() ?? "";
                    }
                    if (album.TryGetProperty("release_date", out var rd) && rd.ValueKind == JsonValueKind.String)
                    {
                        var s = rd.GetString();
                        if (!string.IsNullOrWhiteSpace(s) && DateTime.TryParse(s, out var dt))
                            releaseDate = dt;
                    }
                }
                catch { }

                int durationMs = 0;
                try { durationMs = item.GetProperty("duration_ms").GetInt32(); } catch { }

                bool isExplicit = false;
                try { isExplicit = item.GetProperty("explicit").GetBoolean(); } catch { }

                int popularity = 0;
                try { popularity = item.GetProperty("popularity").GetInt32(); } catch { }

                results.Add(new Track
                {
                    Id = id,
                    Name = name,
                    ArtistName = artistName,
                    AlbumName = albumName,
                    AlbumImageUrl = imageUrl,
                    DurationMs = durationMs,
                    PreviewUrl = previewUrl ?? "",
                    IsExplicit = isExplicit,
                    Popularity = popularity,
                    ReleaseDate = releaseDate,
                    Genres = new List<string>(),         // không có trong track search; để trống
                    AudioFeatures = null                 // chỉ set nếu bạn gọi GetAudioFeaturesAsync riêng
                });
            }

            return results;
        }
    }
}
