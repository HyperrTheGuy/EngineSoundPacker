using ScoobSoundPacker.Models;

namespace ScoobSoundPacker;

public sealed class ResourceCombineService
{
    /// <summary>Ensures standard audioconfig + sfx/dlc_* folders exist for every entry.</summary>
    public static void EnsureFolderLayout(string outputResourceRoot, IReadOnlyList<SoundPackEntry> entries)
    {
        var root = Path.GetFullPath(outputResourceRoot);
        Directory.CreateDirectory(Path.Combine(root, "audioconfig"));
        Directory.CreateDirectory(Path.Combine(root, "sfx"));
        foreach (var e in entries)
            Directory.CreateDirectory(Path.Combine(root, "sfx", "dlc_" + e.AudioHash));
    }

    /// <summary>
    /// Merges multiple FiveM resource folders into one output folder (audioconfig + sfx),
    /// one source at a time, collecting per-source file lists.
    /// </summary>
    public CombineResult MergeFolders(
        IReadOnlyList<string> sourceResourceRoots,
        string outputResourceRoot,
        bool overwriteConflicts)
    {
        if (sourceResourceRoots.Count == 0)
            throw new ArgumentException("At least one source resource folder is required.", nameof(sourceResourceRoots));

        var fullOutput = Path.GetFullPath(outputResourceRoot);
        Directory.CreateDirectory(fullOutput);

        var globalWarnings = new List<string>();
        var reports = new List<PackSourceReport>();
        var entryMap = new Dictionary<string, SoundPackEntry>(StringComparer.OrdinalIgnoreCase);

        var total = sourceResourceRoots.Count;
        for (var i = 0; i < sourceResourceRoots.Count; i++)
        {
            var raw = sourceResourceRoots[i];
            var idx = i + 1;
            var src = Path.GetFullPath(raw.Trim());
            var displayName = Path.GetFileName(src.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));

            var copied = new List<string>();
            var skipped = new List<string>();
            var problems = new List<string>();

            if (!Directory.Exists(src))
            {
                problems.Add($"Folder does not exist: {src}");
                reports.Add(new PackSourceReport(src, displayName, idx, total, copied, skipped, problems));
                continue;
            }

            if (IsSubPath(fullOutput, src) || IsSubPath(src, fullOutput))
                globalWarnings.Add($"Source path overlaps output — verify files are correct: {src}");

            try
            {
                MergeScanEntries(src, entryMap);
            }
            catch (Exception ex)
            {
                problems.Add($"Could not scan resource metadata: {ex.Message}");
            }

            TryCopyTree(Path.Combine(src, "audioconfig"), Path.Combine(fullOutput, "audioconfig"), fullOutput, overwriteConflicts, copied, skipped, problems);
            TryCopyTree(Path.Combine(src, "sfx"), Path.Combine(fullOutput, "sfx"), fullOutput, overwriteConflicts, copied, skipped, problems);

            if (copied.Count == 0 && skipped.Count == 0 && problems.Count == 0)
            {
                var hasAudio = Directory.Exists(Path.Combine(src, "audioconfig"));
                var hasSfx = Directory.Exists(Path.Combine(src, "sfx"));
                if (!hasAudio && !hasSfx)
                    problems.Add("No audioconfig or sfx folder found — nothing was copied from this resource.");
            }

            copied.Sort(StringComparer.OrdinalIgnoreCase);
            skipped.Sort(StringComparer.OrdinalIgnoreCase);

            reports.Add(new PackSourceReport(src, displayName, idx, total, copied, skipped, problems));
        }

        ReconcileEntriesFromOutput(fullOutput, entryMap);

        var entries = entryMap.Values.OrderBy(e => e.AudioHash, StringComparer.OrdinalIgnoreCase).ToList();
        if (entries.Count == 0)
            throw new InvalidOperationException(
                "No audio hashes were discovered. Ensure each source contains sfx/dlc_<hash> folders or a parseable fxmanifest.lua / __resource.lua.");

        return new CombineResult(entries, reports, globalWarnings);
    }

    private static void MergeScanEntries(string srcRoot, Dictionary<string, SoundPackEntry> entryMap)
    {
        foreach (var e in ResourceLayoutScanner.ScanResourceFolder(srcRoot))
        {
            if (entryMap.TryGetValue(e.AudioHash, out var existing))
                entryMap[e.AudioHash] = SoundPackEntry.Merge(existing, e);
            else
                entryMap[e.AudioHash] = e;
        }
    }

    private static void ReconcileEntriesFromOutput(string outputRoot, Dictionary<string, SoundPackEntry> entryMap)
    {
        foreach (var e in ResourceLayoutScanner.ScanResourceFolder(outputRoot))
        {
            if (entryMap.TryGetValue(e.AudioHash, out var existing))
                entryMap[e.AudioHash] = SoundPackEntry.Merge(existing, e);
            else
                entryMap[e.AudioHash] = e;
        }
    }

    private static void TryCopyTree(
        string sourceDir,
        string destDir,
        string outputRoot,
        bool overwrite,
        List<string> copiedRelative,
        List<string> skippedRelative,
        List<string> problems)
    {
        if (string.IsNullOrEmpty(sourceDir) || !Directory.Exists(sourceDir))
            return;

        Directory.CreateDirectory(destDir);

        foreach (var file in Directory.EnumerateFiles(sourceDir, "*", SearchOption.AllDirectories))
        {
            var rel = Path.GetRelativePath(sourceDir, file);
            var destFile = Path.Combine(destDir, rel);
            var destParent = Path.GetDirectoryName(destFile);
            if (!string.IsNullOrEmpty(destParent))
                Directory.CreateDirectory(destParent);

            try
            {
                if (File.Exists(destFile) && !overwrite)
                {
                    skippedRelative.Add(Path.GetRelativePath(outputRoot, Path.GetFullPath(destFile)));
                    continue;
                }

                File.Copy(file, destFile, overwrite: true);
                copiedRelative.Add(Path.GetRelativePath(outputRoot, Path.GetFullPath(destFile)));
            }
            catch (Exception ex)
            {
                problems.Add($"{Path.GetRelativePath(outputRoot, Path.GetFullPath(destFile))}: {ex.Message}");
            }
        }
    }

    private static bool IsSubPath(string parent, string child)
    {
        var p = parent.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
        var c = Path.GetFullPath(child).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        return c.StartsWith(p, StringComparison.OrdinalIgnoreCase);
    }
}