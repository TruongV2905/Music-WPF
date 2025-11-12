using System;
using System.Collections.Generic;

namespace Group1.MusicApp.Models
{
    public class Track
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string ArtistName { get; set; }
        public string AlbumName { get; set; }
        public string AlbumImageUrl { get; set; }
        public int DurationMs { get; set; }
        public int Popularity { get; set; }
        public DateTime? ReleaseDate { get; set; }
        public List<string> Genres { get; set; } = new();
        public string PreviewUrl { get; set; }
        public bool IsExplicit { get; set; }

        // Audio Features
        public AudioFeatures AudioFeatures { get; set; }

        // Computed properties
        public string Duration => TimeSpan.FromMilliseconds(DurationMs).ToString(@"m\:ss");
        public string PopularityText => $"{Popularity}%";
        public string ReleaseDateText => ReleaseDate?.ToString("MMM dd, yyyy") ?? "Unknown";
        
        // Category for grouping
        public string Category { get; set; }
    }

    public class AudioFeatures
    {
        public float Energy { get; set; }          // 0.0 - 1.0
        public float Danceability { get; set; }    // 0.0 - 1.0
        public float Valence { get; set; }         // 0.0 - 1.0 (happiness)
        public float Acousticness { get; set; }    // 0.0 - 1.0
        public float Instrumentalness { get; set; } // 0.0 - 1.0
        public float Speechiness { get; set; }     // 0.0 - 1.0
        public float Tempo { get; set; }           // BPM
        public int Key { get; set; }               // Musical key
        public int Mode { get; set; }              // Major (1) or Minor (0)

        // Display percentages
        public int EnergyPercent => (int)(Energy * 100);
        public int DanceabilityPercent => (int)(Danceability * 100);
        public int ValencePercent => (int)(Valence * 100);
        public int AcousticnessPercent => (int)(Acousticness * 100);
    }
}

