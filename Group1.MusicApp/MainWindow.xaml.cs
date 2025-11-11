using Group1.ApiClient;
using Group1.MusicApp.Models;
using Group1.MusicApp.ViewModels;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
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
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // Window is loaded and ready
            if (StatusText != null)
            {
                StatusText.Text = "Ready - Search for music!";
            }
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
            string? query = SearchBox?.Text?.Trim();

            if (string.IsNullOrEmpty(query))
            {
                MessageBox.Show("Please enter a search term.", "Search", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                if (StatusText != null) StatusText.Text = "Searching...";
                if (SearchResultsList != null) SearchResultsList.ItemsSource = null;

                List<Track> results = await _viewModel.SearchTracksAsync(query, 20);
                //List<Track> results = await _viewModel.SearchTracksWithImagesAsync(query, 20);

                if (results.Count == 0)
                {
                    MessageBox.Show("No results found", "Search", MessageBoxButton.OK, MessageBoxImage.Information);
                    if (StatusText != null) StatusText.Text = "No results found";
                    return;
                }

                // Display results
                if (SearchResultsList != null) SearchResultsList.ItemsSource = results;
                if (SearchResultsPanel != null) SearchResultsPanel.Visibility = Visibility.Visible;
                if (StatusText != null) StatusText.Text = $"Found {results.Count} tracks";

                // Hide welcome panel
                if (WelcomePanel != null) WelcomePanel.Visibility = Visibility.Collapsed;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Search failed: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                if (StatusText != null) StatusText.Text = "Search failed";
            }
        }

        private async void lstTracks_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (lstTracks.SelectedItem is Track selectedTrack)
            {
                try
                {
                    if (StatusText != null) StatusText.Text = "Loading track details...";

                    // Fetch full track details with audio features
                    var fullTrackDetails = await _viewModel.GetTrackDetailsAsync(selectedTrack.Id);

                    // Show track detail view
                    ShowTrackDetailView();
                    if (TrackDetailView != null) TrackDetailView.LoadTrack(fullTrackDetails);

                    if (StatusText != null) StatusText.Text = "Track loaded";
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to load track details: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    if (StatusText != null) StatusText.Text = "Failed to load track";
                }
            }
        }

        /// <summary>
        /// Handle Playlist selection from sidebar
        /// </summary>
        private void lstPlaylist_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (lstPlaylist?.SelectedItem is System.Windows.Controls.ListBoxItem selectedItem)
            {
                lblNowPlaying.Text = "Loading track...";
                var fullTrackDetails = await _viewModel.GetTrackDetailsAsync(track.Id);

                lblNowPlaying.Text = fullTrackDetails.Name;
                lblArtist.Text = fullTrackDetails.ArtistName;

                if (!string.IsNullOrEmpty(fullTrackDetails.AlbumImageUrl))
                    imgCover.Source = new BitmapImage(new Uri(fullTrackDetails.AlbumImageUrl));

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
                    lblEnergy.Text = "";
                    lblDanceability.Text = "";
                    lblValence.Text = "";
                    lblAcousticness.Text = "";
                    audioFeaturesPanel.Visibility = Visibility.Collapsed;
                }

                if (!string.IsNullOrEmpty(fullTrackDetails.PreviewUrl))
                {
                    ShowPlaylistView();
                }
                // Có thể thêm các xử lý khác cho Favorites, Recently Played, AI Mood Mix ở đây
                else if (content.Contains("Favorites"))
                {
                    // TODO: Xử lý Favorites nếu cần
                    ShowPlaylistView(); // Tạm thời dùng PlaylistView
                }
            }
        }

        /// <summary>
        /// Handle Playlist button click (kept for compatibility)
        /// </summary>
        private void PlaylistButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                lblNowPlaying.Text = "Loading track...";
                var fullTrackDetails = await _viewModel.GetTrackDetailsAsync(track.Id);

        /// <summary>
        /// Show playlist view and hide other views
        /// </summary>
        private void ShowPlaylistView()
        {
            if (PlaylistView != null)
            {
                PlaylistView.Visibility = Visibility.Visible;
                PlaylistView.Refresh();
            }
            if (TrackDetailView != null) TrackDetailView.Visibility = Visibility.Collapsed;
            if (WelcomePanel != null) WelcomePanel.Visibility = Visibility.Collapsed;
            if (SearchResultsPanel != null) SearchResultsPanel.Visibility = Visibility.Collapsed;
            if (StatusText != null) StatusText.Text = "My Playlist";
        }

        /// <summary>
        /// Show track detail view and hide other views
        /// </summary>
        private void ShowTrackDetailView()
        {
            if (TrackDetailView != null) TrackDetailView.Visibility = Visibility.Visible;
            if (PlaylistView != null) PlaylistView.Visibility = Visibility.Collapsed;
            if (WelcomePanel != null) WelcomePanel.Visibility = Visibility.Collapsed;
            if (SearchResultsPanel != null) SearchResultsPanel.Visibility = Visibility.Collapsed;
        }

        /// <summary>
        /// Handle track added to playlist event
        /// </summary>
        private void TrackDetailView_TrackAddedToPlaylist(object? sender, Track track)
        {
            // Refresh playlist view if it's visible
            if (PlaylistView != null && PlaylistView.Visibility == Visibility.Visible)
            {
                MessageBox.Show($"Failed to load track: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                lblNowPlaying.Text = "Failed to play track.";
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (StatusText != null) StatusText.Text = "Loading track...";

                // Fetch full track details with audio features
                var fullTrackDetails = await _viewModel.GetTrackDetailsAsync(trackId);

                // Show track detail view
                ShowTrackDetailView();
                if (TrackDetailView != null) TrackDetailView.LoadTrack(fullTrackDetails);

                if (StatusText != null) StatusText.Text = "Track loaded";
            }
            else
            {
                MessageBox.Show($"Failed to load track: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                if (StatusText != null) StatusText.Text = "Failed to load track";
            }
        }

        /// <summary>
        /// Handle close playlist request
        /// </summary>
        private void PlaylistView_CloseRequested(object? sender, EventArgs e)
        {
            // Ẩn PlaylistView
            if (PlaylistView != null) PlaylistView.Visibility = Visibility.Collapsed;

            // Hiển thị lại WelcomePanel hoặc SearchResultsPanel
            if (SearchResultsPanel != null && SearchResultsPanel.Visibility == Visibility.Visible)
            {
                // Nếu đang có kết quả tìm kiếm, giữ nguyên
                return;
            }
            else
            {
                // Nếu không có kết quả tìm kiếm, hiển thị WelcomePanel
                if (WelcomePanel != null) WelcomePanel.Visibility = Visibility.Visible;
            }

            // Cập nhật status
            if (StatusText != null) StatusText.Text = "Ready - Search for music!";

            // Bỏ chọn trong sidebar
            if (lstPlaylist != null) lstPlaylist.SelectedItem = null;
        }
    }
}