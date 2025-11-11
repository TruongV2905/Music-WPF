using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Group1.ApiClient;
using Group1.MusicApp.Models;
using Group1.MusicApp.ViewModels;

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

        private MediaPlayer _player = new MediaPlayer();

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            lblNowPlaying.Text = "🎵 Đang phát file local...";
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

        // hiển thị lyric
        private async void lstTracks_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (lstTracks.SelectedItem is Track selected)
            {
                lblNowPlaying.Text = selected.Name;
                lblArtist.Text = selected.ArtistName;
                imgCover.Source = new BitmapImage(new Uri(selected.AlbumImageUrl));
                mediaPlayer.Source = new Uri(selected.PreviewUrl);
                mediaPlayer.Play();

                // ✅ Gọi lyrics API
                txtLyrics.Text = "🎵 Đang tải lời bài hát...";
                var lyrics = await _musicApi.GetLyricsAsync(selected.ArtistName, selected.Name);
                txtLyrics.Text = lyrics;
            }
        }

        // lời chạy theo từng chữ
        DispatcherTimer lyricsTimer;
        string[] lyricLines;
        int currentLine = 0;
        private async Task PlayTrack(Track track)
        {
            // Phát nhạc
            mediaPlayer.Source = new Uri(track.PreviewUrl);
            mediaPlayer.Play();

            // Lấy lời bài hát
            var lyrics = await _musicApi.GetLyricsAsync(track.ArtistName, track.Name);
            lyricLines = lyrics.Split('\n', StringSplitOptions.RemoveEmptyEntries);

            currentLine = 0;
            txtLyrics.Text = lyricLines.Length > 0 ? lyricLines[0] : "🎶 Không có lời bài hát.";

            // Khởi tạo timer để cập nhật từng dòng
            lyricsTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(3) // mỗi dòng hiển thị 3s
            };
            lyricsTimer.Tick += LyricsTimer_Tick;
            lyricsTimer.Start();
        }
        private void LyricsTimer_Tick(object sender, EventArgs e)
        {
            if (lyricLines == null || currentLine >= lyricLines.Length - 1)
            {
                lyricsTimer.Stop();
                return;
            }

            currentLine++;
            txtLyrics.Text = lyricLines[currentLine];
        }
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (mediaPlayer.Source == null)
                return;

            if (mediaPlayer.CanPause)
            {
                mediaPlayer.Pause();
                lblNowPlaying.Text += "⏸️";
            }
            else
            {
                mediaPlayer.Play();
                lblNowPlaying.Text = lblNowPlaying.Text.Replace("⏸️", "▶️");
            }
        }



    }
}