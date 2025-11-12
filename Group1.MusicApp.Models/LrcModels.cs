using System;
using System.Collections.Generic;

namespace Group1.MusicApp.Models
{
    public class LrcLine
    {
        public TimeSpan Time { get; set; }
        public string Text { get; set; } = "";
    }

    public class SyncedLyricsResult
    {
        public bool HasSync => Lines.Count > 0;
        public List<LrcLine> Lines { get; set; } = new();
        public string Plain { get; set; } = "";
    }
}