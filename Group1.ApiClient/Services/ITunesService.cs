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

        /// <summary>
        /// Tìm bài hát từ iTunes Search API (entity=song).
        /// </summary>
        public async Task<List<Track>> SearchTracksAsync(string query, int limit = 20, int offset = 0, string country = "US")
        {
            // iTunes hỗ trợ limit & offset
            var url =
                $"https://itunes.apple.com/search?term={Uri.EscapeDataString(query)}&entity=song&country={country}&limit={limit}&offset={offset}";

            using var resp = await _http.GetAsync(url);
            resp.EnsureSuccessStatusCode();
            var json = await resp.Content.ReadAsStringAsync();

            return ParseSearch(json);
        }

        /// <summary>
        /// Lấy track theo trackId (lookup).
        /// </summary>
        public async Task<Track?> GetTrackByIdAsync(string trackId, string country = "US")
        {
            if (string.IsNullOrWhiteSpace(trackId)) return null;

            var url = $"https://itunes.apple.com/lookup?id={Uri.EscapeDataString(trackId)}&country={country}";
            using var resp = await _http.GetAsync(url);
            resp.EnsureSuccessStatusCode();
            var json = await resp.Content.ReadAsStringAsync();

            var list = ParseSearch(json);
            return list.Count > 0 ? list[0] : null;
        }

        /// <summary>
        /// Lấy bài hát theo danh mục (Trendy, Popular, New Releases, etc.)
        /// </summary>
        public async Task<List<Track>> GetTracksByCategoryAsync(string category, int limit = 20)
        {
            string searchQuery = category switch
            {
                "Trendy" => "top hits 2024",
                "Thịnh hành" => "popular songs",
                "Mới phát hành" => "new releases 2024",
                "Pop" => "pop music",
                "Rock" => "rock music",
                "Hip Hop" => "hip hop",
                "EDM" => "edm electronic",
                "R&B" => "r&b soul",
                "Country" => "country music",
                _ => "top songs"
            };

            return await SearchTracksAsync(searchQuery, limit: limit, offset: 0);
        }

        private static List<Track> ParseSearch(string json)
        {
            var list = new List<Track>();
            using var doc = JsonDocument.Parse(json);
            if (!doc.RootElement.TryGetProperty("results", out var results)) return list;

            foreach (var item in results.EnumerateArray())
            {
                try
                {
                    // iTunes fields
                    string id = item.TryGetProperty("trackId", out var tid) ? tid.GetInt64().ToString() : "";
                    string name = item.TryGetProperty("trackName", out var tn) ? (tn.GetString() ?? "") : "";
                    string artist = item.TryGetProperty("artistName", out var an) ? (an.GetString() ?? "") : "";
                    string album = item.TryGetProperty("collectionName", out var cn) ? (cn.GetString() ?? "") : "";
                    string artwork = item.TryGetProperty("artworkUrl100", out var art) ? (art.GetString() ?? "") : "";
                    string preview = item.TryGetProperty("previewUrl", out var pu) ? (pu.GetString() ?? "") : "";
                    int durationMs = item.TryGetProperty("trackTimeMillis", out var tt) ? tt.GetInt32() : 0;

                    // nâng artwork lên 300x300 (hack URL iTunes)
                    if (!string.IsNullOrEmpty(artwork))
                        artwork = artwork.Replace("100x100bb", "300x300bb");

                    if (!string.IsNullOrEmpty(id) && !string.IsNullOrEmpty(name))
                    {
                        list.Add(new Track
                        {
                            Id = id,
                            Name = name,
                            ArtistName = artist,
                            AlbumName = album,
                            AlbumImageUrl = artwork,
                            PreviewUrl = preview,
                            DurationMs = durationMs,
                            Popularity = 0,
                            IsExplicit = false,
                            Genres = new List<string>()
                        });
                    }
                }
                catch
                {
                    // bỏ qua item lỗi
                }
            }

            return list;
        }
    }
}
