using Group1.ApiClient;
using Group1.MusicApp.Models;
using Group1.MusicApp.ViewModels;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;

namespace Group1.MusicApp
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private MusicAPI _musicApi;
        private TrackDetailViewModel _viewModel;

        public MainWindow()
        {
            InitializeComponent();

            // Load environment variables from .env file
            try
            {
                DotNetEnv.Env.Load();
            }
            catch
            {
                // .env file might not exist, will use fallback values
            }

            // Get credentials from environment variables
            string clientId = DotNetEnv.Env.GetString("SPOTIFY_CLIENT_ID", "YOUR_CLIENT_ID");
            string clientSecret = DotNetEnv.Env.GetString("SPOTIFY_CLIENT_SECRET", "YOUR_CLIENT_SECRET");

            if (clientId == "YOUR_CLIENT_ID" || clientSecret == "YOUR_CLIENT_SECRET")
            {
                MessageBox.Show(
                    "Please configure your Spotify API credentials in the .env file.\n\n" +
                    "See SETUP_CREDENTIALS.md for instructions.",
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

        /// <summary>
        /// Handle search button click
        /// </summary>
        private async void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            await PerformSearch();
        }

        /// <summary>
        /// Handle Enter key in search box
        /// </summary>
        private async void SearchBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                await PerformSearch();
            }
        }

        /// <summary>
        /// Perform search and display results
        /// </summary>
        private async System.Threading.Tasks.Task PerformSearch()
        {
            string? query = SearchBox?.Text?.Trim();

            if (string.IsNullOrEmpty(query))
            {
                MessageBox.Show("Please enter a search term", "Search", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                if (StatusText != null) StatusText.Text = "Searching...";
                if (SearchResultsList != null) SearchResultsList.ItemsSource = null;

                // Search for tracks
                List<Track> results = await _viewModel.SearchTracksAsync(query, 20);

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
        private async void SearchResultsList_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (SearchResultsList.SelectedItem is Track selectedTrack)
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
            if (lstPlaylist.SelectedItem is System.Windows.Controls.ListBoxItem selectedItem)
            {
                string content = selectedItem.Content?.ToString() ?? "";
                if (content.Contains("Favorites"))
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
        /// Handle track play request from playlist
        /// </summary>
        private async void PlaylistView_TrackPlayRequested(object? sender, string trackId)
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
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to load track: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                if (StatusText != null) StatusText.Text = "Failed to load track";
            }
        }
    }
}