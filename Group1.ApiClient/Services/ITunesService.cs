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

        private static List<Track> ParseSearch(string json)
        {
            var list = new List<Track>();
            using var doc = JsonDocument.Parse(json);
            if (!doc.RootElement.TryGetProperty("results", out var results)) return list;

            foreach (var item in results.EnumerateArray())
            {
                try
                {
                    // Basic iTunes fields
                    string id = item.TryGetProperty("trackId", out var tid) ? tid.GetInt64().ToString() : "";
                    string name = item.TryGetProperty("trackName", out var tn) ? (tn.GetString() ?? "") : "";
                    string artist = item.TryGetProperty("artistName", out var an) ? (an.GetString() ?? "") : "";
                    string album = item.TryGetProperty("collectionName", out var cn) ? (cn.GetString() ?? "") : "";
                    string artwork = item.TryGetProperty("artworkUrl100", out var art) ? (art.GetString() ?? "") : "";
                    string preview = item.TryGetProperty("previewUrl", out var pu) ? (pu.GetString() ?? "") : "";
                    int durationMs = item.TryGetProperty("trackTimeMillis", out var tt) ? tt.GetInt32() : 0;

                    // Genre and metadata
                    string genre = item.TryGetProperty("primaryGenreName", out var gn) ? (gn.GetString() ?? "") : "";
                    DateTime? releaseDate = null;
                    if (item.TryGetProperty("releaseDate", out var rd))
                    {
                        var rdStr = rd.GetString();
                        if (!string.IsNullOrEmpty(rdStr) && DateTime.TryParse(rdStr, out var dt))
                            releaseDate = dt;
                    }
                    bool isExplicit = item.TryGetProperty("trackExplicitness", out var te) && te.GetString() == "explicit";

                    // Additional detailed fields
                    string artistId = item.TryGetProperty("artistId", out var aid) ? aid.GetInt64().ToString() : "";
                    string albumId = item.TryGetProperty("collectionId", out var cid) ? cid.GetInt64().ToString() : "";
                    int trackNumber = item.TryGetProperty("trackNumber", out var tnum) ? tnum.GetInt32() : 0;
                    int trackCount = item.TryGetProperty("trackCount", out var tcnt) ? tcnt.GetInt32() : 0;
                    int discNumber = item.TryGetProperty("discNumber", out var dnum) ? dnum.GetInt32() : 1;
                    int discCount = item.TryGetProperty("discCount", out var dcnt) ? dcnt.GetInt32() : 1;
                    string country = item.TryGetProperty("country", out var ctry) ? (ctry.GetString() ?? "") : "";
                    string currency = item.TryGetProperty("currency", out var curr) ? (curr.GetString() ?? "") : "";
                    decimal? price = null;
                    if (item.TryGetProperty("trackPrice", out var tp) && tp.ValueKind == System.Text.Json.JsonValueKind.Number)
                        price = tp.GetDecimal();
                    string advisoryRating = item.TryGetProperty("contentAdvisoryRating", out var car) ? (car.GetString() ?? "") : "";

                    // nâng artwork lên 300x300 (hack URL iTunes)
                    if (!string.IsNullOrEmpty(artwork))
                        artwork = artwork.Replace("100x100bb", "300x300bb");

                    if (!string.IsNullOrEmpty(id) && !string.IsNullOrEmpty(name))
                    {
                        var genres = new List<string>();
                        if (!string.IsNullOrEmpty(genre))
                            genres.Add(genre);

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
                            IsExplicit = isExplicit,
                            ReleaseDate = releaseDate,
                            Genres = genres,
                            ArtistId = artistId,
                            AlbumId = albumId,
                            TrackNumber = trackNumber,
                            TrackCount = trackCount,
                            DiscNumber = discNumber,
                            DiscCount = discCount,
                            Country = country,
                            Currency = currency,
                            Price = price,
                            ContentAdvisoryRating = advisoryRating
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
