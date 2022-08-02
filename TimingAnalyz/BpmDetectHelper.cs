using System.Buffers;
using NAudio.Dsp;

namespace TimingAnalyz;

public static class BpmDetectHelper
{
    public static BpmGroup[] Analyze(SampleInfo sampleInfo, int start = 0, int length = -1)
    {
        var samplesOriginal = sampleInfo.Samples;
        var sampleRate = sampleInfo.SampleRate;
        var channels = sampleInfo.Channels;

        if (length < 0)
        {
            length = samplesOriginal.Length - start;
            if (length < 0)
            {
                throw new Exception("Length is too long for sample.");
            }
        }

        var samples = samplesOriginal.AsSpan(start, length);

        // Beats, or kicks, generally occur around the 100 to 150 hz range.
        // First a lowPass to remove most of the song.
        var lowPass = BiQuadFilter.LowPassFilter(sampleRate, 150.0F, 1.0F);
        // Now a highPass to remove the bassline.
        var highPass = BiQuadFilter.HighPassFilter(sampleRate, 100.0F, 1.0F);

        // Below this is often the bassline.  So let's focus just on that.
        for (var ch = 0; ch < channels; ++ch)
        {
            for (var i = ch; i < length; i += channels)
            {
                samples[i] = highPass.Transform(lowPass.Transform(samples[i]));
            }
        }

        int partSize = sampleRate / 2;
        var parts = length / channels / partSize;
        Peak[]? array = null;
        Span<Peak> peaks = parts > 1024
            ? array = ArrayPool<Peak>.Shared.Rent(parts)
            : stackalloc Peak[parts];

        Span<BpmGroup> initial = stackalloc BpmGroup[256];
        var valueListBuilder = new ValueListBuilder<BpmGroup>(initial);
        try
        {
            if (array != null) peaks = peaks.Slice(0, parts);
            FillPeaks(samples, channels, ref peaks, partSize, parts);
            GetIntervals(sampleRate, ref valueListBuilder, peaks);

            var allGroups = valueListBuilder.AsUnsafeSpan();
            allGroups.Sort((x, y) => y.Count.CompareTo(x.Count));

            if (allGroups.Length > 5)
            {
                allGroups = allGroups.Slice(0, 5);
            }

            return allGroups.ToArray();
        }
        finally
        {
            valueListBuilder.Dispose();
            if (array != null)
            {
                ArrayPool<Peak>.Shared.Return(array);
            }
        }
    }

    private static void FillPeaks(ReadOnlySpan<float> samples, int channels,
        ref Span<Peak> peaks, int partSize, int parts)
    {
        // What we're going to do here, is to divide up our audio into parts.

        // We will then identify, for each part, what the loudest sample is in that
        // part.

        // It's implied that that sample would represent the most likely 'beat'
        // within that part.

        // Each part is 0.5 seconds long

        // This will give us 60 'beats' - we will only take the loudest half of
        // those.

        // This will allow us to ignore breaks, and allow us to address tracks with
        // a BPM below 120.

        for (int i = 0; i < parts; ++i)
        {
            var position = -1;
            var volume = 0f;
            for (int j = 0; j < partSize; ++j)
            {
                float vol = 0.0F;
                for (int k = 0; k < channels; ++k)
                {
                    float v = samples[i * channels * partSize + j * channels + k];
                    if (vol < v)
                    {
                        vol = v;
                    }
                }

                if (position == -1 || volume < vol)
                {
                    position = i * partSize + j;
                    volume = vol;
                }
            }

            peaks[i] = new Peak(position, volume);
        }

        // We then sort the peaks according to volume...
        peaks.Sort((x, y) => y.Volume.CompareTo(x.Volume));
        // ...take the loundest half of those...
        peaks = peaks[..(peaks.Length / 2)];
        // ...and re-sort it back based on position.
        peaks.Sort((x, y) => x.Position.CompareTo(y.Position));
    }

    private static void GetIntervals(int sampleRate, ref ValueListBuilder<BpmGroup> groups, Span<Peak> peaks)
    {
        // What we now do is get all of our peaks, and then measure the distance to
        // other peaks, to create intervals.  Then based on the distance between
        // those peaks (the distance of the intervals) we can calculate the BPM of
        // that particular interval.

        // The interval that is seen the most should have the BPM that corresponds
        // to the track itself.
        for (int index = 0; index < peaks.Length; ++index)
        {
            ref Peak peak = ref peaks[index];
            for (int i = 1; index + i < peaks.Length && i < 10; ++i)
            {
                float tempo = 60.0F * sampleRate / (peaks[index + i].Position - peak.Position);
                while (tempo < 90.0F)
                {
                    tempo *= 2.0F;
                }

                while (tempo > 180.0F)
                {
                    tempo /= 2.0F;
                }

                var tempo2 = (short)Math.Round(tempo);
                var count = 1;
                int j;
                for (j = 0; j < groups.Length && groups[j].Tempo != tempo2; ++j)
                {
                }

                if (j < groups.Length)
                {
                    count = groups[j].Count + 1;
                    groups[j] = new BpmGroup(count, tempo2);
                }
                else
                {
                    groups.Append(new BpmGroup(count, tempo2));
                }
            }
        }
    }
}