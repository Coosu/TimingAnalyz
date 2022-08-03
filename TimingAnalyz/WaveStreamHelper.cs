using NAudio.Wave;

namespace TimingAnalyz;

public static class WaveStreamHelper
{
    public static SampleInfo GetSamples(WaveStream waveStream)
    {
        if (waveStream.CanSeek)
        {
            waveStream.Seek(0, SeekOrigin.Begin);
        }

        var sampleRate = waveStream.WaveFormat.SampleRate;
        var channels = waveStream.WaveFormat.Channels;
        // Originally the sample rate was constant (44100), and the number of channels was 2. 
        // Let's just in case take them from file's properties

        int bytesPerSample = waveStream.WaveFormat.BitsPerSample / 8;
        if (bytesPerSample == 0)
        {
            bytesPerSample = 2; // assume 16 bit
        }

        int sampleCount = (int)waveStream.Length / bytesPerSample;

        // Read the wave data
        if (sampleCount <= 0)
        {
            return new SampleInfo(Array.Empty<float>(), sampleRate, channels);
        }

        var length = sampleCount / channels * channels;

        var sampleReader = SampleProviderConverters.ConvertWaveProviderIntoSampleProvider(waveStream);
        var samples = new float[length];
        sampleReader.Read(samples, 0, length);
        return new SampleInfo(samples, sampleRate, channels);
    }
}