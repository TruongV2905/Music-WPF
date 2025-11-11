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

        /// <summary>
        /// Handle track selection from search results
        /// </summary>
        private async void SearchResultsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (SearchResultsList.SelectedItem is Track selectedTrack)
            {
                try
                {
                    lblNowPlaying.Text = "Loading track details...";

                    // Fetch full track details with audio features
                    var fullTrackDetails = await _viewModel.GetTrackDetailsAsync(selectedTrack.Id);

                    // Update player UI with metadata
                    await PlayTrack(fullTrackDetails);

                    lblNowPlaying.Text = "Track loaded";
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to load track details: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    lblNowPlaying.Text = "Failed to load track";
                }
            }
        }

        /// <summary>
        /// Play a track and update UI with metadata
        /// </summary>
        private async Task PlayTrack(Track track)
        {
            try
            {
                lblNowPlaying.Text = track.Name;
                lblArtist.Text = track.ArtistName;

                if (!string.IsNullOrEmpty(track.AlbumImageUrl))
                    imgCover.Source = new BitmapImage(new Uri(track.AlbumImageUrl));

                // Update metadata
                lblDuration.Text = track.Duration;
                lblPopularity.Text = track.PopularityText;
                lblReleaseDate.Text = track.ReleaseDateText;
                lblAlbum.Text = track.AlbumName;
                metadataPanel.Visibility = Visibility.Visible;

                // Update audio features if available
                if (track.AudioFeatures != null)
                {
                    lblEnergy.Text = $"{track.AudioFeatures.EnergyPercent}%";
                    lblDanceability.Text = $"{track.AudioFeatures.DanceabilityPercent}%";
                    lblValence.Text = $"{track.AudioFeatures.ValencePercent}%";
                    lblAcousticness.Text = $"{track.AudioFeatures.AcousticnessPercent}%";
                    audioFeaturesPanel.Visibility = Visibility.Visible;
                }
                else
                {
                    audioFeaturesPanel.Visibility = Visibility.Collapsed;
                }

                if (!string.IsNullOrEmpty(track.PreviewUrl))
                {
                    mediaPlayer.Source = new Uri(track.PreviewUrl);
                    mediaPlayer.Play();
                }
                else
                {
                    MessageBox.Show("This track has no preview available.", "No Preview", MessageBoxButton.OK, MessageBoxImage.Information);
                }

                txtLyrics.Text = "🎵 Lyrics are not available for this song.";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to play track: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Handle Playlist selection from sidebar
        /// </summary>
        private void AddToPlaylistButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is Track track)
            {
                string content = selectedItem.Content?.ToString() ?? "";

                if (content.Contains("My Playlist") || content.Contains("Favorites") || content.Contains("Recently Played") || content.Contains("AI Mood Mix"))
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
            }
        }

        /// <summary>
        /// Handle track play request from playlist
        /// </summary>
        private async void PlaylistView_TrackPlayRequested(object? sender, string trackId)
        {
            ShowPlaylistView();
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
                PlaylistView.Refresh();
            }
        }

        /// <summary>
        /// Handle play/pause button click
        /// </summary>
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (mediaPlayer.Source == null)
                return;

            if (mediaPlayer.CanPause)
            {
                mediaPlayer.Pause();
            }
            else
            {
                mediaPlayer.Play();
            }
        }

        /// <summary>
        /// Handle track play request from playlist view
        /// </summary>
        private async void PlaylistView_TrackPlayRequested(object? sender, string trackId)
        {
            try
            {
                if (StatusText != null) StatusText.Text = "Loading track...";

                // Fetch full track details with audio features
                var fullTrackDetails = await _viewModel.GetTrackDetailsAsync(trackId);

                // Update player UI with metadata
                await PlayTrack(fullTrackDetails);

                if (StatusText != null) StatusText.Text = "Playing";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to load track: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                if (StatusText != null) StatusText.Text = "Failed to load track";
            }
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