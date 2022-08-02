namespace TimingAnalyz;

public sealed class SampleInfo
{
    public SampleInfo(float[] samples, int sampleRate, int channels)
    {
        Samples = samples;
        SampleRate = sampleRate;
        Channels = channels;
    }

    public float[] Samples { get; }
    public int SampleRate { get; }
    public int Channels { get; }
}