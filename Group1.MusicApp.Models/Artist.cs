using System.Collections.Generic;

namespace Group1.MusicApp.Models
{
    public class Artist
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public List<string> Genres { get; set; } = new();
        public int Popularity { get; set; }
        public string ImageUrl { get; set; }
        public int Followers { get; set; }
    }
}

