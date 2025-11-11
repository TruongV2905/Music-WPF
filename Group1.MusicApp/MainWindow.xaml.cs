using Group1.ApiClient;
using Group1.MusicApp.Models;
using Group1.MusicApp.Services;
using Group1.MusicApp.ViewModels;
using System;
using System.Collections.Generic;
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
        private List<Track> _currentTracks = new();
        private PlaylistService _playlistService;
        private int _currentOffset = 0;
        private string _currentQuery = "";
        private bool _isLoadingMore = false;

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
            
            // Refresh playlist view
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
            var scrollViewer = sender as ScrollViewer;

            // Nếu đang loading thì bỏ qua
            if (_isLoadingMore) return;

            // Nếu chạm gần cuối (ví dụ còn 50px)
            if (scrollViewer.VerticalOffset + scrollViewer.ViewportHeight >= scrollViewer.ExtentHeight - 50)
            {
                _isLoadingMore = true;
                _currentOffset += 20;

                try
                {
                    var moreTracks = await _viewModel.SearchTracksAsync(_currentQuery, 20, _currentOffset);
                    if (moreTracks.Count > 0)
                    {
                        _currentTracks.AddRange(moreTracks);
                        lstTracks.ItemsSource = null;
                        lstTracks.ItemsSource = _currentTracks;
                    }
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
                if (child is T childOfType)
                    return childOfType;

                var childOfChild = FindVisualChild<T>(child);
                if (childOfChild != null)
                    return childOfChild;
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
                await PerformSearch();
        }

        private async Task PerformSearch()
        {
            _currentQuery = txtSearch.Text?.Trim();

            if (string.IsNullOrEmpty(_currentQuery))
            {
                MessageBox.Show("Please enter a search term.", "Search", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                lblNowPlaying.Text = "Searching...";
                lstTracks.ItemsSource = null;
                _currentTracks.Clear();
                _currentOffset = 0;

                // Lấy kết quả đầu tiên
                var results = await _viewModel.SearchTracksAsync(_currentQuery, 20, _currentOffset);

                if (results == null || results.Count == 0)
                {
                    MessageBox.Show("No results found.", "Search", MessageBoxButton.OK, MessageBoxImage.Information);
                    lblNowPlaying.Text = "No results found";
                    return;
                }

                // Lưu và hiển thị kết quả
                _currentTracks.AddRange(results);
                lstTracks.ItemsSource = _currentTracks;
                lblNowPlaying.Text = $"Found {_currentTracks.Count} tracks";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Search failed: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                lblNowPlaying.Text = "Search failed";
            }
        }

        private async void lstTracks_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (lstTracks.SelectedItem is Track selectedTrack)
            {
                try
                {
                    lblNowPlaying.Text = "Loading track details...";

                    // Fetch full track details with audio features
                    var fullTrackDetails = await _viewModel.GetTrackDetailsAsync(selectedTrack.Id);

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

                    // Play preview if available
                    if (!string.IsNullOrEmpty(fullTrackDetails.PreviewUrl))
                    {
                        mediaPlayer.Source = new Uri(fullTrackDetails.PreviewUrl);
                        mediaPlayer.Play();
                    }

                    txtLyrics.Text = "🎵 Lyrics are not available for this song.";
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to load track details: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    lblNowPlaying.Text = "Failed to load track";
                }
            }
        }

        /// <summary>
        /// Handle adding track to playlist from search results
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
                    }
                    catch
                    {
                        MessageBox.Show("Không thể phát preview của bài hát này.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }

                txtLyrics.Text = "🎵 Lyrics are not available for this song.";
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

        /// <summary>
        /// Show search view - Module độc lập
        /// </summary>
        private void ShowSearchView()
        {
            if (SearchResultsContainer != null)
                SearchResultsContainer.Visibility = Visibility.Visible;
            if (PlaylistViewControl != null)
                PlaylistViewControl.Visibility = Visibility.Collapsed;
        }

        /// <summary>
        /// Show playlist view - Module độc lập
        /// </summary>
        private void ShowPlaylistView()
        {
            if (SearchResultsContainer != null)
                SearchResultsContainer.Visibility = Visibility.Collapsed;
            if (PlaylistViewControl != null)
            {
                PlaylistViewControl.Visibility = Visibility.Visible;
                PlaylistViewControl.Refresh();
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

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            if (lstTracks.SelectedItem is Track selectedTrack)
            {
                try
                {
                    lblNowPlaying.Text = "Loading track...";

                    // Fetch full track details with audio features
                    var fullTrackDetails = await _viewModel.GetTrackDetailsAsync(selectedTrack.Id);

                    // Update UI with track info
                    lblNowPlaying.Text = fullTrackDetails.Name;
                    lblArtist.Text = fullTrackDetails.ArtistName;
                    
                    if (!string.IsNullOrEmpty(fullTrackDetails.AlbumImageUrl))
                    {
                        imgCover.Source = new BitmapImage(new Uri(fullTrackDetails.AlbumImageUrl));
                    }

                    lblNowPlaying.Text = "Track loaded";
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to load track: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    lblNowPlaying.Text = "Failed to load track";
                }
            }
            else
            {
                MessageBox.Show("Please select a track first.", "No Track Selected", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
    }
}