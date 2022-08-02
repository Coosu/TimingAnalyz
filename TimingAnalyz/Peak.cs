using System.Diagnostics;

namespace TimingAnalyz;

[DebuggerDisplay("{DebuggerDisplay}")]
internal readonly struct Peak
{
    public readonly int Position;
    public readonly float Volume;

    public Peak(int position, float volume)
    {
        Position = position;
        Volume = volume;
    }

    private string DebuggerDisplay => $"{Position}:{Volume:P2}";
}