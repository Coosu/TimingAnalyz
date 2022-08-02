namespace TimingAnalyz;

public readonly struct BpmGroup
{
    public readonly int Count;
    public readonly short Tempo;

    public BpmGroup(int count, short tempo)
    {
        Count = count;
        Tempo = tempo;
    }
}