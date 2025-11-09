using System;
using System.Collections.Generic;
using Group1.MusicApp.Models;
using Group1.MusicApp.Services;

namespace Group1.MusicApp.ViewModels
{
    // ViewModel đơn giản để quản lý playlist
    // ViewModel này chỉ chứa dữ liệu và gọi service, không có logic phức tạp
    public class PlaylistViewModel
    {
        private PlaylistService playlistService;  // Service để làm việc với database

        // Danh sách bài hát trong playlist
        public List<PlaylistItem> PlaylistItems { get; set; }

        // Constructor - khởi tạo
        public PlaylistViewModel()
        {
            playlistService = new PlaylistService();
            PlaylistItems = new List<PlaylistItem>();
            LoadPlaylist();
        }

        // Hàm tải danh sách bài hát từ database
        public void LoadPlaylist()
        {
            PlaylistItems.Clear();
            List<PlaylistItem> tracks = playlistService.GetAllTracks();
            
            // Thêm từng bài hát vào danh sách
            foreach (PlaylistItem track in tracks)
            {
                PlaylistItems.Add(track);
            }
        }

        // Hàm thêm bài hát vào playlist
        public bool AddTrack(Track track)
        {
            try
            {
                // Kiểm tra xem bài hát đã có chưa
                if (playlistService.IsTrackInPlaylist(track.Id))
                {
                    return false; // Đã có rồi, không thêm nữa
                }

                // Thêm vào database
                if (playlistService.AddTrack(track))
                {
                    // Tải lại danh sách
                    LoadPlaylist();
                    return true;
                }
                return false;
            }
            catch
            {
                return false;
            }
        }

        // Hàm xóa bài hát khỏi playlist
        public void RemoveTrack(string trackId)
        {
            try
            {
                // Xóa khỏi database
                if (playlistService.RemoveTrack(trackId))
                {
                    // Tìm và xóa khỏi danh sách hiện tại
                    for (int i = 0; i < PlaylistItems.Count; i++)
                    {
                        if (PlaylistItems[i].TrackId == trackId)
                        {
                            PlaylistItems.RemoveAt(i);
                            break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show("Lỗi khi xóa bài hát: " + ex.Message, 
                    "Lỗi", 
                    System.Windows.MessageBoxButton.OK, 
                    System.Windows.MessageBoxImage.Error);
            }
        }

        // Hàm kiểm tra bài hát có trong playlist không
        public bool IsTrackInPlaylist(string trackId)
        {
            return playlistService.IsTrackInPlaylist(trackId);
        }

        // Hàm đếm số lượng bài hát
        public int GetTrackCount()
        {
            return playlistService.GetTrackCount();
        }
    }
}
