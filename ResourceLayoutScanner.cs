using ScoobSoundPacker.Models;

namespace ScoobSoundPacker;

/// <summary>
/// Discovers hashes from sfx/dlc_* folders and optional *_amp.dat* files under audioconfig.
/// </summary>
public static class ResourceLayoutScanner
{
    public static IReadOnlyList<SoundPackEntry> ScanResourceFolder(string resourceRoot)
    {
        var map = new Dictionary<string, SoundPackEntry>(StringComparer.OrdinalIgnoreCase);
        var sfxRoot = Path.Combine(resourceRoot, "sfx");
        if (Directory.Exists(sfxRoot))
        {
            foreach (var dir in Directory.EnumerateDirectories(sfxRoot))
            {
                var name = Path.GetFileName(dir);
                if (!name.StartsWith("dlc_", StringComparison.OrdinalIgnoreCase))
                    continue;
                var hash = name["dlc_".Length..];
                if (string.IsNullOrWhiteSpace(hash))
                    continue;
                Upsert(map, hash.Trim(), hasDat10: false);
            }
        }

        var audioRoot = Path.Combine(resourceRoot, "audioconfig");
        if (Directory.Exists(audioRoot))
        {
            foreach (var file in Directory.EnumerateFiles(audioRoot))
            {
                var fn = Path.GetFileName(file);
                var ampIdx = fn.IndexOf("_amp.", StringComparison.OrdinalIgnoreCase);
                if (ampIdx <= 0)
                    continue;
                var hash = fn[..ampIdx];
                Upsert(map, hash, hasDat10: true);
            }
        }

        foreach (var manifestPath in ResourceManifestNames.EnumerateExisting(resourceRoot))
        {
            foreach (var parsed in FxManifestParser.ParseFile(manifestPath))
                Upsert(map, parsed.AudioHash, parsed.HasDat10);
        }

        return map.Values.OrderBy(v => v.AudioHash, StringComparer.OrdinalIgnoreCase).ToList();
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
}