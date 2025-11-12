
using Group1.MusicApp.Models;
using Group1.MusicApp.Services;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Group1.MusicApp
{
    public partial class MainWindow : Window
    {
        private readonly ITunesService _itunes = new();
        private readonly LyricsService _lyrics = new();
        private readonly PlaylistService _playlistService = new();

        private bool _isPlaying = false;
        private string _currentQuery = "";
        private int _currentOffset = 0;
        private bool _isLoadingMore = false;
        private readonly List<Track> _currentTracks = new();
        private Track? _currentTrackPlaying = null;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            lblNowPlaying.Text = "Ready - Search with iTunes!";
            if (PlaylistViewControl != null)
            {
                PlaylistViewControl.Refresh();
                PlaylistViewControl.TrackPlayRequested += PlaylistView_TrackPlayRequested;
                PlaylistViewControl.CloseRequested += PlaylistView_CloseRequested;
            }

            var scrollViewer = FindVisualChild<ScrollViewer>(lstTracks);
            if (scrollViewer != null)
                scrollViewer.ScrollChanged += ScrollViewer_ScrollChanged;
        }

        private static T FindVisualChild<T>(DependencyObject obj) where T : DependencyObject
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(obj); i++)
            {
                var child = VisualTreeHelper.GetChild(obj, i);
                if (child is T t) return t;
                var result = FindVisualChild<T>(child);
                if (result != null) return result;
            }
            return null;
        }


        private async void btnSearch_Click(object sender, RoutedEventArgs e) => await PerformSearch();
        private async void txtSearch_KeyDown(object sender, KeyEventArgs e) { if (e.Key == Key.Enter) await PerformSearch(); }

        private async Task PerformSearch()
        {
            _currentQuery = txtSearch.Text?.Trim() ?? "";
            if (string.IsNullOrEmpty(_currentQuery))
            {
                MessageBox.Show("Vui lòng nhập từ khóa tìm kiếm.", "Tìm kiếm", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                lblNowPlaying.Text = "Đang tìm kiếm (iTunes)...";
                lstTracks.ItemsSource = null;
                _currentTracks.Clear();
                _currentOffset = 0;

                var results = await _itunes.SearchTracksAsync(_currentQuery, limit: 20, offset: _currentOffset);
                if (results == null || results.Count == 0)
                {
                    MessageBox.Show("Không tìm thấy bài hát.", "Tìm kiếm", MessageBoxButton.OK, MessageBoxImage.Information);
                    lblNowPlaying.Text = "Không có kết quả.";
                    return;
                }

                _currentTracks.AddRange(results);
                lstTracks.ItemsSource = _currentTracks;
                lblNowPlaying.Text = $"Tìm thấy {_currentTracks.Count} bài hát (iTunes)";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi tìm kiếm: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Vô hạn (infinite scroll)
        private async void ScrollViewer_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            if (_isLoadingMore || string.IsNullOrEmpty(_currentQuery)) return;

            var sv = sender as ScrollViewer;
            if (sv == null) return;

            if (sv.VerticalOffset + sv.ViewportHeight >= sv.ExtentHeight - 50)
            {
                _isLoadingMore = true;
                _currentOffset += 20;
                lblNowPlaying.Text = "Đang tải thêm...";

                try
                {
                    var moreTracks = await _itunes.SearchTracksAsync(_currentQuery, limit: 20, offset: _currentOffset);
                    if (moreTracks != null && moreTracks.Count > 0)
                    {
                        _currentTracks.AddRange(moreTracks);
                        lstTracks.ItemsSource = null;
                        lstTracks.ItemsSource = _currentTracks;
                        lblNowPlaying.Text = $"Đã tải {_currentTracks.Count} bài hát";
                    }
                    else
                    {
                        lblNowPlaying.Text = "Hết kết quả!";
                    }
                }
                catch (Exception ex)
                {
                    lblNowPlaying.Text = $"Lỗi tải thêm: {ex.Message}";
                }
                finally
                {
                    _isLoadingMore = false;
                }
            }
        }

        // Bắt chọn item chính xác
        private void lstTracks_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            DependencyObject obj = (DependencyObject)e.OriginalSource;
            while (obj != null && !(obj is ListViewItem))
                obj = VisualTreeHelper.GetParent(obj);
            if (obj is ListViewItem item) item.IsSelected = true;
        }

        // ========= PLAY =========
        private async void lstTracks_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (lstTracks.SelectedItem is not Track selected) return;

            _currentTrackPlaying = selected;
            lblNowPlaying.Text = "Đang tải bài hát...";
            lblArtist.Text = selected.ArtistName ?? "";

            if (!string.IsNullOrEmpty(selected.AlbumImageUrl))
                imgCover.Source = new BitmapImage(new Uri(selected.AlbumImageUrl));

            try
            {
                if (!string.IsNullOrEmpty(selected.PreviewUrl))
                {
                    mediaPlayer.Source = new Uri(selected.PreviewUrl);
                    mediaPlayer.Play();
                    _isPlaying = true;
                    lblNowPlaying.Text = $"▶ Đang phát (Preview): {selected.Name}";
                }
                else
                {
                    lblNowPlaying.Text = $"{selected.Name} (Không có preview)";
                    _isPlaying = false;
                }

                // Lyrics
                var lyrics = await _lyrics.GetLyricsAsync(selected.ArtistName ?? "", selected.Name ?? "");
                txtLyrics.Text = string.IsNullOrWhiteSpace(lyrics) ? "🎶 Chưa có lời bài hát..." : lyrics;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi phát: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Play/Pause button
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (_currentTrackPlaying == null)
            {
                MessageBox.Show("Vui lòng chọn bài hát để phát.", "Chưa chọn bài hát",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (mediaPlayer.Source == null && !string.IsNullOrEmpty(_currentTrackPlaying.PreviewUrl))
            {
                mediaPlayer.Source = new Uri(_currentTrackPlaying.PreviewUrl);
            }

            if (mediaPlayer.Source == null)
            {
                MessageBox.Show("Bài hát này không có preview để phát.", "Không khả dụng",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (_isPlaying) { mediaPlayer.Pause(); _isPlaying = false; }
            else { mediaPlayer.Play(); _isPlaying = true; }
        }

        // ========= PlaylistView events =========
        private async void PlaylistView_TrackPlayRequested(object? sender, string trackId)
        {
            try
            {
                lblNowPlaying.Text = "Loading track...";
                var t = await _itunes.GetTrackByIdAsync(trackId);
                if (t == null)
                {
                    lblNowPlaying.Text = "Không tìm thấy bài hát để phát.";
                    return;
                }

                _currentTrackPlaying = t;
                lblNowPlaying.Text = t.Name;
                lblArtist.Text = t.ArtistName;

                if (!string.IsNullOrEmpty(t.AlbumImageUrl))
                    imgCover.Source = new BitmapImage(new Uri(t.AlbumImageUrl));

                if (!string.IsNullOrEmpty(t.PreviewUrl))
                {
                    mediaPlayer.Source = new Uri(t.PreviewUrl);
                    mediaPlayer.Play();
                    _isPlaying = true;
                    lblNowPlaying.Text = $"▶ Đang phát (Preview): {t.Name}";
                }
                else
                {
                    _isPlaying = false;
                    lblNowPlaying.Text = $"{t.Name} (Không có preview)";
                }

                var lyrics = await _lyrics.GetLyricsAsync(t.ArtistName ?? "", t.Name ?? "");
                txtLyrics.Text = string.IsNullOrWhiteSpace(lyrics) ? "🎶 Chưa có lời bài hát..." : lyrics;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi phát bài hát: {ex.Message}", "Lỗi",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                lblNowPlaying.Text = "Lỗi khi phát bài hát";
            }
        }

        private void PlaylistView_CloseRequested(object? sender, EventArgs e) => ShowSearchView();

        // ========= View switch =========
        private void SearchMenuItem_Selected(object sender, RoutedEventArgs e) => ShowSearchView();
        private void PlaylistMenuItem_Selected(object sender, RoutedEventArgs e) => ShowPlaylistView();
        private void ShowSearchView() { SearchResultsContainer.Visibility = Visibility.Visible; PlaylistViewControl.Visibility = Visibility.Collapsed; }
        private void ShowPlaylistView() { SearchResultsContainer.Visibility = Visibility.Collapsed; PlaylistViewControl.Visibility = Visibility.Visible; PlaylistViewControl.Refresh(); }
    }
}
