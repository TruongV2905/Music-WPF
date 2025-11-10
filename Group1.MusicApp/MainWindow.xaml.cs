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
            lblNowPlaying.Text = "Ready to play 🎧";
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
            string query = txtSearch.Text?.Trim() ?? "";

            if (string.IsNullOrEmpty(query))
            {
                MessageBox.Show("Please enter a search term.", "Search", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                lstTracks.ItemsSource = null;
                lblNowPlaying.Text = "Searching...";

                List<Track> results = await _viewModel.SearchTracksAsync(query, 20);
                //List<Track> results = await _viewModel.SearchTracksWithImagesAsync(query, 20);

                if (results.Count == 0)
                {
                    MessageBox.Show("No results found.", "Search", MessageBoxButton.OK, MessageBoxImage.Information);
                    lblNowPlaying.Text = "No results found.";
                    return;
                }

                _currentTracks = results;
                lstTracks.ItemsSource = _currentTracks;
                lblNowPlaying.Text = $"Found {results.Count} tracks 🎵";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Search failed: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                lblNowPlaying.Text = "Search failed ❌";
            }
        }

        private async void lstTracks_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (lstTracks.SelectedItem is Track selectedTrack)
            {
                await PlayTrack(selectedTrack);
            }
        }

        private async Task PlayTrack(Track track)
        {
            try
            {
                lblNowPlaying.Text = "Loading track...";
                var fullTrackDetails = await _viewModel.GetTrackDetailsAsync(track.Id);

                lblNowPlaying.Text = fullTrackDetails.Name;
                lblArtist.Text = fullTrackDetails.ArtistName;

                if (!string.IsNullOrEmpty(fullTrackDetails.AlbumImageUrl))
                    imgCover.Source = new BitmapImage(new Uri(fullTrackDetails.AlbumImageUrl));

                if (!string.IsNullOrEmpty(fullTrackDetails.PreviewUrl))
                {
                    mediaPlayer.Source = new Uri(fullTrackDetails.PreviewUrl);
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
                MessageBox.Show($"Failed to load track: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                lblNowPlaying.Text = "Failed to play track.";
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (mediaPlayer.Source == null)
                return;

            if (mediaPlayer.CanPause)
            {
                mediaPlayer.Pause();
                lblNowPlaying.Text += " ⏸️";
            }
            else
            {
                mediaPlayer.Play();
                lblNowPlaying.Text = lblNowPlaying.Text.Replace(" ⏸️", " ▶️");
            }
        }
    }
}