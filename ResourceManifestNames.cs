namespace ScoobSoundPacker;

/// <summary>FiveM resource descriptor files (new and legacy).</summary>
public static class ResourceManifestNames
{
    public const string FxManifest = "fxmanifest.lua";
    public const string LegacyResource = "__resource.lua";

    /// <summary>Returns full paths for manifest files that exist under <paramref name="resourceRoot"/>.</summary>
    public static IEnumerable<string> EnumerateExisting(string resourceRoot)
    {
        foreach (var name in new[] { FxManifest, LegacyResource })
        {
            var p = Path.Combine(resourceRoot, name);
            if (File.Exists(p))
                yield return p;
        }
    }
}