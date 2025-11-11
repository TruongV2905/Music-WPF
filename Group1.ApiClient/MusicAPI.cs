using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Group1.DAL.Dtos;
namespace Group1.ApiClient
{
    public class MusicAPI
    {
        private readonly string _clientId;
        private readonly string _clientSecret;
        private readonly HttpClient _httpClient;

        private string _accessToken;
        private DateTime _expiresAt = DateTime.MinValue;

        public MusicAPI(string clientId, string clientSecret)
        {
            _clientId = clientId;
            _clientSecret = clientSecret;

            _httpClient = new HttpClient
            {
                BaseAddress = new Uri("https://api.spotify.com/v1/")
            };
        }

        // ================= TOKEN ====================
        private async Task<string> GetAccessTokenAsync()
        {
            if (!string.IsNullOrEmpty(_accessToken) && DateTime.UtcNow < _expiresAt)
                return _accessToken;

            var tokenUrl = "https://accounts.spotify.com/api/token";
            var body = new FormUrlEncodedContent(new[]
            {
          new KeyValuePair<string, string>("grant_type", "client_credentials")
      });

            var auth = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_clientId}:{_clientSecret}"));
            var req = new HttpRequestMessage(HttpMethod.Post, tokenUrl) { Content = body };
            req.Headers.Authorization = new AuthenticationHeaderValue("Basic", auth);

            var rsp = await new HttpClient().SendAsync(req);
            rsp.EnsureSuccessStatusCode();

            var json = await rsp.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            _accessToken = root.GetProperty("access_token").GetString();
            var expiresIn = root.GetProperty("expires_in").GetInt32(); // seconds
            _expiresAt = DateTime.UtcNow.AddSeconds(expiresIn - 30); // refresh sớm 30s

            return _accessToken;
        }

        // ================= API CALLS ====================
        public async Task<string> GetNewReleasesAsync(int limit = 10)
        {
            var token = await GetAccessTokenAsync();
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await _httpClient.GetAsync($"browse/new-releases?limit={limit}");
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync();
        }

        public async Task<string> SearchAsync(string query, string type = "track", int limit = 10)
        {
            var token = await GetAccessTokenAsync();
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await _httpClient.GetAsync($"search?q={Uri.EscapeDataString(query)}&type={type}&limit={limit}");
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync();
        }

        // Get detailed track information by ID
        public async Task<string> GetTrackAsync(string trackId)
        {
            var token = await GetAccessTokenAsync();
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await _httpClient.GetAsync($"tracks/{trackId}");
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync();
        }

        // Get audio features for a track
        public async Task<string> GetAudioFeaturesAsync(string trackId)
        {
            var token = await GetAccessTokenAsync();
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await _httpClient.GetAsync($"audio-features/{trackId}");
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync();
        }

        // Get artist information by ID
        public async Task<string> GetArtistAsync(string artistId)
        {
            var token = await GetAccessTokenAsync();
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await _httpClient.GetAsync($"artists/{artistId}");
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync();
        }

        // Get album information by ID
        public async Task<string> GetAlbumAsync(string albumId)
        {
            var token = await GetAccessTokenAsync();
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await _httpClient.GetAsync($"albums/{albumId}");
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync();
        }

        // ================= CUSTOM SEARCH: CHỈ LẤY TRACK CÓ PREVIEW ====================
        public async Task<List<TrackInfo>> SearchTracksWithPreviewAsync(string query, int limit = 20)
        {
            var token = await GetAccessTokenAsync();
            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);

            var response = await _httpClient.GetAsync(
                $"https://api.spotify.com/v1/search?q={Uri.EscapeDataString(query)}&type=track&limit={limit}");

            response.EnsureSuccessStatusCode();

            string json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);

            var results = new List<TrackInfo>();
            var items = doc.RootElement.GetProperty("tracks").GetProperty("items").EnumerateArray();

            foreach (var item in items)
            {
                string previewUrl = item.GetProperty("preview_url").GetString();
                if (string.IsNullOrEmpty(previewUrl)) continue;

                results.Add(new TrackInfo
                {
                    Id = item.GetProperty("id").GetString(),
                    Name = item.GetProperty("name").GetString(),
                    ArtistName = item.GetProperty("artists")[0].GetProperty("name").GetString(),
                    AlbumImageUrl = item.GetProperty("album").GetProperty("images")[0].GetProperty("url").GetString(),
                    PreviewUrl = previewUrl
                });
            }
            return results;
        }



        // hiển thị lyrics
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

                return doc.RootElement.GetProperty("lyrics").GetString();
            }
            catch
            {
                return "❌ Không tìm thấy lời bài hát.";
            }
        }
    }
}
