using ScoobSoundPacker.Models;

namespace ScoobSoundPacker;

public sealed record CombineResult(
    IReadOnlyList<SoundPackEntry> Entries,
    IReadOnlyList<PackSourceReport> SourceReports,
    IReadOnlyList<string> GlobalWarnings);