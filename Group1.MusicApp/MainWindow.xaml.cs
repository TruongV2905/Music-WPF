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

        /// <summary>
        /// Handle track selection from search results
        /// </summary>
        private async void SearchResultsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (SearchResultsList.SelectedItem is Track selectedTrack)
            {
                try
                {
                    if (StatusText != null) StatusText.Text = "Loading track details...";

                    // Fetch full track details with audio features
                    var fullTrackDetails = await _viewModel.GetTrackDetailsAsync(selectedTrack.Id);

                    // Update player UI with metadata
                    await PlayTrack(fullTrackDetails);

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
        private void lstPlaylist_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (lstPlaylist?.SelectedItem is System.Windows.Controls.ListBoxItem selectedItem)
            {
                string content = selectedItem.Content?.ToString() ?? "";

                if (content.Contains("My Playlist") || content.Contains("Favorites") || content.Contains("Recently Played") || content.Contains("AI Mood Mix"))
                {
                    ShowPlaylistView();
                }
            }
        }

        /// <summary>
        /// Handle Playlist button click (kept for compatibility)
        /// </summary>
        private void PlaylistButton_Click(object sender, RoutedEventArgs e)
        {
            ShowPlaylistView();
        }

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