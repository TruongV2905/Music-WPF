using System;
using System.Windows;
using System.Windows.Controls;
using Group1.MusicApp.Models;
using Group1.MusicApp.ViewModels;

namespace Group1.MusicApp.Views
{
    // UserControl để hiển thị danh sách playlist
    public partial class PlaylistView : UserControl
    {
        private PlaylistViewModel viewModel;  // ViewModel chứa dữ liệu

        // Event để thông báo khi muốn phát bài hát
        public event EventHandler<string>? TrackPlayRequested;

        // Event để thông báo khi muốn đóng PlaylistView
        public event EventHandler? CloseRequested;

        // Constructor - khởi tạo
        public PlaylistView()
        {
            InitializeComponent();
            
            // Tạo ViewModel mới
            viewModel = new PlaylistViewModel();
            
            // Gán dữ liệu cho ListBox
            PlaylistItemsControl.ItemsSource = viewModel.PlaylistItems;
            
            // Cập nhật giao diện
            UpdateTrackCount();
            UpdateEmptyState();
        }

        // Hàm làm mới danh sách playlist
        public void Refresh()
        {
            viewModel.LoadPlaylist();
            // Clear và set lại ItemsSource để force refresh UI
            PlaylistItemsControl.ItemsSource = null;
            PlaylistItemsControl.ItemsSource = viewModel.PlaylistItems;
            UpdateTrackCount();
            UpdateEmptyState();
        }

        // Hàm thêm bài hát vào playlist
        public bool AddTrack(Track track)
        {
            bool result = viewModel.AddTrack(track);
            if (result)
            {
                // Clear và set lại ItemsSource để force refresh UI
                PlaylistItemsControl.ItemsSource = null;
                PlaylistItemsControl.ItemsSource = viewModel.PlaylistItems;
                UpdateTrackCount();
                UpdateEmptyState();
            }
            return result;
        }

        // Hàm kiểm tra bài hát có trong playlist không
        public bool IsTrackInPlaylist(string trackId)
        {
            return viewModel.IsTrackInPlaylist(trackId);
        }

        // Hàm cập nhật số lượng bài hát hiển thị
        private void UpdateTrackCount()
        {
            int count = viewModel.GetTrackCount();
            if (count == 1)
            {
                TrackCountText.Text = count + " bài hát";
            }
            else
            {
                TrackCountText.Text = count + " bài hát";
            }
        }

        // Hàm cập nhật trạng thái khi playlist rỗng
        private void UpdateEmptyState()
        {
            if (viewModel.PlaylistItems.Count == 0)
            {
                // Hiển thị thông báo playlist rỗng
                EmptyStatePanel.Visibility = Visibility.Visible;
                PlaylistItemsControl.Visibility = Visibility.Collapsed;
            }
            else
            {
                // Ẩn thông báo, hiển thị danh sách
                EmptyStatePanel.Visibility = Visibility.Collapsed;
                PlaylistItemsControl.Visibility = Visibility.Visible;
            }
        }

        // Hàm xử lý khi click nút Play
        private void PlayButton_Click(object sender, RoutedEventArgs e)
        {
            // Lấy button được click
            Button? button = sender as Button;
            if (button != null)
            {
                // Lấy PlaylistItem từ Tag
                PlaylistItem? item = button.Tag as PlaylistItem;
                if (item != null && !string.IsNullOrEmpty(item.TrackId))
                {
                    // Gửi event để phát bài hát
                    TrackPlayRequested?.Invoke(this, item.TrackId);
                }
            }
        }

        // Hàm xử lý khi click nút Delete
        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            // Lấy button được click
            Button? button = sender as Button;
            if (button != null)
            {
                // Lấy TrackId từ Tag (có thể là string hoặc object)
                string? trackId = button.Tag?.ToString();
                if (!string.IsNullOrEmpty(trackId))
                {
                    // Hỏi xác nhận trước khi xóa
                    MessageBoxResult result = MessageBox.Show(
                        "Bạn có chắc muốn xóa bài hát này khỏi playlist?",
                        "Xóa bài hát",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question);

                    if (result == MessageBoxResult.Yes)
                    {
                        // Xóa bài hát
                        viewModel.RemoveTrack(trackId);
                        
                        // Cập nhật lại danh sách bằng cách clear và set lại ItemsSource
                        PlaylistItemsControl.ItemsSource = null;
                        PlaylistItemsControl.ItemsSource = viewModel.PlaylistItems;
                        
                        UpdateTrackCount();
                        UpdateEmptyState();
                    }
                }
            }
        }

        // Hàm xử lý khi click nút đóng
        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            // Gửi event để đóng PlaylistView
            CloseRequested?.Invoke(this, EventArgs.Empty);
        }
        public List<PlaylistItem> GetPlaylistItems()
        {
            if (viewModel?.PlaylistItems == null || viewModel.PlaylistItems.Count == 0)
                return new List<PlaylistItem>();

            return viewModel.PlaylistItems.ToList();
        }
    }
}

