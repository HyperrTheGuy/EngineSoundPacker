using System.Text.RegularExpressions;
using ScoobSoundPacker.Models;

namespace ScoobSoundPacker;

/// <summary>
/// Extracts audio hashes and dat10 hints from <c>fxmanifest.lua</c> and legacy <c>__resource.lua</c>
/// (same <c>data_file</c> patterns).
/// </summary>
public static partial class FxManifestParser
{
    [GeneratedRegex(@"data_file\s+""AUDIO_SYNTHDATA""\s+""audioconfig/([^""]+)_amp\.dat""", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex SynthDataRegex();

    [GeneratedRegex(@"data_file\s+""AUDIO_WAVEPACK""\s+""sfx/dlc_([^""]+)""", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex WavepackRegex();

    [GeneratedRegex(@"data_file\s+""AUDIO_GAMEDATA""\s+""audioconfig/([^""]+)_game\.dat""", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex GameDataRegex();

    public static IReadOnlyList<SoundPackEntry> ParseFile(string path)
    {
        if (!File.Exists(path))
            return [];

        var text = File.ReadAllText(path);
        return ParseText(text);
    }

    public static IReadOnlyList<SoundPackEntry> ParseText(string text)
    {
        var byHash = new Dictionary<string, SoundPackEntry>(StringComparer.OrdinalIgnoreCase);

        foreach (Match m in SynthDataRegex().Matches(text))
        {
            var hash = NormalizeHash(m.Groups[1].Value);
            Upsert(byHash, hash, hasDat10: true);
        }

        foreach (Match m in WavepackRegex().Matches(text))
        {
            var hash = NormalizeHash(m.Groups[1].Value);
            Upsert(byHash, hash, hasDat10: false);
        }

        foreach (Match m in GameDataRegex().Matches(text))
        {
            var hash = NormalizeHash(m.Groups[1].Value);
            Upsert(byHash, hash, hasDat10: false);
        }

        return byHash.Values
            .OrderBy(v => v.AudioHash, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static void Upsert(Dictionary<string, SoundPackEntry> map, string hash, bool hasDat10)
    {
        if (string.IsNullOrWhiteSpace(hash))
            return;

        if (map.TryGetValue(hash, out var existing))
            map[hash] = SoundPackEntry.Merge(existing, new SoundPackEntry { AudioHash = hash, HasDat10 = hasDat10 });
        else
            map[hash] = new SoundPackEntry { AudioHash = hash, HasDat10 = hasDat10 };
    }

    private static string NormalizeHash(string raw) => raw.Trim();
}