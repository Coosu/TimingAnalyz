using Milki.Extensions.MixPlayer.NAudioExtensions.Wave;

// ReSharper disable once CheckNamespace
namespace Milki.Extensions.MixPlayer.Utilities;

public static class FileFormatHelper
{
    private class ByteMatchRule
    {
        public ByteMatchRule(string name, FileFormat fileFormat, params MatchRule[] matchRules)
        {
            if (matchRules.Length < 1)
                throw new ArgumentOutOfRangeException(nameof(matchRules), matchRules.Length, null);
            Name = name;
            FileFormat = fileFormat;
            MatchRules = matchRules;
        }

        public string Name { get; }
        public FileFormat FileFormat { get; }
        public IReadOnlyList<MatchRule> MatchRules { get; }

        public bool PeekFromBegin(byte b)
        {
            var firstRule = MatchRules[0];
            if (firstRule.Skip > 0) return false;
            return firstRule.Signatures[0] == b;
        }

        public bool PeekFromBegin(ReadOnlySpan<byte> b)
        {
            var firstRule = MatchRules[0];
            if (firstRule.Skip > 0)
            {
                return b.Length >= firstRule.Skip;
            }

            if (b.Length > firstRule.Signatures.Length) return false;
            return b.SequenceEqual(firstRule.Signatures.AsSpan(0, b.Length));
        }

        public bool MatchOthers(int skip, Stream stream)
        {
            int arrayIndex;
            int arrayStartIndex;
            if (MatchRules[0].Skip > 0)
            {
                if (skip > MatchRules[0].Skip) return false;
                if (skip == MatchRules[0].Skip)
                {
                    arrayIndex = 1;
                    arrayStartIndex = 0;
                }
                else
                {
                    arrayIndex = 0;
                    arrayStartIndex = skip;
                }
            }
            else
            {
                if (skip > MatchRules[0].Signatures.Length) return false;
                if (skip == MatchRules[0].Signatures.Length)
                {
                    arrayIndex = 1;
                    arrayStartIndex = 0;
                }
                else
                {
                    arrayIndex = 0;
                    arrayStartIndex = skip;
                }
            }

            bool success = false;
            // ReSharper disable once AssignmentInConditionalExpression
            while (arrayIndex < MatchRules.Count &&
                   (success = GetNext(stream, arrayIndex, arrayStartIndex)))
            {
                arrayIndex++;
                arrayStartIndex = 0;
            }

            return success;
        }

        private bool GetNext(Stream stream, int arrayIndex, int arrayStartIndex)
        {
            var array = MatchRules[arrayIndex];
            if (array.Skip > 0)
            {
                for (int i = 0; i < array.Skip - arrayStartIndex; i++)
                {
                    var b = stream.ReadByte();
                    if (b == -1) return false;
                }
            }
            else
            {
                Span<byte> span = stackalloc byte[array.Signatures.Length - arrayStartIndex];
                var count = stream.Read(span);
                if (count < span.Length) return false;
                if (!span.SequenceEqual(array.Signatures.AsSpan(arrayStartIndex))) return false;
            }

            return true;
        }
    }

    private class MatchRule
    {
        public MatchRule(int skip)
        {
            Skip = skip;
        }

        public MatchRule(params byte[] signatures)
        {
            if (signatures.Length < 1)
                throw new ArgumentOutOfRangeException(nameof(signatures), signatures.Length, null);
            Signatures = signatures;
        }

        public byte[] Signatures { get; } = Array.Empty<byte>();
        public int Skip { get; } = 0;
    }

    public static FileFormat GetFileFormatFromStream(Stream sourceStream)
    {
        if (!sourceStream.CanSeek)
        {
            sourceStream = new ReadSeekableStream(sourceStream, 32);
        }

        var firstByte = sourceStream.ReadByte();
        var matches = KnownMatchRules.Where(k => k.PeekFromBegin((byte)firstByte));
        foreach (var match in matches)
        {
            if (match.MatchOthers(1, sourceStream))
            {
                return match.FileFormat;
            }

            sourceStream.Seek(1, SeekOrigin.Begin);
        }

        return FileFormat.Others;
    }

    // https://en.wikipedia.org/wiki/List_of_file_signatures
    private static readonly ByteMatchRule[] KnownMatchRules =
    {
        new("WAV", FileFormat.Wav,
            new MatchRule(0x52, 0x49, 0x46, 0x46),
            new MatchRule(4),
            new MatchRule(0x57, 0x41, 0x56, 0x45)
        ),
        new("WMA", FileFormat.Wma,
            new MatchRule(0x30, 0x26, 0xB2, 0x75, 0x8E, 0x66, 0xCF, 0x11, 0xA6, 0xD9, 0x00, 0xAA, 0x00, 0x62, 0xCE,
                0x6C)
        ),
        new("OGG", FileFormat.Ogg,
            new MatchRule(0x4F, 0x67, 0x67, 0x53)
        ),
        new("MP3_FA", FileFormat.Mp3,
            new MatchRule(0xFF, 0xFA)
        ),
        new("MP3_FB", FileFormat.Mp3,
            new MatchRule(0xFF, 0xFB)
        ),
        new("MP3_F3", FileFormat.Mp3,
            new MatchRule(0xFF, 0xF3)
        ),
        new("MP3_F2", FileFormat.Mp3,
            new MatchRule(0xFF, 0xF2)
        ),
        new("MP3_E3", FileFormat.Mp3,
            new MatchRule(0xFF, 0xE3)
        ),
        new("MP3_ID3", FileFormat.Mp3Id3,
            new MatchRule(0x49, 0x44, 0x33)
        ),
        new("FLAC", FileFormat.Flac,
            new MatchRule(0x66, 0x4C, 0x61, 0x43)
        ),
        new("AIFF", FileFormat.Aiff,
            new MatchRule(0x46, 0x4F, 0x52, 0x4D),
            new MatchRule(4),
            new MatchRule(0x41, 0x49, 0x46, 0x46)
        ),
    };
}