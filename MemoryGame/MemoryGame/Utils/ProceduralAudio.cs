// Utils/ProceduralAudio.cs
using System;
using System.Threading.Tasks;
using Tizen.Multimedia;

namespace MemoryGame.Utils
{
    public static class ProceduralAudio
    {
        public static void PlayExplosion()
        {
            Task.Run(() =>
            {
                try
                {
                    int sampleRate = 44100;
                    double duration = 0.4; // 400ms explosion
                    int totalSamples = (int)(sampleRate * duration);
                    byte[] buffer = new byte[totalSamples * 2]; // 16-bit PCM
                    Random rand = new Random();

                    for (int i = 0; i < totalSamples; i++)
                    {
                        // Generate white noise (-1.0 to 1.0)
                        double noise = (rand.NextDouble() * 2.0) - 1.0;
                        
                        // Apply an exponential decay so it fades out smoothly
                        double envelope = Math.Exp(-7.0 * i / totalSamples);
                        
                        // Convert to 16-bit PCM integer
                        short sample = (short)(noise * envelope * short.MaxValue * 0.4); // 40% Volume
                        
                        buffer[i * 2] = (byte)(sample & 0xFF);
                        buffer[i * 2 + 1] = (byte)((sample >> 8) & 0xFF);
                    }

                    using (var playback = new AudioPlayback(sampleRate, AudioChannel.Mono, AudioSampleType.S16Le))
                    {
                        playback.Prepare();
                        
                        // Writing the buffer immediately plays it through the speakers!
                        playback.Write(buffer); 
                        
                        // Wait for the sound to finish physically playing before closing the stream
                        Task.Delay((int)(duration * 1000)).Wait();
                        playback.Unprepare();
                    }
                }
                catch { /* Failsafe if running on a simulator without audio hardware */ }
            });
        }

        public static void PlayLaser()
        {
            Task.Run(() =>
            {
                try
                {
                    int sampleRate = 44100;
                    double duration = 0.15; // 150ms fast laser
                    int totalSamples = (int)(sampleRate * duration);
                    byte[] buffer = new byte[totalSamples * 2];
                    
                    double frequency = 800.0; // Start at high pitch

                    for (int i = 0; i < totalSamples; i++)
                    {
                        frequency *= 0.998; // Rapidly drop the pitch to create the "Pew!"
                        
                        double t = (double)i / sampleRate;
                        
                        // Generate an 8-bit style Square Wave
                        double val = Math.Sin(2.0 * Math.PI * frequency * t);
                        double wave = val > 0 ? 1.0 : -1.0;
                        
                        // Fast linear decay to prevent clicking at the end
                        double envelope = 1.0 - ((double)i / totalSamples);
                        
                        short sample = (short)(wave * envelope * short.MaxValue * 0.15); // 15% Volume
                        
                        buffer[i * 2] = (byte)(sample & 0xFF);
                        buffer[i * 2 + 1] = (byte)((sample >> 8) & 0xFF);
                    }

                    using (var playback = new AudioPlayback(sampleRate, AudioChannel.Mono, AudioSampleType.S16Le))
                    {
                        playback.Prepare();
                        
                        // Feed the hardware stream directly
                        playback.Write(buffer); 
                        
                        Task.Delay((int)(duration * 1000)).Wait();
                        playback.Unprepare();
                    }
                }
                catch { }
            });
        }
    }
}