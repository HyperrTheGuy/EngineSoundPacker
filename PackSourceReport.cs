namespace ScoobSoundPacker;

/// <summary>Outcome of packing one source resource folder into the combined output.</summary>
public sealed record PackSourceReport(
    string SourcePath,
    string DisplayName,
    int Index,
    int Total,
    IReadOnlyList<string> CopiedRelativePaths,
    IReadOnlyList<string> SkippedRelativePaths,
    IReadOnlyList<string> Problems);