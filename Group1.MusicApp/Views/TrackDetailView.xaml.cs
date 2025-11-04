using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using Group1.MusicApp.Models;
using Group1.MusicApp.Utilities;

namespace Group1.MusicApp.Views
{
    public partial class TrackDetailView : UserControl
    {
        public TrackDetailView()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Load and display track information with animations
        /// </summary>
        public async void LoadTrack(Track track)
        {
            if (track == null) return;

            // Show loading
            LoadingPanel.Visibility = Visibility.Visible;
            AudioFeaturesSection.Visibility = Visibility.Collapsed;
            GenresSection.Visibility = Visibility.Collapsed;

            try
            {
                // Update basic info
                TrackTitle.Text = track.Name;
                ArtistName.Text = track.ArtistName;
                AlbumName.Text = track.AlbumName;
                Duration.Text = track.Duration;
                Popularity.Text = track.PopularityText;
                ReleaseDate.Text = track.ReleaseDateText;

                // Load album image
                if (!string.IsNullOrEmpty(track.AlbumImageUrl))
                {
                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.UriSource = new Uri(track.AlbumImageUrl);
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.EndInit();
                    AlbumImage.Source = bitmap;
                    ImagePlaceholder.Visibility = Visibility.Collapsed;

                    // Extract and apply dominant color
                    await ApplyDominantColorAsync(track.AlbumImageUrl);
                }

                // Show genres if available
                if (track.Genres != null && track.Genres.Count > 0)
                {
                    GenresList.ItemsSource = track.Genres;
                    GenresSection.Visibility = Visibility.Visible;
                }

                // Show audio features if available
                if (track.AudioFeatures != null)
                {
                    AnimateAudioFeature(EnergyBar, EnergyValue, track.AudioFeatures.EnergyPercent);
                    AnimateAudioFeature(DanceabilityBar, DanceabilityValue, track.AudioFeatures.DanceabilityPercent);
                    AnimateAudioFeature(ValenceBar, ValenceValue, track.AudioFeatures.ValencePercent);
                    AnimateAudioFeature(AcousticnessBar, AcousticnessValue, track.AudioFeatures.AcousticnessPercent);
                    AudioFeaturesSection.Visibility = Visibility.Visible;
                }

                // Hide loading
                LoadingPanel.Visibility = Visibility.Collapsed;

                // Animate entrance
                AnimateEntrance();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading track: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                LoadingPanel.Visibility = Visibility.Collapsed;
            }
        }

        /// <summary>
        /// Extract dominant color from album art and apply to background
        /// </summary>
        private async System.Threading.Tasks.Task ApplyDominantColorAsync(string imageUrl)
        {
            try
            {
                var dominantColor = await ColorExtractor.GetDominantColorAsync(imageUrl);
                var (startColor, endColor) = ColorExtractor.CreateGradient(dominantColor);

                // Animate color transition
                var startAnimation = new ColorAnimation
                {
                    To = startColor,
                    Duration = TimeSpan.FromSeconds(1),
                    EasingFunction = new CubicEase { EasingMode = EasingMode.EaseInOut }
                };

                var endAnimation = new ColorAnimation
                {
                    To = endColor,
                    Duration = TimeSpan.FromSeconds(1),
                    EasingFunction = new CubicEase { EasingMode = EasingMode.EaseInOut }
                };

                GradientStart.BeginAnimation(GradientStop.ColorProperty, startAnimation);
                GradientEnd.BeginAnimation(GradientStop.ColorProperty, endAnimation);
            }
            catch
            {
                // Ignore color extraction errors
            }
        }

        /// <summary>
        /// Animate audio feature progress bar
        /// </summary>
        private void AnimateAudioFeature(ProgressBar progressBar, TextBlock valueText, int targetValue)
        {
            var animation = new DoubleAnimation
            {
                From = 0,
                To = targetValue,
                Duration = TimeSpan.FromSeconds(1.5),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };

            animation.Completed += (s, e) =>
            {
                valueText.Text = $"{targetValue}%";
            };

            progressBar.BeginAnimation(ProgressBar.ValueProperty, animation);
        }

        /// <summary>
        /// Animate entrance of the view
        /// </summary>
        private void AnimateEntrance()
        {
            // Fade in animation
            var fadeIn = new DoubleAnimation
            {
                From = 0,
                To = 1,
                Duration = TimeSpan.FromSeconds(0.5),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };

            // Slide up animation
            var slideUp = new ThicknessAnimation
            {
                From = new Thickness(0, 20, 0, 0),
                To = new Thickness(0, 0, 0, 0),
                Duration = TimeSpan.FromSeconds(0.6),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };

            RootGrid.BeginAnimation(OpacityProperty, fadeIn);
            RootGrid.BeginAnimation(MarginProperty, slideUp);
        }
    }
}
