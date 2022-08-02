// From: https://github.com/TheOsch/naudio-bpm

using Milki.Extensions.MixPlayer.NAudioExtensions.Wave;
using TimingAnalyz;

Console.WriteLine("NAudio BPM demo");
Console.WriteLine("Usage: NAudioBPM <Audio file path or URI> [<start(sec)> [<length(sec)>]]");
if (args.Length == 0)
{
    return;
}

string file = args[0];
int start = 0, length = -1;
if (args.Length > 1) int.TryParse(args[1], out start);
if (args.Length > 2) int.TryParse(args[2], out length);

SampleInfo sampleInfo;
using (var reader = new SmartWaveReader(file))
{
    sampleInfo = WaveStreamHelper.GetSamples(reader);
}

var groups = BpmDetectHelper.Analyze(sampleInfo, start, length);
if (groups.Length <= 0)
{
    Console.Error.WriteLine("ERROR: Cannot determine the BPM.");
    return;
}

Console.WriteLine($"Most probable BPM is {groups[0].Tempo} ({groups[0].Count} samples)");
if (groups.Length <= 1)
{
    return;
}

Console.WriteLine("Other options are:");
for (int i = 1; i < groups.Length; ++i)
{
    Console.WriteLine($"{groups[i].Tempo} BPM ({groups[i].Count} samples)");
}