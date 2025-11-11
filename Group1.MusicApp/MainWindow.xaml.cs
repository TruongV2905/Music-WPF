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
            StatusText.Text = "Ready - Search for music!";
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
            string query = SearchBox.Text?.Trim();

            if (string.IsNullOrEmpty(query))
            {
                MessageBox.Show("Please enter a search term.", "Search", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                StatusText.Text = "Searching...";
                SearchResultsList.ItemsSource = null;

                List<Track> results = await _viewModel.SearchTracksAsync(query, 20);
                //List<Track> results = await _viewModel.SearchTracksWithImagesAsync(query, 20);

                if (results.Count == 0)
                {
                    MessageBox.Show("No results found", "Search", MessageBoxButton.OK, MessageBoxImage.Information);
                    StatusText.Text = "No results found";
                    return;
                }

                // Display results
                SearchResultsList.ItemsSource = results;
                SearchResultsPanel.Visibility = Visibility.Visible;
                StatusText.Text = $"Found {results.Count} tracks";

                // Hide welcome panel
                WelcomePanel.Visibility = Visibility.Collapsed;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Search failed: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                StatusText.Text = "Search failed";
            }
        }

        private async void lstTracks_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (lstTracks.SelectedItem is Track selectedTrack)
            {
                try
                {
                    StatusText.Text = "Loading track details...";

                    // Fetch full track details with audio features
                    var fullTrackDetails = await _viewModel.GetTrackDetailsAsync(selectedTrack.Id);

                    // Show track detail view
                    ShowTrackDetailView();
                    TrackDetailView.LoadTrack(fullTrackDetails);

                    StatusText.Text = "Track loaded";
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to load track details: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    StatusText.Text = "Failed to load track";
                }
            }
        }

        /// <summary>
        /// Handle Playlist button click
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
            PlaylistView.Visibility = Visibility.Visible;
            PlaylistView.Refresh();
            TrackDetailView.Visibility = Visibility.Collapsed;
            WelcomePanel.Visibility = Visibility.Collapsed;
            SearchResultsPanel.Visibility = Visibility.Collapsed;
            StatusText.Text = "My Playlist";
        }

        /// <summary>
        /// Show track detail view and hide other views
        /// </summary>
        private void ShowTrackDetailView()
        {
            TrackDetailView.Visibility = Visibility.Visible;
            PlaylistView.Visibility = Visibility.Collapsed;
            WelcomePanel.Visibility = Visibility.Collapsed;
        }

        /// <summary>
        /// Handle track added to playlist event
        /// </summary>
        private void TrackDetailView_TrackAddedToPlaylist(object? sender, Track track)
        {
            // Refresh playlist view if it's visible
            if (PlaylistView.Visibility == Visibility.Visible)
            {
                MessageBox.Show($"Failed to load track: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                lblNowPlaying.Text = "Failed to play track.";
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                StatusText.Text = "Loading track...";

                // Fetch full track details with audio features
                var fullTrackDetails = await _viewModel.GetTrackDetailsAsync(trackId);

                // Show track detail view
                ShowTrackDetailView();
                TrackDetailView.LoadTrack(fullTrackDetails);

                StatusText.Text = "Track loaded";
            }
            else
            {
                MessageBox.Show($"Failed to load track: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                StatusText.Text = "Failed to load track";
            }
        }
    }
}