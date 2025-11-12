//using System;
//using System.Collections.Generic;
//using System.IO;
//using Microsoft.Data.Sqlite;
//using Group1.MusicApp.Models;

//namespace Group1.MusicApp.Services
//{
//    public class PlaylistService
//    {
//        private readonly string connectionString;

//        public PlaylistService()
//        {
//            string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
//            string appFolder = Path.Combine(appDataPath, "MusicApp");

//            if (!Directory.Exists(appFolder))
//                Directory.CreateDirectory(appFolder);

//            string dbPath = Path.Combine(appFolder, "playlist.db");
//            connectionString = $"Data Source={dbPath}";

//            InitializeDatabase();
//        }

//        private void InitializeDatabase()
//        {
//            using var connection = new SqliteConnection(connectionString);
//            connection.Open();

//            string createTableQuery = @"
//                CREATE TABLE IF NOT EXISTS PlaylistItems (
//                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
//                    TrackId TEXT NOT NULL UNIQUE,
//                    TrackName TEXT NOT NULL,
//                    ArtistName TEXT NOT NULL,
//                    AlbumName TEXT,
//                    AlbumImageUrl TEXT,
//                    DurationMs INTEGER NOT NULL,
//                    PreviewUrl TEXT,
//                    AddedDate TEXT NOT NULL
//                );
//            ";

//            using var command = new SqliteCommand(createTableQuery, connection);
//            command.ExecuteNonQuery();
//        }

//        public bool AddTrack(Track track)
//        {
//            try
//            {
//                using var connection = new SqliteConnection(connectionString);
//                connection.Open();

//                string insertQuery = @"
//                    INSERT OR IGNORE INTO PlaylistItems 
//                    (TrackId, TrackName, ArtistName, AlbumName, AlbumImageUrl, DurationMs, PreviewUrl, AddedDate)
//                    VALUES (@TrackId, @TrackName, @ArtistName, @AlbumName, @AlbumImageUrl, @DurationMs, @PreviewUrl, @AddedDate);
//                ";

//                using var command = new SqliteCommand(insertQuery, connection);
//                command.Parameters.AddWithValue("@TrackId", track.Id ?? "");
//                command.Parameters.AddWithValue("@TrackName", track.Name ?? "");
//                command.Parameters.AddWithValue("@ArtistName", track.ArtistName ?? "");
//                command.Parameters.AddWithValue("@AlbumName", track.AlbumName ?? "");
//                command.Parameters.AddWithValue("@AlbumImageUrl", track.AlbumImageUrl ?? "");
//                command.Parameters.AddWithValue("@DurationMs", track.DurationMs);
//                command.Parameters.AddWithValue("@PreviewUrl", track.PreviewUrl ?? "");
//                command.Parameters.AddWithValue("@AddedDate", DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"));

//                int rows = command.ExecuteNonQuery();
//                return rows > 0;
//            }
//            catch (Exception ex)
//            {
//                Console.WriteLine($"[AddTrack Error] {ex.Message}");
//                return false;
//            }
//        }

//        public bool RemoveTrack(string trackId)
//        {
//            try
//            {
//                using var connection = new SqliteConnection(connectionString);
//                connection.Open();

//                string deleteQuery = "DELETE FROM PlaylistItems WHERE TrackId = @TrackId";
//                using var command = new SqliteCommand(deleteQuery, connection);
//                command.Parameters.AddWithValue("@TrackId", trackId);

//                int rows = command.ExecuteNonQuery();
//                return rows > 0;
//            }
//            catch (Exception ex)
//            {
//                Console.WriteLine($"[RemoveTrack Error] {ex.Message}");
//                return false;
//            }
//        }

//        public List<PlaylistItem> GetAllTracks()
//        {
//            List<PlaylistItem> tracks = new();

//            try
//            {
//                using var connection = new SqliteConnection(connectionString);
//                connection.Open();

//                string selectQuery = @"
//                    SELECT Id, TrackId, TrackName, ArtistName, AlbumName, AlbumImageUrl, DurationMs, PreviewUrl, AddedDate
//                    FROM PlaylistItems
//                    ORDER BY datetime(AddedDate) DESC;
//                ";

//                using var command = new SqliteCommand(selectQuery, connection);
//                using var reader = command.ExecuteReader();

//                while (reader.Read())
//                {
//                    PlaylistItem item = new()
//                    {
//                        Id = reader.GetInt32(0),
//                        TrackId = reader.GetString(1),
//                        TrackName = reader.GetString(2),
//                        ArtistName = reader.GetString(3),
//                        AlbumName = reader.IsDBNull(4) ? "" : reader.GetString(4),
//                        AlbumImageUrl = reader.IsDBNull(5) ? "" : reader.GetString(5),
//                        DurationMs = reader.GetInt32(6),
//                        PreviewUrl = reader.IsDBNull(7) ? "" : reader.GetString(7),
//                        AddedDate = DateTime.Parse(reader.GetString(8))
//                    };

//                    tracks.Add(item);
//                }
//            }
//            catch (Exception ex)
//            {
//                Console.WriteLine($"[GetAllTracks Error] {ex.Message}");
//            }

//            return tracks;
//        }

//        public bool IsTrackInPlaylist(string trackId)
//        {
//            try
//            {
//                using var connection = new SqliteConnection(connectionString);
//                connection.Open();

//                string countQuery = "SELECT COUNT(*) FROM PlaylistItems WHERE TrackId = @TrackId";
//                using var command = new SqliteCommand(countQuery, connection);
//                command.Parameters.AddWithValue("@TrackId", trackId);

//                long count = (long)command.ExecuteScalar();
//                return count > 0;
//            }
//            catch
//            {
//                return false;
//            }
//        }

//        public int GetTrackCount()
//        {
//            try
//            {
//                using var connection = new SqliteConnection(connectionString);
//                connection.Open();

//                string countQuery = "SELECT COUNT(*) FROM PlaylistItems";
//                using var command = new SqliteCommand(countQuery, connection);

//                long count = (long)command.ExecuteScalar();
//                return (int)count;
//            }
//            catch
//            {
//                return 0;
//            }
//        }
//    }
//}
