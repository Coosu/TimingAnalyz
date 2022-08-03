using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace TimingAnalyz;

internal static class SampleProviderConverters
{
    public static ISampleProvider ConvertWaveProviderIntoSampleProvider(IWaveProvider waveProvider)
    {
        if (waveProvider.WaveFormat.Encoding == WaveFormatEncoding.Pcm)
        {
            if (waveProvider.WaveFormat.BitsPerSample == 8)
                return new Pcm8BitToSampleProvider(waveProvider);
            if (waveProvider.WaveFormat.BitsPerSample == 16)
                return new Pcm16BitToSampleProvider(waveProvider);
            if (waveProvider.WaveFormat.BitsPerSample == 24)
                return new Pcm24BitToSampleProvider(waveProvider);
            if (waveProvider.WaveFormat.BitsPerSample == 32)
                return new Pcm32BitToSampleProvider(waveProvider);
            throw new InvalidOperationException("Unsupported bit depth");
        }

        if (waveProvider.WaveFormat.Encoding != WaveFormatEncoding.IeeeFloat)
            throw new ArgumentException("Unsupported source encoding");
        if (waveProvider.WaveFormat.BitsPerSample == 64)
            return new WaveToSampleProvider64(waveProvider);
        return new WaveToSampleProvider2(waveProvider);
    }
}

/// <summary>
/// Helper class turning an already 32 bit floating point IWaveProvider
/// into an ISampleProvider - hopefully not needed for most applications
/// </summary>
public class WaveToSampleProvider2 : SampleProviderConverterBase
{
    /// <summary>
    /// Initializes a new instance of the WaveToSampleProvider class
    /// </summary>
    /// <param name="source">Source wave provider, must be IEEE float</param>
    public WaveToSampleProvider2(IWaveProvider source)
        : base(source)
    {
        if (source.WaveFormat.Encoding != WaveFormatEncoding.IeeeFloat)
        {
            throw new ArgumentException("Must be already floating point");
        }
    }

    /// <summary>
    /// Reads from this provider
    /// </summary>
    public override int Read(float[] buffer, int offset, int count)
    {
        int bytesNeeded = count << 2;
        EnsureSourceBuffer(bytesNeeded);
        int bytesRead = source.Read(sourceBuffer, 0, bytesNeeded);
        int samplesRead = bytesRead >> 2;
        Buffer.BlockCopy(sourceBuffer, 0, buffer, offset, bytesRead);
        return samplesRead;
    }
}