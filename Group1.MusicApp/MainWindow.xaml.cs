using Group1.MusicApp.Models;
using Group1.MusicApp.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace Group1.MusicApp
{
    public partial class MainWindow : Window
    {
        private readonly ITunesService _itunes = new();
        private readonly LyricsService _lyricsService = new();
        //private readonly PlaylistService _playlistService = new();

        private bool _isPlaying = false;
        private string _currentQuery = "";
        private string _currentCategory = "";
        private int _currentOffset = 0;
        private bool _isLoadingMore = false;
        private readonly List<Track> _currentTracks = new();
        private Track? _currentTrackPlaying = null;

        // Timer cập nhật progress
        private readonly DispatcherTimer _progressTimer = new();

        public MainWindow()
        {
            InitializeComponent();
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            lblNowPlaying.Text = "Đang tải bài hát...";

            // Playlist view
            //if (PlaylistViewControl != null)
            //{
            //    PlaylistViewControl.Refresh();
            //    PlaylistViewControl.TrackPlayRequested += PlaylistView_TrackPlayRequested;
            //    PlaylistViewControl.CloseRequested += PlaylistView_CloseRequested;
            //}

            // Vô hạn scroll
            var scrollViewer = FindVisualChild<ScrollViewer>(lstTracks);
            if (scrollViewer != null)
                scrollViewer.ScrollChanged += ScrollViewer_ScrollChanged;

            // Timer progress
            _progressTimer.Interval = TimeSpan.FromMilliseconds(500);
            _progressTimer.Tick += (s, ev) =>
            {
                if (mediaPlayer.Source != null && mediaPlayer.NaturalDuration.HasTimeSpan)
                {
                    progressPreview.Value = mediaPlayer.Position.TotalSeconds;
                }
            };

            // Sự kiện media
            mediaPlayer.MediaOpened += (s, ev) =>
            {
                if (mediaPlayer.NaturalDuration.HasTimeSpan)
                    progressPreview.Maximum = mediaPlayer.NaturalDuration.TimeSpan.TotalSeconds;
                else
                    progressPreview.Maximum = 30; // fallback cho preview iTunes
                progressPreview.Value = 0;
            };

            mediaPlayer.MediaEnded += (s, ev) =>
            {
                _isPlaying = false;
                _progressTimer.Stop();
                progressPreview.Value = progressPreview.Maximum;
                lblNowPlaying.Text = "Đã phát xong preview.";
            };

            mediaPlayer.MediaFailed += (s, ev) =>
            {
                _isPlaying = false;
                _progressTimer.Stop();
                MessageBox.Show("Không phát được audio.", "Media Failed",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            };

            // Tự động load danh mục Trendy khi khởi động lần đầu
            await LoadCategoryTracksAsync("Trendy");
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

        // ===== SEARCH =====
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
                _currentCategory = ""; // Clear category when searching
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
                
                // Không group khi search - hiển thị flat list
                // Sử dụng trực tiếp list thay vì CollectionViewSource để tránh lỗi scroll
                lstTracks.ItemsSource = null;
                lstTracks.ItemsSource = _currentTracks;
                
                lblNowPlaying.Text = $"Tìm thấy {_currentTracks.Count} bài hát (iTunes)";
                ShowSearchView();

                // Reset category button styles when searching
                UpdateCategoryButtonStyles("");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi tìm kiếm: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void ScrollViewer_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            if (_isLoadingMore) return;
            var sv = sender as ScrollViewer;
            if (sv == null) return;

            if (sv.VerticalOffset + sv.ViewportHeight >= sv.ExtentHeight - 50)
            {
                _isLoadingMore = true;
                _currentOffset += 20;
                lblNowPlaying.Text = "Đang tải thêm...";

                try
                {
                    List<Track>? moreTracks = null;
                    if (!string.IsNullOrEmpty(_currentCategory))
                    {
                        // Load thêm từ danh mục
                        moreTracks = await _itunes.GetTracksByCategoryAsync(_currentCategory, limit: 20);
                    }
                    else if (!string.IsNullOrEmpty(_currentQuery))
                    {
                        // Load thêm từ tìm kiếm
                        moreTracks = await _itunes.SearchTracksAsync(_currentQuery, limit: 20, offset: _currentOffset);
                    }

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

        private void lstTracks_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            DependencyObject obj = (DependencyObject)e.OriginalSource;
            while (obj != null && !(obj is ListViewItem))
                obj = VisualTreeHelper.GetParent(obj);
            if (obj is ListViewItem item) item.IsSelected = true;
        }

        // ===== PLAY & LYRICS (PLAIN TEXT) =====
        private async void lstTracks_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (lstTracks.SelectedItem is not Track selected) return;
            ShowSearchView();
            await PlaySelectedTrackAsync(selected);
        }

        private async Task PlaySelectedTrackAsync(Track selected)
        {
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
                    _progressTimer.Start();
                    _isPlaying = true;
                    lblNowPlaying.Text = $"▶ Đang phát (Preview): {selected.Name}";
                }
                else
                {
                    lblNowPlaying.Text = $"{selected.Name} (Không có preview)";
                    _isPlaying = false;
                    _progressTimer.Stop();
                }

                // Chỉ hiển thị lyrics dạng text
                await LoadPlainLyricsAsync(selected.ArtistName ?? "", selected.Name ?? "");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi phát: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task LoadPlainLyricsAsync(string artist, string title)
        {
            // Nếu chưa dùng lyrics sync, chỉ cần ẩn svLyrics
            svLyrics.Visibility = Visibility.Collapsed;
            svPlainLyrics.Visibility = Visibility.Visible;
            txtLyrics.Text = "Đang tải lời bài hát...";

            var result = await _lyricsService.GetSyncedAsync(artist, title);
            if (!string.IsNullOrWhiteSpace(result.Plain))
                txtLyrics.Text = result.Plain;
            else
                txtLyrics.Text = "🎶 Chưa có lời bài hát.";
        }

        private void btnPlayPause_Click(object sender, RoutedEventArgs e)
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

            if (_isPlaying)
            {
                mediaPlayer.Pause();
                _progressTimer.Stop();
                _isPlaying = false;
            }
            else
            {
                mediaPlayer.Play();
                _progressTimer.Start();
                _isPlaying = true;
            }
        }

        private void btnPrev_Click(object sender, RoutedEventArgs e) => PlayRelative(-1);
        private void btnNext_Click(object sender, RoutedEventArgs e) => PlayRelative(1);

        private void PlayRelative(int delta)
        {
            if (_currentTracks.Count == 0) return;

            int idx = _currentTrackPlaying != null
                ? _currentTracks.FindIndex(t => t.Id == _currentTrackPlaying.Id)
                : lstTracks.SelectedIndex;

            if (idx < 0) idx = 0;
            int next = (idx + delta + _currentTracks.Count) % _currentTracks.Count;

            ShowSearchView();
            lstTracks.SelectedIndex = next;
            lstTracks.ScrollIntoView(lstTracks.SelectedItem);
        }

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

                ShowSearchView();
                await PlaySelectedTrackAsync(t);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi phát bài hát: {ex.Message}", "Lỗi",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                lblNowPlaying.Text = "Lỗi khi phát bài hát";
            }
        }

        private void PlaylistView_CloseRequested(object? sender, EventArgs e) => ShowSearchView();

        private void SearchMenuItem_Selected(object sender, RoutedEventArgs e) => ShowSearchView();
        private void PlaylistMenuItem_Selected(object sender, RoutedEventArgs e) => ShowPlaylistView();

        private void ShowSearchView()
        {
            SearchResultsContainer.Visibility = Visibility.Visible;
            PlaylistViewControl.Visibility = Visibility.Collapsed;
        }

        private void ShowPlaylistView()
        {
            SearchResultsContainer.Visibility = Visibility.Collapsed;
            PlaylistViewControl.Visibility = Visibility.Visible;
            //PlaylistViewControl.Refresh();
        }

        // ===== CATEGORY LOADING =====
        private async Task LoadMultipleCategoriesAsync()
        {
            try
            {
                lblNowPlaying.Text = "Đang tải danh sách nhạc...";
                ShowSearchView();
                
                lstTracks.ItemsSource = null;
                _currentTracks.Clear();
                _currentCategory = "";
                _currentQuery = "";

                // Load nhiều danh mục cùng lúc
                var categories = new[] { "Trendy", "Thịnh hành", "Mới phát hành", "Pop", "Rock" };
                
                foreach (var category in categories)
                {
                    try
                    {
                        var tracks = await _itunes.GetTracksByCategoryAsync(category, limit: 6);
                        if (tracks != null && tracks.Count > 0)
                        {
                            // Gán category cho mỗi track
                            foreach (var track in tracks)
                            {
                                track.Category = category;
                            }
                            _currentTracks.AddRange(tracks);
                        }
                    }
                    catch
                    {
                        // Bỏ qua nếu category nào đó fail
                    }
                }

                if (_currentTracks.Count > 0)
                {
                    // Nhóm tracks theo category
                    var groupedTracks = CollectionViewSource.GetDefaultView(_currentTracks);
                    groupedTracks.GroupDescriptions.Clear();
                    groupedTracks.GroupDescriptions.Add(new PropertyGroupDescription("Category"));
                    
                    lstTracks.ItemsSource = groupedTracks;
                    lblNowPlaying.Text = $"Đã tải {_currentTracks.Count} bài hát từ {categories.Length} danh mục";
                }
                else
                {
                    lblNowPlaying.Text = "Không tìm thấy bài hát nào";
                }
                
                ShowSearchView();
                UpdateCategoryButtonStyles("");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi tải danh mục: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                lblNowPlaying.Text = "Lỗi khi tải danh sách nhạc";
                ShowSearchView();
            }
        }

        private async void CategoryButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string category)
            {
                await LoadCategoryTracksAsync(category);
            }
        }

        private async Task LoadCategoryTracksAsync(string category)
        {
            try
            {
                _currentCategory = category;
                _currentQuery = ""; // Clear search query
                _currentOffset = 0;
                _isLoadingMore = false;

                lblNowPlaying.Text = $"Đang tải {category}...";
                
                // Đảm bảo container được hiển thị trước khi load
                ShowSearchView();
                
                lstTracks.ItemsSource = null;
                _currentTracks.Clear();

                // Load nhiều bài hát hơn để có đủ nội dung hiển thị
                var results = await _itunes.GetTracksByCategoryAsync(category, limit: 30);
                if (results == null || results.Count == 0)
                {
                    lblNowPlaying.Text = $"Không tìm thấy bài hát trong danh mục {category}.";
                    // Vẫn hiển thị danh sách trống để người dùng biết
                    lstTracks.ItemsSource = _currentTracks;
                    UpdateCategoryButtonStyles(category);
                    return;
                }

                _currentTracks.AddRange(results);
                
                // Không group khi chỉ load 1 category - hiển thị flat list
                // Sử dụng trực tiếp list thay vì CollectionViewSource để tránh lỗi scroll
                lstTracks.ItemsSource = null;
                lstTracks.ItemsSource = _currentTracks;
                
                lblNowPlaying.Text = $"Đã tải {_currentTracks.Count} bài hát - {category}";
                
                // Đảm bảo view được hiển thị
                ShowSearchView();

                // Update button styles - highlight nút được chọn
                UpdateCategoryButtonStyles(category);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi tải danh mục: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                lblNowPlaying.Text = "Lỗi khi tải danh mục";
                // Vẫn hiển thị danh sách trống
                lstTracks.ItemsSource = _currentTracks;
                ShowSearchView();
                UpdateCategoryButtonStyles(category);
            }
        }

        private void UpdateCategoryButtonStyles(string selectedCategory)
        {
            // Reset all buttons
            var defaultColor = new SolidColorBrush(Color.FromRgb(42, 42, 42));
            btnCategoryTrendy.Background = defaultColor;
            btnCategoryPopular.Background = defaultColor;
            btnCategoryNew.Background = defaultColor;
            btnCategoryPop.Background = defaultColor;
            btnCategoryRock.Background = defaultColor;
            btnCategoryHipHop.Background = defaultColor;

            // Highlight selected button
            var selectedColor = new SolidColorBrush(Color.FromRgb(29, 185, 84)); // #1DB954
            switch (selectedCategory)
            {
                case "Trendy":
                    btnCategoryTrendy.Background = selectedColor;
                    break;
                case "Thịnh hành":
                    btnCategoryPopular.Background = selectedColor;
                    break;
                case "Mới phát hành":
                    btnCategoryNew.Background = selectedColor;
                    break;
                case "Pop":
                    btnCategoryPop.Background = selectedColor;
                    break;
                case "Rock":
                    btnCategoryRock.Background = selectedColor;
                    break;
                case "Hip Hop":
                    btnCategoryHipHop.Background = selectedColor;
                    break;
            }
        }
    }
}
