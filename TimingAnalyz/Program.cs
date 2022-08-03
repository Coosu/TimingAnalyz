// From: https://github.com/TheOsch/naudio-bpm

using Milki.Extensions.MixPlayer;
using Milki.Extensions.MixPlayer.Utilities;
using NAudio.Vorbis;
using NAudio.Wave;
using TimingAnalyz;

if (args.Length == 0)
{
    Console.WriteLine("TimingAnalyz");
    Console.WriteLine("Usage: TimingAnalyz <Audio file path or URI> [<start(sec)> [<length(sec)>]]");
    return;
}

string file = args[0];
int start = 0, length = -1;
if (args.Length > 1) int.TryParse(args[1], out start);
if (args.Length > 2) int.TryParse(args[2], out length);

SampleInfo sampleInfo;

using (var sourceStream = File.OpenRead(file))
{
    var type = FileFormatHelper.GetFileFormatFromStream(sourceStream);
    WaveStream reader = type switch
    {
        FileFormat.Wav => new WaveFileReader(sourceStream),
        FileFormat.Mp3 or FileFormat.Mp3Id3 => new StreamMediaFoundationReader(sourceStream),
        FileFormat.Ogg => new VorbisWaveReader(sourceStream),
        FileFormat.Aiff => new AiffFileReader(sourceStream),
        _ => new StreamMediaFoundationReader(sourceStream)
    };
    if (type == FileFormat.Wav)
    {
        if (reader.WaveFormat.Encoding is not (WaveFormatEncoding.Pcm or WaveFormatEncoding.IeeeFloat))
        {
            throw new FormatException($"Encoding {reader.WaveFormat.Encoding} not supported.");
        }
    }
    //WaveStream reader = (file.EndsWith(".mp3", StringComparison.OrdinalIgnoreCase))
    //    ? new Mp3FileReaderBase(file, k => new AcmMp3FrameDecompressor(k))
    //    : new SmartWaveReader(file);
    using (reader)
    {
        Console.WriteLine("Reading audio file...");
        sampleInfo = WaveStreamHelper.GetSamples(reader);
    }
}

Console.WriteLine("Detecting timing...");
var groups = BpmDetectHelper.Analyze(sampleInfo, start, length);
if (groups.Length <= 0)
{
    Console.Error.WriteLine("\r\nERROR: Cannot determine the BPM.");
    return;
}

var addition = groups[0].Tempo < 110 ? $", or double as {groups[0].Tempo * 2}" : "";
Console.WriteLine($"\r\nMost probable BPM is {groups[0].Tempo}{addition} ({groups[0].Count} samples)");
if (groups.Length <= 1)
{
    return;
}

Console.WriteLine("Other options are:");
for (int i = 1; i < groups.Length; ++i)
{
    Console.WriteLine($"{groups[i].Tempo} BPM ({groups[i].Count} samples)");
}

Console.WriteLine("\r\nPress any key to continue...");
Console.ReadKey(true);