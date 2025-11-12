using Google.Cloud.Speech.V1;
using System;
using System.Threading.Tasks;

namespace Group1.MusicApp.Services
{
    public class GoogleSpeechToTextService
    {
        private readonly SpeechClient _client;

        public GoogleSpeechToTextService(string credentialsPath)
        {
            Environment.SetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS", credentialsPath);
            _client = SpeechClient.Create();
        }

        /// <summary>
        /// Nhận diện giọng nói từ file WAV (chuẩn 16-bit PCM)
        /// </summary>
        public async Task<string> RecognizeOnceAsync(string audioFilePath)
        {
            var response = await _client.RecognizeAsync(new RecognitionConfig
            {
                Encoding = RecognitionConfig.Types.AudioEncoding.Linear16,
                SampleRateHertz = 16000,
                LanguageCode = "vi-VN", // 🇻🇳 Tiếng Việt
                Model = "latest_long"
            },
            RecognitionAudio.FromFile(audioFilePath));

            if (response.Results.Count == 0)
                return "Không nghe rõ. Hãy thử lại.";

            // Ghép các câu nhận được
            string text = string.Join(" ", response.Results.SelectMany(r => r.Alternatives)
                                                          .Select(a => a.Transcript));
            return string.IsNullOrWhiteSpace(text) ? "Không nghe rõ." : text.Trim();
        }
    }
}
