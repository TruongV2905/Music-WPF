using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows.Media;

namespace Group1.MusicApp.Utilities
{
    /// <summary>
    /// Extracts dominant colors from album artwork for adaptive UI theming
    /// </summary>
    public class ColorExtractor
    {
        private static readonly HttpClient _httpClient = new HttpClient();

        /// <summary>
        /// Extract dominant color from image URL
        /// </summary>
        public static async Task<System.Windows.Media.Color> GetDominantColorAsync(string imageUrl)
        {
            try
            {
                // Download image
                var imageBytes = await _httpClient.GetByteArrayAsync(imageUrl);
                using var ms = new MemoryStream(imageBytes);
                using var bitmap = new Bitmap(ms);

                // Get dominant color
                var dominantColor = AnalyzeDominantColor(bitmap);

                // Convert System.Drawing.Color to System.Windows.Media.Color
                return System.Windows.Media.Color.FromArgb(
                    dominantColor.A,
                    dominantColor.R,
                    dominantColor.G,
                    dominantColor.B
                );
            }
            catch
            {
                // Fallback to default color
                return System.Windows.Media.Color.FromRgb(33, 150, 243); // Material Blue
            }
        }

        /// <summary>
        /// Analyze bitmap to find dominant color using simplified color thief algorithm
        /// </summary>
        private static System.Drawing.Color AnalyzeDominantColor(Bitmap bitmap)
        {
            // Resize for performance
            int sampleSize = 10;
            var colorCounts = new Dictionary<int, int>();

            // Sample colors from the image
            for (int x = 0; x < bitmap.Width; x += sampleSize)
            {
                for (int y = 0; y < bitmap.Height; y += sampleSize)
                {
                    var pixel = bitmap.GetPixel(x, y);

                    // Skip very dark or very light colors
                    var brightness = pixel.GetBrightness();
                    if (brightness < 0.2f || brightness > 0.9f)
                        continue;

                    // Quantize color to reduce variations
                    int r = (pixel.R / 32) * 32;
                    int g = (pixel.G / 32) * 32;
                    int b = (pixel.B / 32) * 32;
                    int colorKey = (r << 16) | (g << 8) | b;

                    if (colorCounts.ContainsKey(colorKey))
                        colorCounts[colorKey]++;
                    else
                        colorCounts[colorKey] = 1;
                }
            }

            // Find most common color
            if (colorCounts.Count > 0)
            {
                var dominantColorKey = colorCounts.OrderByDescending(x => x.Value).First().Key;
                int r = (dominantColorKey >> 16) & 0xFF;
                int g = (dominantColorKey >> 8) & 0xFF;
                int b = dominantColorKey & 0xFF;
                return System.Drawing.Color.FromArgb(r, g, b);
            }

            return System.Drawing.Color.FromArgb(33, 150, 243);
        }

        /// <summary>
        /// Create gradient colors from dominant color
        /// </summary>
        public static (System.Windows.Media.Color start, System.Windows.Media.Color end) CreateGradient(System.Windows.Media.Color baseColor)
        {
            // Start: Slightly lighter
            var start = System.Windows.Media.Color.FromRgb(
                (byte)Math.Min(255, baseColor.R + 30),
                (byte)Math.Min(255, baseColor.G + 30),
                (byte)Math.Min(255, baseColor.B + 30)
            );

            // End: Slightly darker
            var end = System.Windows.Media.Color.FromRgb(
                (byte)Math.Max(0, baseColor.R - 40),
                (byte)Math.Max(0, baseColor.G - 40),
                (byte)Math.Max(0, baseColor.B - 40)
            );

            return (start, end);
        }

        /// <summary>
        /// Determine if text should be white or black based on background color
        /// </summary>
        public static System.Windows.Media.Color GetTextColor(System.Windows.Media.Color backgroundColor)
        {
            // Calculate luminance
            double luminance = (0.299 * backgroundColor.R + 0.587 * backgroundColor.G + 0.114 * backgroundColor.B) / 255;

            // Return white for dark backgrounds, black for light backgrounds
            return luminance > 0.5
                ? System.Windows.Media.Color.FromRgb(0, 0, 0)      // Black
                : System.Windows.Media.Color.FromRgb(255, 255, 255); // White
        }
    }
}
