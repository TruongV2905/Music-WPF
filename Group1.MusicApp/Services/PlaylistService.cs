using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Data.Sqlite;
using Group1.MusicApp.Models;

namespace Group1.MusicApp.Services
{
    // Class này dùng để quản lý playlist trong database SQLite
    public class PlaylistService
    {
        private string connectionString;  // Chuỗi kết nối database
        private string dbPath;             // Đường dẫn file database

        // Constructor - khởi tạo khi tạo object
        public PlaylistService()
        {
            // Lấy thư mục AppData của user
            string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string appFolder = Path.Combine(appDataPath, "MusicApp");
            
            // Tạo thư mục nếu chưa có
            if (!Directory.Exists(appFolder))
            {
                Directory.CreateDirectory(appFolder);
            }

            // Tạo đường dẫn file database
            dbPath = Path.Combine(appFolder, "playlist.db");
            connectionString = "Data Source=" + dbPath;
            
            // Khởi tạo database (tạo bảng nếu chưa có)
            InitializeDatabase();
        }

        // Hàm tạo bảng trong database nếu chưa có
        private void InitializeDatabase()
        {
            // Tạo kết nối đến database
            SqliteConnection connection = new SqliteConnection(connectionString);
            connection.Open();

            // Tạo câu lệnh SQL để tạo bảng
            SqliteCommand command = connection.CreateCommand();
            command.CommandText = @"
                CREATE TABLE IF NOT EXISTS PlaylistItems (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    TrackId TEXT NOT NULL UNIQUE,
                    TrackName TEXT NOT NULL,
                    ArtistName TEXT NOT NULL,
                    AlbumName TEXT,
                    AlbumImageUrl TEXT,
                    DurationMs INTEGER NOT NULL,
                    PreviewUrl TEXT,
                    AddedDate TEXT NOT NULL
                );
            ";

            // Thực thi câu lệnh
            command.ExecuteNonQuery();
            
            // Đóng kết nối
            connection.Close();
        }

        // Hàm thêm bài hát vào playlist
        public bool AddTrack(Track track)
        {
            try
            {
                // Mở kết nối database
                SqliteConnection connection = new SqliteConnection(connectionString);
                connection.Open();

                // Tạo câu lệnh INSERT
                SqliteCommand command = connection.CreateCommand();
                command.CommandText = @"
                    INSERT OR IGNORE INTO PlaylistItems 
                    (TrackId, TrackName, ArtistName, AlbumName, AlbumImageUrl, DurationMs, PreviewUrl, AddedDate)
                    VALUES (@TrackId, @TrackName, @ArtistName, @AlbumName, @AlbumImageUrl, @DurationMs, @PreviewUrl, @AddedDate)
                ";

                // Thêm các tham số vào câu lệnh
                command.Parameters.AddWithValue("@TrackId", track.Id);
                command.Parameters.AddWithValue("@TrackName", track.Name);
                command.Parameters.AddWithValue("@ArtistName", track.ArtistName ?? "");
                command.Parameters.AddWithValue("@AlbumName", track.AlbumName ?? "");
                command.Parameters.AddWithValue("@AlbumImageUrl", track.AlbumImageUrl ?? "");
                command.Parameters.AddWithValue("@DurationMs", track.DurationMs);
                command.Parameters.AddWithValue("@PreviewUrl", track.PreviewUrl ?? "");
                command.Parameters.AddWithValue("@AddedDate", DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"));

                // Thực thi và lấy số dòng bị ảnh hưởng
                int rowsAffected = command.ExecuteNonQuery();
                
                // Đóng kết nối
                connection.Close();
                
                // Trả về true nếu thêm thành công (rowsAffected > 0)
                return rowsAffected > 0;
            }
            catch (Exception ex)
            {
                throw new Exception("Lỗi khi thêm bài hát vào playlist: " + ex.Message, ex);
            }
        }

        // Hàm xóa bài hát khỏi playlist
        public bool RemoveTrack(string trackId)
        {
            try
            {
                // Mở kết nối database
                SqliteConnection connection = new SqliteConnection(connectionString);
                connection.Open();

                // Tạo câu lệnh DELETE
                SqliteCommand command = connection.CreateCommand();
                command.CommandText = "DELETE FROM PlaylistItems WHERE TrackId = @TrackId";
                command.Parameters.AddWithValue("@TrackId", trackId);

                // Thực thi và lấy số dòng bị ảnh hưởng
                int rowsAffected = command.ExecuteNonQuery();
                
                // Đóng kết nối
                connection.Close();
                
                // Trả về true nếu xóa thành công
                return rowsAffected > 0;
            }
            catch (Exception ex)
            {
                throw new Exception("Lỗi khi xóa bài hát khỏi playlist: " + ex.Message, ex);
            }
        }

        // Hàm lấy tất cả bài hát trong playlist
        public List<PlaylistItem> GetAllTracks()
        {
            // Tạo danh sách để chứa kết quả
            List<PlaylistItem> tracks = new List<PlaylistItem>();

            try
            {
                // Mở kết nối database
                SqliteConnection connection = new SqliteConnection(connectionString);
                connection.Open();

                // Tạo câu lệnh SELECT
                SqliteCommand command = connection.CreateCommand();
                command.CommandText = @"
                    SELECT Id, TrackId, TrackName, ArtistName, AlbumName, AlbumImageUrl, DurationMs, PreviewUrl, AddedDate
                    FROM PlaylistItems
                    ORDER BY AddedDate DESC
                ";

                // Thực thi và đọc kết quả
                SqliteDataReader reader = command.ExecuteReader();
                while (reader.Read())
                {
                    // Tạo object PlaylistItem từ dữ liệu đọc được
                    PlaylistItem item = new PlaylistItem();
                    item.Id = reader.GetInt32(0);
                    item.TrackId = reader.GetString(1);
                    item.TrackName = reader.GetString(2);
                    item.ArtistName = reader.GetString(3);
                    
                    // Kiểm tra null trước khi đọc
                    if (!reader.IsDBNull(4))
                        item.AlbumName = reader.GetString(4);
                    if (!reader.IsDBNull(5))
                        item.AlbumImageUrl = reader.GetString(5);
                    
                    item.DurationMs = reader.GetInt32(6);
                    
                    if (!reader.IsDBNull(7))
                        item.PreviewUrl = reader.GetString(7);
                    
                    item.AddedDate = DateTime.Parse(reader.GetString(8));
                    
                    // Thêm vào danh sách
                    tracks.Add(item);
                }
                
                // Đóng reader và connection
                reader.Close();
                connection.Close();
            }
            catch (Exception ex)
            {
                throw new Exception("Lỗi khi lấy danh sách bài hát: " + ex.Message, ex);
            }

            return tracks;
        }

        // Hàm kiểm tra xem bài hát đã có trong playlist chưa
        public bool IsTrackInPlaylist(string trackId)
        {
            try
            {
                // Mở kết nối database
                SqliteConnection connection = new SqliteConnection(connectionString);
                connection.Open();

                // Tạo câu lệnh đếm số lượng
                SqliteCommand command = connection.CreateCommand();
                command.CommandText = "SELECT COUNT(*) FROM PlaylistItems WHERE TrackId = @TrackId";
                command.Parameters.AddWithValue("@TrackId", trackId);

                // Thực thi và lấy kết quả
                object result = command.ExecuteScalar();
                int count = Convert.ToInt32(result);
                
                // Đóng kết nối
                connection.Close();
                
                // Trả về true nếu count > 0 (có bài hát)
                return count > 0;
            }
            catch
            {
                return false;
            }
        }

        // Hàm đếm số lượng bài hát trong playlist
        public int GetTrackCount()
        {
            try
            {
                // Mở kết nối database
                SqliteConnection connection = new SqliteConnection(connectionString);
                connection.Open();

                // Tạo câu lệnh đếm
                SqliteCommand command = connection.CreateCommand();
                command.CommandText = "SELECT COUNT(*) FROM PlaylistItems";

                // Thực thi và lấy kết quả
                object result = command.ExecuteScalar();
                int count = Convert.ToInt32(result);
                
                // Đóng kết nối
                connection.Close();
                
                return count;
            }
            catch
            {
                return 0;
            }
        }
    }
}

