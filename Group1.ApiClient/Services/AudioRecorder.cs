using NAudio.Wave;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Group1.MusicApp.Services
{
    public static class AudioRecorder
    {
        public static async Task<string> RecordForSecondsAsync(string filePath, int seconds = 5)
        {
            if (File.Exists(filePath))
                File.Delete(filePath);

            using var waveIn = new WaveInEvent
            {
                WaveFormat = new WaveFormat(16000, 1) // 16kHz, mono – chuẩn Google STT
            };

            using var writer = new WaveFileWriter(filePath, waveIn.WaveFormat);

            waveIn.DataAvailable += (s, e) =>
            {
                writer.Write(e.Buffer, 0, e.BytesRecorded);
            };

            waveIn.StartRecording();
            await Task.Delay(TimeSpan.FromSeconds(seconds));
            waveIn.StopRecording();

            return filePath;
        }
    }
}
