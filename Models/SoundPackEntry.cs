namespace ScoobSoundPacker.Models;

/// <summary>
/// One engine sound slot inside a combined FiveM audio resource.
/// </summary>
public sealed class SoundPackEntry : IEquatable<SoundPackEntry>
{
    public required string AudioHash { get; init; }
    /// <summary>
    /// True when AUDIO_SYNTHDATA / dat10 amp pack should be declared for this hash.
    /// </summary>
    public bool HasDat10 { get; init; }

    public bool Equals(SoundPackEntry? other) =>
        other is not null &&
        string.Equals(AudioHash, other.AudioHash, StringComparison.OrdinalIgnoreCase);

    public override bool Equals(object? obj) => obj is SoundPackEntry e && Equals(e);

    public override int GetHashCode() =>
        StringComparer.OrdinalIgnoreCase.GetHashCode(AudioHash);

    public static SoundPackEntry Merge(SoundPackEntry a, SoundPackEntry b) =>
        new()
        {
            AudioHash = a.AudioHash,
            HasDat10 = a.HasDat10 || b.HasDat10,
        };
}
