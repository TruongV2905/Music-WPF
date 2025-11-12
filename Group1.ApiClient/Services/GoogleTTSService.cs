using Google.Cloud.TextToSpeech.V1;
using NAudio.Wave;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Group1.MusicApp.Services
{
    public class GoogleTTSService : IDisposable
    {
        private readonly TextToSpeechClient _client;
        private readonly string _tempDir;

        public GoogleTTSService(string credentialsPath)
        {
            // Thiết lập key môi trường cho Google Cloud
            Environment.SetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS", credentialsPath);

            _client = TextToSpeechClient.Create();
            _tempDir = Path.Combine(Path.GetTempPath(), "MusicApp_TTS");
            Directory.CreateDirectory(_tempDir);
        }

        /// <summary>
        /// Đọc nội dung văn bản tiếng Việt bằng giọng tự nhiên
        /// </summary>
        public async Task SpeakAsync(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return;

            try
            {
                var input = new SynthesisInput { Text = text };

                var voice = new VoiceSelectionParams
                {
                    LanguageCode = "vi-VN",
                    Name = "vi-VN-Neural2-A" // Giọng nữ
                };

                var config = new AudioConfig
                {
                    AudioEncoding = AudioEncoding.Linear16 // WAV PCM
                };

                // 🧠 Gọi Google Cloud API
                var response = await _client.SynthesizeSpeechAsync(input, voice, config);

                // 📁 Lưu file WAV tạm thời
                string filePath = Path.Combine(_tempDir, $"tts_{Guid.NewGuid():N}.wav");
                await File.WriteAllBytesAsync(filePath, response.AudioContent.ToByteArray());

                // 🔊 Phát file
                using (var audioFile = new AudioFileReader(filePath))
                using (var outputDevice = new WaveOutEvent())
                {
                    outputDevice.Init(audioFile);
                    outputDevice.Play();

                    while (outputDevice.PlaybackState == PlaybackState.Playing)
                        await Task.Delay(100);
                }

                // 🧹 Xóa file sau khi phát
                File.Delete(filePath);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[TTS ERROR] {ex.Message}");
            }
        }

        public void Dispose()
        {
            try
            {
                TextToSpeechClient.ShutdownDefaultChannelsAsync().Wait(500);
            }
            catch { /* ignore */ }
        }
    }
}
