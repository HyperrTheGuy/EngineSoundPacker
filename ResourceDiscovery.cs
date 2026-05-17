namespace ScoobSoundPacker;

/// <summary>
/// Resolves which folders to merge from a user-selected parent directory.
/// </summary>
public static class ResourceDiscovery
{
    /// <summary>
    /// If the parent contains subfolders that look like audio resources, returns those paths.
    /// Otherwise, if the parent itself looks like one resource, returns a single-element list.
    /// </summary>
    public static IReadOnlyList<string> ResolveSourceFolders(string inputRoot)
    {
        var full = Path.GetFullPath(inputRoot.Trim());
        if (!Directory.Exists(full))
            throw new DirectoryNotFoundException($"Input folder was not found: {full}");

        var children = Directory
            .EnumerateDirectories(full)
            .Where(IsLikelyEngineSoundResource)
            .OrderBy(p => p, StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (children.Count > 0)
            return children;

        if (IsLikelyEngineSoundResource(full))
            return [full];

        throw new InvalidOperationException(
            "No engine sound resources were found. Put each resource in its own subfolder under the input path, " +
            "or point the input path at a single resource folder that contains audioconfig, sfx, or fxmanifest.lua / __resource.lua.");
    }

    private static bool IsLikelyEngineSoundResource(string path) =>
        Directory.Exists(Path.Combine(path, "audioconfig"))
        || Directory.Exists(Path.Combine(path, "sfx"))
        || File.Exists(Path.Combine(path, ResourceManifestNames.FxManifest))
        || File.Exists(Path.Combine(path, ResourceManifestNames.LegacyResource));
}