using System;

namespace Group1.MusicApp.Models
{
    /// <summary>
    /// Represents a track saved in the user's playlist
    /// </summary>
    public class PlaylistItem
    {
        public int Id { get; set; }
        public string TrackId { get; set; }
        public string TrackName { get; set; }
        public string ArtistName { get; set; }
        public string AlbumName { get; set; }
        public string AlbumImageUrl { get; set; }
        public int DurationMs { get; set; }
        public string PreviewUrl { get; set; }
        public DateTime AddedDate { get; set; }

        // Computed properties
        public string Duration => TimeSpan.FromMilliseconds(DurationMs).ToString(@"m\:ss");
        public string AddedDateText => AddedDate.ToString("MMM dd, yyyy");
    }
}

