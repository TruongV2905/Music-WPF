using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Group1.MusicApp.Models;

namespace Group1.MusicApp.Services
{
    /// <summary>
    /// Ưu tiên lấy synced lyrics từ LRCLIB; nếu không có thì fallback sang plain lyrics từ lyrics.ovh.
    /// Hỗ trợ [offset:xxx] và nhiều timestamp trên một dòng.
    /// </summary>
    public class LyricsService
    {
        private readonly HttpClient _http = new();

        public async Task<SyncedLyricsResult> GetSyncedAsync(string artist, string title)
        {
            var result = new SyncedLyricsResult();

            // 1) Thử LRCLIB (có syncedLyrics)
            try
            {
                var lrclibUrl =
                    $"https://lrclib.net/api/get?track_name={Uri.EscapeDataString(title)}&artist_name={Uri.EscapeDataString(artist)}";

                using var rsp = await _http.GetAsync(lrclibUrl);
                if (rsp.IsSuccessStatusCode)
                {
                    var json = await rsp.Content.ReadAsStringAsync();
                    using var doc = JsonDocument.Parse(json);
                    var root = doc.RootElement;

                    // plain
                    if (root.TryGetProperty("plainLyrics", out var plainNode) &&
                        plainNode.ValueKind == JsonValueKind.String)
                    {
                        result.Plain = plainNode.GetString() ?? "";
                    }

                    // synced (LRC)
                    if (root.TryGetProperty("syncedLyrics", out var syncNode) &&
                        syncNode.ValueKind == JsonValueKind.String)
                    {
                        var lrc = syncNode.GetString() ?? "";
                        var lines = ParseLrc(lrc);
                        if (lines.Count > 0)
                        {
                            result.Lines = lines;
                            return result; // có synced → trả luôn
                        }
                    }
                }
            }
            catch
            {
                // ignore
            }

            // 2) Fallback lyrics.ovh (plain only)
            try
            {
                var url = $"https://api.lyrics.ovh/v1/{Uri.EscapeDataString(artist)}/{Uri.EscapeDataString(title)}";
                using var rsp = await _http.GetAsync(url);
                if (rsp.IsSuccessStatusCode)
                {
                    var json = await rsp.Content.ReadAsStringAsync();
                    using var doc = JsonDocument.Parse(json);
                    result.Plain = doc.RootElement.GetProperty("lyrics").GetString() ?? "";
                }
            }
            catch
            {
                // ignore
            }

            return result;
        }

        // [offset:xxx] (ms)
        private static readonly Regex LrcOffsetRegex =
            new(@"^\s*\[offset\s*:\s*(-?\d+)\s*\]\s*$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        // Nhiều timestamp trên 1 dòng: [mm:ss.xx][mm:ss.xx] Lyric...
        private static readonly Regex TimestampRegex =
            new(@"\[(\d{1,2}):(\d{2})(?:\.(\d{1,3}))?\]", RegexOptions.Compiled);

        public static List<LrcLine> ParseLrc(string lrcText)
        {
            var list = new List<LrcLine>();
            if (string.IsNullOrWhiteSpace(lrcText)) return list;

            int globalOffsetMs = 0;
            var lines = lrcText.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var raw in lines)
            {
                var line = raw.Trim();
                if (line.Length == 0) continue;

                // [offset:xxx]
                var off = LrcOffsetRegex.Match(line);
                if (off.Success)
                {
                    if (int.TryParse(off.Groups[1].Value, out var o)) globalOffsetMs = o;
                    continue;
                }

                // tất cả timestamp trong dòng
                var matches = TimestampRegex.Matches(line);
                if (matches.Count == 0) continue;

                // text sau timestamp cuối
                int lastEnd = matches[matches.Count - 1].Index + matches[matches.Count - 1].Length;
                string text = line.Substring(lastEnd).Trim();
                if (string.IsNullOrEmpty(text)) text = "";

                foreach (Match m in matches)
                {
                    int mm = int.Parse(m.Groups[1].Value);
                    int ss = int.Parse(m.Groups[2].Value);
                    int ms = 0;
                    if (m.Groups[3].Success)
                    {
                        var frac = m.Groups[3].Value;
                        if (frac.Length == 1) ms = int.Parse(frac) * 100;
                        else if (frac.Length == 2) ms = int.Parse(frac) * 10;
                        else ms = int.Parse(frac[..Math.Min(3, frac.Length)]);
                    }

                    // áp dụng global offset (ms)
                    ms += globalOffsetMs;
                    var ts = new TimeSpan(0, 0, mm, ss, ms);
                    list.Add(new LrcLine { Time = ts, Text = text });
                }
            }
            list.Sort((a, b) => a.Time.CompareTo(b.Time));
            return list;
        }
    }
}
