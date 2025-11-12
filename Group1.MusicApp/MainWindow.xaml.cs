<<<<<<< Updated upstream
﻿using System;
=======
﻿using Group1.MusicApp.Models;
using Group1.MusicApp.Services;
using System;
>>>>>>> Stashed changes
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
<<<<<<< Updated upstream
using Group1.ApiClient;
using Group1.MusicApp.Models;
using Group1.MusicApp.ViewModels;
=======
>>>>>>> Stashed changes

namespace Group1.MusicApp
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
<<<<<<< Updated upstream
        private MusicAPI _musicApi;
        private TrackDetailViewModel _viewModel;
        private List<Track> _currentTracks = new();
=======
        private readonly ITunesService _itunes = new();
        private readonly LyricsService _lyricsService = new();
        private readonly PlaylistService _playlistService = new();

        private bool _isPlaying = false;
        private string _currentQuery = "";
        private int _currentOffset = 0;
        private bool _isLoadingMore = false;
        private readonly List<Track> _currentTracks = new();
        private Track? _currentTrackPlaying = null;
>>>>>>> Stashed changes

        // Progress
        private readonly DispatcherTimer _progressTimer = new();

        // Volume / mute
        private bool _isMuted = false;
        private double _lastVolume01 = 0.8; // nhớ volume trước khi mute (0..1)

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
<<<<<<< Updated upstream
            lblNowPlaying.Text = "🎵 Đang phát file local...";
=======
            lblNowPlaying.Text = "🎧 Enjoy my music!";

            // Ẩn thanh player cho đến khi chọn bài
            if (BottomPlayerBar != null)
                BottomPlayerBar.Visibility = Visibility.Collapsed;

            // Playlist view
            if (PlaylistViewControl != null)
            {
                PlaylistViewControl.Refresh();
                PlaylistViewControl.TrackPlayRequested += PlaylistView_TrackPlayRequested;
                PlaylistViewControl.CloseRequested += PlaylistView_CloseRequested;
            }

            // Vô hạn scroll list
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
                    lblElapsed.Text = FormatTime(mediaPlayer.Position);
                }
            };

            // Media events
            mediaPlayer.MediaOpened += (s, ev) =>
            {
                double totalSeconds = 30;
                if (mediaPlayer.NaturalDuration.HasTimeSpan)
                    totalSeconds = mediaPlayer.NaturalDuration.TimeSpan.TotalSeconds;

                progressPreview.Maximum = totalSeconds;
                progressPreview.Value = 0;

                lblElapsed.Text = "0:00";
                lblTotal.Text = FormatTime(TimeSpan.FromSeconds(totalSeconds));
            };

            mediaPlayer.MediaEnded += (s, ev) =>
            {
                SetPlayState(false);
                progressPreview.Value = progressPreview.Maximum;
                lblNowPlaying.Text = "Đã phát xong.";
            };

            mediaPlayer.MediaFailed += (s, ev) =>
            {
                SetPlayState(false);
                MessageBox.Show("Không phát được audio.", "Media Failed",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            };

            // Volume mặc định
            sldVolume.Value = 80;
            mediaPlayer.Volume = sldVolume.Value / 100.0;
            _lastVolume01 = mediaPlayer.Volume;
            UpdateVolumeIcon();
            UpdatePlayButtonIcon();
>>>>>>> Stashed changes
        }

        private async void btnSearch_Click(object sender, RoutedEventArgs e)
        {
            await PerformSearch();
        }

<<<<<<< Updated upstream
        private async void txtSearch_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
                await PerformSearch();
        }
=======
        // ===== SEARCH =====
        private async void btnSearch_Click(object sender, RoutedEventArgs e) => await PerformSearch();
        private async void txtSearch_KeyDown(object sender, KeyEventArgs e) { if (e.Key == Key.Enter) await PerformSearch(); }
>>>>>>> Stashed changes

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
<<<<<<< Updated upstream
                lblNowPlaying.Text = $"Found {results.Count} tracks 🎵";
=======
                lblNowPlaying.Text = $"Tìm thấy {_currentTracks.Count} bài hát (iTunes)";
                ShowSearchView();
>>>>>>> Stashed changes
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Search failed: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                lblNowPlaying.Text = "Search failed ❌";
            }
        }

<<<<<<< Updated upstream
        // hiển thị lyric
        private async void lstTracks_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (lstTracks.SelectedItem is Track selected)
            {
                lblNowPlaying.Text = selected.Name;
                lblArtist.Text = selected.ArtistName;
=======
        private async void ScrollViewer_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            if (_isLoadingMore || string.IsNullOrEmpty(_currentQuery)) return;
            var sv = sender as ScrollViewer;
            if (sv == null) return;

            if (sv.VerticalOffset + sv.ViewportHeight >= sv.ExtentHeight - 50)
            {
                _isLoadingMore = true;
                _currentOffset += 20;
                lblNowPlaying.Text = "Đang tải thêm...";

                try
                {
                    var moreTracks = await _itunes.SearchTracksAsync(_currentQuery, limit: 20, offset: _currentOffset);
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

            // Hiện thanh player khi người dùng chọn bài
            if (BottomPlayerBar.Visibility != Visibility.Visible)
                BottomPlayerBar.Visibility = Visibility.Visible;

            await PlaySelectedTrackAsync(selected);
        }

        private async Task PlaySelectedTrackAsync(Track selected)
        {
            _currentTrackPlaying = selected;
            lblNowPlaying.Text = selected.Name ?? "Đang tải bài hát...";
            lblArtist.Text = selected.ArtistName ?? "";

            if (!string.IsNullOrEmpty(selected.AlbumImageUrl))
>>>>>>> Stashed changes
                imgCover.Source = new BitmapImage(new Uri(selected.AlbumImageUrl));
                mediaPlayer.Source = new Uri(selected.PreviewUrl);
                mediaPlayer.Play();

<<<<<<< Updated upstream
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
=======
            try
            {
                if (!string.IsNullOrEmpty(selected.PreviewUrl))
                {
                    mediaPlayer.Source = new Uri(selected.PreviewUrl);
                    mediaPlayer.Play();
                    _progressTimer.Start();
                    SetPlayState(true);
                }
                else
                {
                    lblNowPlaying.Text = $"{selected.Name} (Không có preview)";
                    SetPlayState(false);
                }

                await LoadPlainLyricsAsync(selected.ArtistName ?? "", selected.Name ?? "");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi phát: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task LoadPlainLyricsAsync(string artist, string title)
        {
            svLyrics.Visibility = Visibility.Collapsed;
            svPlainLyrics.Visibility = Visibility.Visible;
            txtLyrics.Text = "Đang tải lời bài hát...";

            var result = await _lyricsService.GetSyncedAsync(artist, title);
            if (!string.IsNullOrWhiteSpace(result.Plain))
                txtLyrics.Text = result.Plain;
            else
                txtLyrics.Text = "🎶 Chưa có lời bài hát.";

            svPlainLyrics.UpdateLayout();
            svPlainLyrics.ScrollToTop();
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
                mediaPlayer.Source = new Uri(_currentTrackPlaying.PreviewUrl);

>>>>>>> Stashed changes
            if (mediaPlayer.Source == null)
                return;

<<<<<<< Updated upstream
            if (mediaPlayer.CanPause)
            {
                mediaPlayer.Pause();
                lblNowPlaying.Text += "⏸️";
=======
            if (_isPlaying)
            {
                mediaPlayer.Pause();
                _progressTimer.Stop();
                SetPlayState(false);
            }
            else
            {
                mediaPlayer.Play();
                _progressTimer.Start();
                SetPlayState(true);
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

                if (BottomPlayerBar.Visibility != Visibility.Visible)
                    BottomPlayerBar.Visibility = Visibility.Visible;

                ShowSearchView();
                await PlaySelectedTrackAsync(t);
>>>>>>> Stashed changes
            }
            else
            {
                mediaPlayer.Play();
                lblNowPlaying.Text = lblNowPlaying.Text.Replace("⏸️", "▶️");
            }
        }

<<<<<<< Updated upstream


=======
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
            PlaylistViewControl.Refresh();
        }

        // ===== Volume / Mute =====
        private void sldVolume_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            // kéo slider thì coi như unmute
            _isMuted = false;
            mediaPlayer.Volume = sldVolume.Value / 100.0;
            _lastVolume01 = mediaPlayer.Volume;
            UpdateVolumeIcon();
        }

        private void btnVolume_Click(object sender, RoutedEventArgs e)
        {
            if (_isMuted || mediaPlayer.Volume == 0 || sldVolume.Value == 0)
            {
                // unmute -> khôi phục
                _isMuted = false;
                var restore = _lastVolume01 > 0 ? _lastVolume01 : 0.5;
                mediaPlayer.Volume = restore;
                sldVolume.Value = restore * 100;
            }
            else
            {
                // mute
                _isMuted = true;
                _lastVolume01 = mediaPlayer.Volume;
                mediaPlayer.Volume = 0;
                sldVolume.Value = 0;
            }
            UpdateVolumeIcon();
        }

        private void UpdateVolumeIcon()
        {
            double v = sldVolume.Value;
            if (v <= 0 || _isMuted) btnVolume.Content = "🔇";
            else if (v < 40) btnVolume.Content = "🔈";
            else btnVolume.Content = "🔊";
            btnVolume.ToolTip = _isMuted || v == 0 ? "Unmute" : "Mute";
        }

        // ===== Play state helpers =====
        private void SetPlayState(bool playing)
        {
            _isPlaying = playing;
            UpdatePlayButtonIcon();
            if (!playing) _progressTimer.Stop();
        }

        private void UpdatePlayButtonIcon()
        {
            btnPlayPause.Content = _isPlaying ? "⏸" : "▶";
            btnPlayPause.ToolTip = _isPlaying ? "Pause" : "Play";
        }

        private static string FormatTime(TimeSpan ts)
        {
            if (ts.TotalHours >= 1)
                return ts.ToString(@"h\:mm\:ss");
            return ts.ToString(@"m\:ss");
        }
>>>>>>> Stashed changes
    }
}