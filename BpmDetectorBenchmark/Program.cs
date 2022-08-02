using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Order;
using BenchmarkDotNet.Running;
using Milki.Extensions.MixPlayer.NAudioExtensions.Wave;
using NAudio.Wave;
using TimingAnalyz;
using TimingAnalyz.Old;

BenchmarkRunner.Run<BpmDetectorTask>();

[SimpleJob(RuntimeMoniker.Net50)]
[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
public class BpmDetectorTask
{
    private SmartWaveReader _reader;
    private MediaFoundationReader _reader2;

    [GlobalSetup]
    public void Setup()
    {
        var file = @"C:\Users\milkitic\AppData\Local\osu!\Songs\cYsmix_triangles\audio.mp3";
        _reader = new SmartWaveReader(file);
        _reader2 = new MediaFoundationReader(file);
    }

    [Benchmark(Baseline = true)]
    public object? NewSmart()
    {
        var sampleInfo = WaveStreamHelper.GetSamples(_reader);
        return BpmDetectHelper.Analyze(sampleInfo);
    }

    //[Benchmark]
    //public object? NewMediaFoundation()
    //{
    //    var bpm = new BPMDetector(_reader2);
    //    return bpm.Groups;
    //}

    [Benchmark]
    public object? OldSmart()
    {
        var bpm = new BPMDetector(_reader);
        return bpm.Groups;
    }
}