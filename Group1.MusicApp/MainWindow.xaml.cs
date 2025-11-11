using Group1.ApiClient;
using Group1.MusicApp.Models;
using Group1.MusicApp.Services;
using Group1.MusicApp.ViewModels;
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
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private MusicAPI _musicApi;
        private TrackDetailViewModel _viewModel;
        private PlaylistService _playlistService;
        private bool _isPlaying = false;
        private string _currentQuery = "";
        private int _currentOffset = 0;
        private bool _isLoadingMore = false;
        private List<Track> _currentTracks = new();

        public MainWindow()
        {
            InitializeComponent();

            try
            {
                DotNetEnv.Env.Load();
            }
            catch { }

            string clientId = DotNetEnv.Env.GetString("SPOTIFY_CLIENT_ID", "YOUR_CLIENT_ID");
            string clientSecret = DotNetEnv.Env.GetString("SPOTIFY_CLIENT_SECRET", "YOUR_CLIENT_SECRET");

            if (clientId == "YOUR_CLIENT_ID" || clientSecret == "YOUR_CLIENT_SECRET")
            {
                MessageBox.Show(
                    "Please configure your Spotify API credentials in the .env file.\n\nSee SETUP_CREDENTIALS.md for instructions.",
                    "Configuration Required",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning
                );
            }

            _musicApi = new MusicAPI(clientId, clientSecret);
            _viewModel = new TrackDetailViewModel(_musicApi);
            _playlistService = new PlaylistService();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // Window is loaded and ready
            lblNowPlaying.Text = "Ready - Search for music!";
            
            // Refresh playlist view if it exists
            if (PlaylistViewControl != null)
            {
                PlaylistViewControl.Refresh();
            }

            var scrollViewer = FindVisualChild<ScrollViewer>(lstTracks);
            if (scrollViewer != null)
            {
                scrollViewer.ScrollChanged += ScrollViewer_ScrollChanged;
            }
        }

        private async void ScrollViewer_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            if (_isLoadingMore) return;

            var scrollViewer = sender as ScrollViewer;
            if (scrollViewer == null || string.IsNullOrEmpty(_currentQuery)) return;

            // Khi kéo gần đến cuối danh sách
            if (scrollViewer.VerticalOffset + scrollViewer.ViewportHeight >= scrollViewer.ExtentHeight - 50)
            {
                _isLoadingMore = true;
                _currentOffset += 20;

                lblNowPlaying.Text = "Đang tải thêm...";

                try
                {
                    var moreTracks = await _viewModel.SearchTracksAsync(_currentQuery, 20, _currentOffset);
                    if (moreTracks.Count > 0)
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

        private static T FindVisualChild<T>(DependencyObject obj) where T : DependencyObject
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(obj); i++)
            {
                var child = VisualTreeHelper.GetChild(obj, i);
                if (child is T t)
                    return t;

                var result = FindVisualChild<T>(child);
                if (result != null)
                    return result;
            }
            return null;
        }

        private async void btnSearch_Click(object sender, RoutedEventArgs e)
        {
            await PerformSearch();
        }

        private async void txtSearch_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                await PerformSearch();
            }
        }

        private async Task PerformSearch()
        {
            _currentQuery = txtSearch.Text?.Trim();

            if (string.IsNullOrEmpty(_currentQuery))
            {
                MessageBox.Show("Vui lòng nhập từ khóa tìm kiếm.", "Tìm kiếm", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                lblNowPlaying.Text = "Đang tìm kiếm...";
                lstTracks.ItemsSource = null;
                _currentTracks.Clear();
                _currentOffset = 0;

                var results = await _viewModel.SearchTracksAsync(_currentQuery, 20, _currentOffset);

                if (results == null || results.Count == 0)
                {
                    MessageBox.Show("Không tìm thấy kết quả", "Tìm kiếm", MessageBoxButton.OK, MessageBoxImage.Information);
                    lblNowPlaying.Text = "Không tìm thấy kết quả";
                    return;
                }

                _currentTracks.AddRange(results);
                lstTracks.ItemsSource = _currentTracks;
                lblNowPlaying.Text = $"Tìm thấy {_currentTracks.Count} bài hát";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi tìm kiếm: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                lblNowPlaying.Text = "Lỗi khi tìm kiếm";
            }
        }

        private async void lstTracks_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (lstTracks.SelectedItem is Track selectedTrack)
            {
                try
                {
                    lblNowPlaying.Text = "Đang tải bài hát...";

                    // Fetch full track details
                    var fullTrackDetails = await _viewModel.GetTrackDetailsAsync(selectedTrack.Id);

                    // Update UI with track info
                    lblNowPlaying.Text = fullTrackDetails.Name;
                    lblArtist.Text = fullTrackDetails.ArtistName;

                    if (!string.IsNullOrEmpty(fullTrackDetails.AlbumImageUrl))
                    {
                        imgCover.Source = new BitmapImage(new Uri(fullTrackDetails.AlbumImageUrl));
                    }

                    // Try to play preview if available
                    if (!string.IsNullOrEmpty(fullTrackDetails.PreviewUrl))
                    {
                        try
                        {
                            mediaPlayer.Source = new Uri(fullTrackDetails.PreviewUrl);
                            mediaPlayer.Play();
                            _isPlaying = true;
                            lblNowPlaying.Text = $"Đang phát: {fullTrackDetails.Name}";
                        }
                        catch
                        {
                            _isPlaying = false;
                            lblNowPlaying.Text = $"Đã tải: {fullTrackDetails.Name} (Không có preview)";
                        }
                    }
                    else
                    {
                        _isPlaying = false;
                        lblNowPlaying.Text = $"Đã tải: {fullTrackDetails.Name} (Không có preview)";
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Lỗi khi tải bài hát: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                    lblNowPlaying.Text = "Lỗi khi tải bài hát";
                }
            }
        }

        /// <summary>
        /// Handle adding track to playlist
        /// </summary>
        private void AddToPlaylistButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is Track track)
            {
                try
                {
                    // Check if track is already in playlist
                    if (_playlistService.IsTrackInPlaylist(track.Id))
                    {
                        MessageBox.Show(
                            $"Bài hát '{track.Name}' đã có trong playlist rồi!",
                            "Đã có rồi",
                            MessageBoxButton.OK,
                            MessageBoxImage.Information);
                        return;
                    }

                    // Add track to playlist
                    if (_playlistService.AddTrack(track))
                    {
                        MessageBox.Show(
                            $"Đã thêm '{track.Name}' vào playlist!",
                            "Thêm thành công",
                            MessageBoxButton.OK,
                            MessageBoxImage.Information);

                        // Refresh playlist view if it exists
                        if (PlaylistViewControl != null)
                        {
                            PlaylistViewControl.Refresh();
                        }
                    }
                    else
                    {
                        MessageBox.Show(
                            "Không thể thêm bài hát vào playlist.",
                            "Lỗi",
                            MessageBoxButton.OK,
                            MessageBoxImage.Error);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        $"Lỗi khi thêm bài hát vào playlist: {ex.Message}",
                        "Lỗi",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                }
            }
        }

        /// <summary>
        /// Handle menu item selection - Search
        /// </summary>
        private void SearchMenuItem_Selected(object sender, RoutedEventArgs e)
        {
            ShowSearchView();
        }

        /// <summary>
        /// Handle menu item selection - Playlist
        /// </summary>
        private void PlaylistMenuItem_Selected(object sender, RoutedEventArgs e)
        {
            ShowPlaylistView();
        }

        /// <summary>
        /// Show search view
        /// </summary>
        private void ShowSearchView()
        {
            SearchResultsContainer.Visibility = Visibility.Visible;
            if (PlaylistViewControl != null)
            {
                PlaylistViewControl.Visibility = Visibility.Collapsed;
            }
        }

        /// <summary>
        /// Show playlist view
        /// </summary>
        private void ShowPlaylistView()
        {
            SearchResultsContainer.Visibility = Visibility.Collapsed;
            if (PlaylistViewControl != null)
            {
                PlaylistViewControl.Visibility = Visibility.Visible;
                PlaylistViewControl.Refresh();
            }
        }

        /// <summary>
        /// Handle track play request from playlist
        /// </summary>
        private async void PlaylistView_TrackPlayRequested(object? sender, string trackId)
        {
            if (string.IsNullOrEmpty(trackId))
                return;

            try
            {
                lblNowPlaying.Text = "Loading track...";

                // Fetch full track details with audio features
                var fullTrackDetails = await _viewModel.GetTrackDetailsAsync(trackId);

                // Update UI with track info
                lblNowPlaying.Text = fullTrackDetails.Name;
                lblArtist.Text = fullTrackDetails.ArtistName;

                if (!string.IsNullOrEmpty(fullTrackDetails.AlbumImageUrl))
                {
                    imgCover.Source = new BitmapImage(new Uri(fullTrackDetails.AlbumImageUrl));
                }

                // Update metadata
                lblDuration.Text = fullTrackDetails.Duration;
                lblPopularity.Text = fullTrackDetails.PopularityText;
                lblReleaseDate.Text = fullTrackDetails.ReleaseDateText;
                lblAlbum.Text = fullTrackDetails.AlbumName;
                metadataPanel.Visibility = Visibility.Visible;

                // Update audio features if available
                if (fullTrackDetails.AudioFeatures != null)
                {
                    lblEnergy.Text = $"{fullTrackDetails.AudioFeatures.EnergyPercent}%";
                    lblDanceability.Text = $"{fullTrackDetails.AudioFeatures.DanceabilityPercent}%";
                    lblValence.Text = $"{fullTrackDetails.AudioFeatures.ValencePercent}%";
                    lblAcousticness.Text = $"{fullTrackDetails.AudioFeatures.AcousticnessPercent}%";
                    audioFeaturesPanel.Visibility = Visibility.Visible;
                }
                else
                {
                    audioFeaturesPanel.Visibility = Visibility.Collapsed;
                }

                // Try to play preview if available
                if (!string.IsNullOrEmpty(fullTrackDetails.PreviewUrl))
                {
                    try
                    {
                        mediaPlayer.Source = new Uri(fullTrackDetails.PreviewUrl);
                        mediaPlayer.Play();
                        _isPlaying = true;
                        lblNowPlaying.Text = $"Đang phát: {fullTrackDetails.Name}";
                    }
                    catch
                    {
                        _isPlaying = false;
                        lblNowPlaying.Text = $"Đã tải: {fullTrackDetails.Name} (Không có preview)";
                    }
                }
                else
                {
                    _isPlaying = false;
                    lblNowPlaying.Text = $"Đã tải: {fullTrackDetails.Name} (Không có preview)";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Lỗi khi phát bài hát: {ex.Message}",
                    "Lỗi",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                lblNowPlaying.Text = "Lỗi khi phát bài hát";
            }
        }

        /// <summary>
        /// Handle playlist view close request (switch to search view)
        /// </summary>
        private void PlaylistView_CloseRequested(object? sender, EventArgs e)
        {
            // Switch to search view
            ShowSearchView();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            // Play/Pause button - toggle play/pause
            if (mediaPlayer.Source != null)
            {
                if (_isPlaying)
                {
                    mediaPlayer.Pause();
                    _isPlaying = false;
                }
                else
                {
                    mediaPlayer.Play();
                    _isPlaying = true;
                }
            }
            else
            {
                MessageBox.Show("Vui lòng chọn bài hát để phát.", "Chưa chọn bài hát", 
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
    }
}
