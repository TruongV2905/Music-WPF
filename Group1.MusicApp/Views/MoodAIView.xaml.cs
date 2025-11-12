using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Group1.MusicApp.Services;
using Group1.MusicApp.Models;
namespace Group1.MusicApp.Views
{
    public partial class MoodAIView : UserControl
    {
        public MoodAIView()
        {
            InitializeComponent();
        }
        public PlaylistView? PlaylistViewRef { get; set; }
        // Event để thông báo khi muốn phát bài hát
        public event EventHandler<string>? TrackPlayRequested;

        private async void btnMoodAI_Click(object sender, RoutedEventArgs e)
        {
            string mood = txtMood.Text.Trim();
            if (string.IsNullOrEmpty(mood))
            {
                MessageBox.Show("Hãy nhập hoặc nói tâm trạng của bạn!");
                return;
            }

            btnMoodAI.IsEnabled = false;
            btnMoodAI.Content = "🤖 Đang phân tích ...";

            // Gọi Gemini
            var gemini = new GeminiMoodService("");


            // 1️⃣ Phân tích cảm xúc tổng quát
            string emotion = await gemini.AnalyzeMoodAsync(mood);

            // 3️⃣ Gọi Gemini để tạo lời động viên tự nhiên
            string aiMessage = await gemini.GenerateResponseAsync(
                $"Người dùng nói rằng họ cảm thấy {mood}. Hãy trả lời bằng giọng thân mật, động viên, không dài quá 1 câu, tiếng Việt."
            );
            // 4️⃣ Đọc lên bằng giọng nói
            var tts = new GoogleTTSService("google_tts_key.json");
            await tts.SpeakAsync($"{aiMessage}, đây là những bài hát sẽ phù hợp với tâm trạng của bạn");

            // 2️⃣ Gợi ý bài hát theo cảm xúc
            var songs = await SuggestSongsByMood(emotion);
            lstMoodResults.ItemsSource = songs;





            btnMoodAI.Content = "🎧 Gợi ý nhạc theo tâm trạng";
            btnMoodAI.IsEnabled = true;
        }

        public async Task GreetUserAsync()
        {
            try
            {
                var gemini = new GeminiMoodService("");
                var tts = new GoogleTTSService("google_tts_key.json");

                string prompt =
                    "Bạn là Mood AI. Hãy chào người dùng bằng một câu tiếng Việt thân mật, vui tươi, tự nhiên " +
                    "và hỏi xem hôm nay họ cảm thấy thế nào. Giới thiệu bạn là Mood AI nhé.";

                string greeting = await gemini.GenerateResponseAsync(prompt);
                await tts.SpeakAsync(greeting);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MoodAIView] Greeting failed: {ex.Message}");
            }
        }
        private async void btnVoiceMood_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                btnVoiceMood.IsEnabled = false;
                btnVoiceMood.Content = "🎙 Đang ghi âm...";

                string tempFile = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "voice_input.wav");

                // 🎧 Ghi âm 5 giây
                await AudioRecorder.RecordForSecondsAsync(tempFile, 5);

                btnVoiceMood.Content = "🧠 Đang nhận diện...";
                var stt = new GoogleSpeechToTextService("google_tts_key.json");
                string text = await stt.RecognizeOnceAsync(tempFile);

                txtMood.Text = text;

                btnVoiceMood.Content = "🎙 Nói tâm trạng của bạn";
                btnVoiceMood.IsEnabled = true;



            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi micro hoặc API: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                btnVoiceMood.Content = "🎙 Nói tâm trạng của bạn";
                btnVoiceMood.IsEnabled = true;
            }
        }

        private async Task<List<PlaylistItem>> SuggestSongsByMood(string mood)
        {
            var result = new List<PlaylistItem>();

            try
            {
                // ✅ Lấy playlist hiện tại
                var playlistItems = PlaylistViewRef?.GetPlaylistItems();
                if (playlistItems == null || playlistItems.Count == 0)
                {
                    MessageBox.Show("Playlist của bạn đang trống. Vui lòng thêm bài hát vào playlist trước.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                    return result;
                }

                // 🧠 Dùng Gemini để chọn nhạc phù hợp từ danh sách có sẵn
                var gemini = new GeminiMoodService("");

                // Tạo danh sách tiêu đề để gửi vào AI
                string allSongs = string.Join(", ", playlistItems.Select(p => $"{p.TrackName} - {p.ArtistName}"));

                string prompt =
    $"Danh sách bài hát của người dùng: {allSongs}. " +
    $"Tâm trạng hiện tại: '{mood}'. " +
    $"Hãy chọn nhạc thật CHÍNH XÁC theo từng mood sau, không được suy đoán cảm xúc sai lệch:" +

    // 1. Mapping cảm xúc
    $"\n1. Xác định mood của người dùng theo nhóm:" +
    $"\n   - HAPPY (vui, phấn khởi, hào hứng)." +
    $"\n   - SAD (buồn, tổn thương, thất tình, cô đơn)." +
    $"\n   - RELAX (chill, thư giãn, dễ chịu)." +
    $"\n   - ENERGY (nhiều năng lượng, hype, sôi động)." +
    $"\n   - ANGRY (stress, cáu, tức giận)." +
    $"\n   - LOVE (lãng mạn, yêu đương)." +

    // 2. RULE SIẾT CHẶT
    $"\n2. Quy tắc chọn bài (rất quan trọng):" +

    // --- Mood vui -> siết mạnh
    $"\n   • Nếu mood = HAPPY: TUYỆT ĐỐI không được chọn bài có bất kỳ từ khóa buồn sau:" +
    $"\n       buồn, sad, nước mắt, mưa, khóc, đau, đau khổ, tổn thương, chia tay, thất tình," +
    $"\n       cô đơn, tuyệt vọng, lạc nhau, tổn thương, tan vỡ, nhớ, thương em, em ơi, day dứt." +
    $"\n     Chỉ chọn bài mang vibe: vui, tươi, dance, upbeat, positive." +

    // --- Mood buồn -> bài buồn
    $"\n   • Nếu mood = SAD: ưu tiên ballad, tình cảm, lyrics buồn." +

    // --- Các mood còn lại
    $"\n   • RELAX: chill, nhẹ, lofi, acoustic." +
    $"\n   • ENERGY: EDM, dance, hip-hop, remix." +
    $"\n   • ANGRY: rock, rap mạnh." +
    $"\n   • LOVE: bài romantic, mellow, sweet." +

    // 3. Chỉ chọn khi chắc chắn
    $"\n3. Chỉ chọn bài nếu BẠN CHẮC CHẮN 100% bài đó hợp mood. Nếu không chắc → bỏ qua." +

    // 4. Nếu không có bài hợp
    $"\n4. Nếu KHÔNG có bài nào phù hợp → TRẢ VỀ đúng CHUỖI RỖNG (\"\")." +

    // 5. Trả về tối đa 10 bài
    $"\n5. Nếu có, trả về TỐI ĐA 10 bài." +

    // 6. Format chuẩn
    $"\n6. Định dạng trả về CHÍNH XÁC như sau (một dòng duy nhất):" +
    $"\n      Tên bài hát – Tên ca sĩ, Tên bài hát – Tên ca sĩ, ..." +

    // 7. Không giải thích
    $"\n7. Không giải thích gì thêm, không mô tả, không xuống dòng, không cảm thán. " +
    $"Chỉ output đúng danh sách theo format.";



                string response = await gemini.GenerateResponseAsync(prompt);

                // 🧩 Parse kết quả AI (tách theo dấu phẩy)
                foreach (var entry in response.Split(',', StringSplitOptions.RemoveEmptyEntries))
                {
                    var parts = entry.Split('–', StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length == 2)
                    {
                        string trackName = parts[0].Trim();
                        string artistName = parts[1].Trim();

                        // Tìm bài hát trong playlist khớp với kết quả AI
                        var matchingItem = playlistItems.FirstOrDefault(p =>
                            string.Equals(p.TrackName, trackName, StringComparison.OrdinalIgnoreCase) &&
                            string.Equals(p.ArtistName, artistName, StringComparison.OrdinalIgnoreCase));

                        if (matchingItem != null)
                            result.Add(matchingItem);
                    }
                }

                // Nếu AI không trả kết quả hợp lệ, fallback random
                if (result.Count == 0)
                {
                    var rnd = new Random();
                    result = playlistItems.OrderBy(x => rnd.Next())
                                          .Take(5)
                                          .ToList();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SuggestSongsByMood] Lỗi: {ex.Message}");
            }

            return result;
        }

        private void MoodPlay_Click(object sender, RoutedEventArgs e)
        {
            Button? button = sender as Button;
            if (button != null)
            {
                // Lấy PlaylistItem từ Tag
                PlaylistItem? item = button.Tag as PlaylistItem;
                if (item != null && !string.IsNullOrEmpty(item.TrackId))
                {
                    // Gửi event để phát bài hát
                    TrackPlayRequested?.Invoke(this, item.TrackId);
                }
            }
        }

        // 👇 Di chuyển hàm này VÀO bên trong class MoodAIView (trước dấu ngoặc đóng cuối cùng)
        private void lstMoodResults_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Không cần xử lý gì ở đây (chỉ để XAML không báo lỗi)
        }

      
    }

    public class MoodSong   
    {
        public string TrackName { get; set; }
        public string ArtistName { get; set; }
        public string AlbumImageUrl { get; set; }

        public MoodSong(string t, string a, string img)
        {
            TrackName = t;
            ArtistName = a;
            AlbumImageUrl = img;
        }
    }
}
