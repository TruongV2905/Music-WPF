using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Group1.ApiClient;
using Group1.MusicApp.Models;

namespace Group1.MusicApp.ViewModels
{
    /// <summary>
    /// ViewModel for Track Detail View - Handles data fetching and transformation
    /// </summary>
    public class TrackDetailViewModel
    {
        private readonly MusicAPI _musicApi;

        public TrackDetailViewModel(MusicAPI musicApi)
        {
            _musicApi = musicApi;
        }

        /// <summary>
        /// Get comprehensive track details including audio features and artist info
        /// </summary>
        public async Task<Track> GetTrackDetailsAsync(string trackId)
        {
            try
            {
                // Fetch track info
                var trackJson = await _musicApi.GetTrackAsync(trackId);
                var track = ParseTrackFromJson(trackJson);

                // Fetch audio features
                try
                {
                    var audioFeaturesJson = await _musicApi.GetAudioFeaturesAsync(trackId);
                    track.AudioFeatures = ParseAudioFeatures(audioFeaturesJson);
                }
                catch
                {
                    // Audio features might not be available for all tracks
                    track.AudioFeatures = null;
                }

                // Fetch artist info for genres
                if (!string.IsNullOrEmpty(track.Id))
                {
                    try
                    {
                        var trackData = JsonDocument.Parse(trackJson);
                        var artists = trackData.RootElement.GetProperty("artists");
                        if (artists.GetArrayLength() > 0)
                        {
                            var firstArtistId = artists[0].GetProperty("id").GetString();
                            var artistJson = await _musicApi.GetArtistAsync(firstArtistId);
                            var artistGenres = ParseArtistGenres(artistJson);
                            track.Genres = artistGenres;
                        }
                    }
                    catch
                    {
                        // Genres might not be available
                        track.Genres = new List<string>();
                    }
                }

                return track;
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to fetch track details: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Search for tracks and return simplified list
        /// </summary>
        public async Task<List<Track>> SearchTracksAsync(string query, int limit = 10)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(query))
                {
                    return new List<Track>();
                }

                // Spotify API có giới hạn limit tối đa là 50
                int searchLimit = Math.Min(limit, 50); // Tối đa 50 (giới hạn của Spotify API)
                
                var searchJson = await _musicApi.SearchAsync(query, "track", searchLimit);
                var allTracks = ParseTracksFromSearchJson(searchJson);
                
                // Trả về tất cả tracks đã parse (không giới hạn nữa vì đã lọc ở API level)
                return allTracks;
            }
            catch (Exception ex)
            {
                throw new Exception($"Search failed: {ex.Message}", ex);
            }
        }

        public async Task<List<Track>> SearchTracksAsync(string query, int limit = 10, int offset = 0)
        {
            var searchJson = await _musicApi.SearchAsync(query, "track", limit, offset);
            return ParseTracksFromSearchJson(searchJson);
        }

        // ================== JSON Parsing Methods ==================

        private Track ParseTrackFromJson(string json)
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            var track = new Track
            {
                Id = root.GetProperty("id").GetString(),
                Name = root.GetProperty("name").GetString(),
                DurationMs = root.GetProperty("duration_ms").GetInt32(),
                Popularity = root.GetProperty("popularity").GetInt32(),
                IsExplicit = root.GetProperty("explicit").GetBoolean(),
                PreviewUrl = root.TryGetProperty("preview_url", out var preview) ? preview.GetString() : null
            };

            // Album info
            if (root.TryGetProperty("album", out var album))
            {
                track.AlbumName = album.GetProperty("name").GetString();

                // Release date
                if (album.TryGetProperty("release_date", out var releaseDate))
                {
                    if (DateTime.TryParse(releaseDate.GetString(), out var date))
                        track.ReleaseDate = date;
                }

                // Album image
                if (album.TryGetProperty("images", out var images) && images.GetArrayLength() > 0)
                {
                    track.AlbumImageUrl = images[0].GetProperty("url").GetString();
                }
            }

            // Artist names
            if (root.TryGetProperty("artists", out var artists) && artists.GetArrayLength() > 0)
            {
                var artistNames = new List<string>();
                foreach (var artist in artists.EnumerateArray())
                {
                    artistNames.Add(artist.GetProperty("name").GetString());
                }
                track.ArtistName = string.Join(", ", artistNames);
            }

            return track;
        }

        private List<Track> ParseTracksFromSearchJson(string json)
        {
            var tracks = new List<Track>();

            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            if (root.TryGetProperty("tracks", out var tracksObj) &&
                tracksObj.TryGetProperty("items", out var items))
            {
                foreach (var item in items.EnumerateArray())
                {
                    try
                    {
                        // Lấy preview_url - có thể null
                        string? previewUrl = null;
                        if (item.TryGetProperty("preview_url", out var previewUrlElement) && 
                            previewUrlElement.ValueKind != JsonValueKind.Null)
                        {
                            previewUrl = previewUrlElement.GetString();
                        }

                        // Lấy các thông tin cơ bản
                        if (!item.TryGetProperty("id", out var idElement) || 
                            !item.TryGetProperty("name", out var nameElement))
                        {
                            continue; // Bỏ qua nếu thiếu id hoặc name
                        }

                        var track = new Track
                        {
                            Id = idElement.GetString() ?? string.Empty,
                            Name = nameElement.GetString() ?? "Unknown",
                            Popularity = item.TryGetProperty("popularity", out var pop) ? pop.GetInt32() : 0,
                            DurationMs = item.TryGetProperty("duration_ms", out var dur) ? dur.GetInt32() : 0,
                            PreviewUrl = previewUrl,
                            IsExplicit = item.TryGetProperty("explicit", out var explicitProp) ? explicitProp.GetBoolean() : false
                        };

                        // Album
                        if (item.TryGetProperty("album", out var album))
                        {
                            if (album.TryGetProperty("name", out var albumName))
                            {
                                track.AlbumName = albumName.GetString() ?? string.Empty;
                            }

                            if (album.TryGetProperty("images", out var images) && images.GetArrayLength() > 0)
                            {
                                if (images[0].TryGetProperty("url", out var imageUrl))
                                {
                                    track.AlbumImageUrl = imageUrl.GetString() ?? string.Empty;
                                }
                            }

                            // Release date
                            if (album.TryGetProperty("release_date", out var releaseDate))
                            {
                                var dateStr = releaseDate.GetString();
                                if (!string.IsNullOrEmpty(dateStr) && DateTime.TryParse(dateStr, out var date))
                                {
                                    track.ReleaseDate = date;
                                }
                            }
                        }

                        // Artists
                        if (item.TryGetProperty("artists", out var artists) && artists.GetArrayLength() > 0)
                        {
                            var artistNames = new List<string>();
                            foreach (var artist in artists.EnumerateArray())
                            {
                                if (artist.TryGetProperty("name", out var artistName))
                                {
                                    var name = artistName.GetString();
                                    if (!string.IsNullOrEmpty(name))
                                    {
                                        artistNames.Add(name);
                                    }
                                }
                            }
                            track.ArtistName = artistNames.Count > 0 ? string.Join(", ", artistNames) : "Unknown Artist";
                        }
                        else
                        {
                            track.ArtistName = "Unknown Artist";
                        }

                        // Chỉ thêm track nếu có id hợp lệ
                        if (!string.IsNullOrEmpty(track.Id))
                        {
                            tracks.Add(track);
                        }
                    }
                    catch (Exception ex)
                    {
                        // Bỏ qua track lỗi và tiếp tục
                        System.Diagnostics.Debug.WriteLine($"Error parsing track: {ex.Message}");
                        continue;
                    }
                }
            }

            return tracks;
        }

        private AudioFeatures ParseAudioFeatures(string json)
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            return new AudioFeatures
            {
                Energy = (float)root.GetProperty("energy").GetDouble(),
                Danceability = (float)root.GetProperty("danceability").GetDouble(),
                Valence = (float)root.GetProperty("valence").GetDouble(),
                Acousticness = (float)root.GetProperty("acousticness").GetDouble(),
                Instrumentalness = (float)root.GetProperty("instrumentalness").GetDouble(),
                Speechiness = (float)root.GetProperty("speechiness").GetDouble(),
                Tempo = (float)root.GetProperty("tempo").GetDouble(),
                Key = root.GetProperty("key").GetInt32(),
                Mode = root.GetProperty("mode").GetInt32()
            };
        }

        private List<string> ParseArtistGenres(string json)
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            if (root.TryGetProperty("genres", out var genres))
            {
                return genres.EnumerateArray()
                    .Select(g => CapitalizeGenre(g.GetString()))
                    .ToList();
            }

            return new List<string>();
        }

        private string CapitalizeGenre(string genre)
        {
            if (string.IsNullOrEmpty(genre)) return genre;

            // Convert "pop rock" to "Pop Rock"
            var words = genre.Split(' ', '-', '_');
            var capitalized = words.Select(w =>
                char.ToUpper(w[0]) + w.Substring(1).ToLower()
            );
            return string.Join(" ", capitalized);
        }
    }
}
