using System.Text;
using ScoobSoundPacker.Models;

namespace ScoobSoundPacker;

public static class FxManifestWriter
{
    /// <summary>
    /// Writes a FiveM fxmanifest.lua for combined audio resources (UTF-8 no BOM).
    /// Uses <see cref="FiveMResourceDefaults.FxVersion"/>.
    /// </summary>
    public static void Write(string outputResourceRoot, IReadOnlyList<SoundPackEntry> entries)
    {
        if (entries.Count == 0)
            throw new ArgumentException("At least one sound entry is required.", nameof(entries));

        Directory.CreateDirectory(outputResourceRoot);
        var path = Path.Combine(outputResourceRoot, "fxmanifest.lua");

        var sb = new StringBuilder(512);
        sb.AppendLine($"fx_version \"{EscapeLuaString(FiveMResourceDefaults.FxVersion)}\"");
        sb.AppendLine("game \"gta5\"");
        sb.AppendLine();
        sb.AppendLine("files {");
        sb.AppendLine("\t\"audioconfig/*.dat151.rel\",");
        sb.AppendLine("\t\"audioconfig/*.dat54.rel\",");
        sb.AppendLine("\t\"audioconfig/*.dat10.rel\",");
        sb.AppendLine("\t\"sfx/**/*.awc\"");
        sb.AppendLine("}");
        sb.AppendLine();

        foreach (var e in entries.OrderBy(x => x.AudioHash, StringComparer.OrdinalIgnoreCase))
        {
            if (e.HasDat10)
            {
                sb.AppendLine($"data_file \"AUDIO_SYNTHDATA\" \"audioconfig/{EscapeLuaString(e.AudioHash)}_amp.dat\"");
            }

            sb.AppendLine($"data_file \"AUDIO_GAMEDATA\" \"audioconfig/{EscapeLuaString(e.AudioHash)}_game.dat\"");
            sb.AppendLine($"data_file \"AUDIO_SOUNDDATA\" \"audioconfig/{EscapeLuaString(e.AudioHash)}_sounds.dat\"");
            sb.AppendLine($"data_file \"AUDIO_WAVEPACK\" \"sfx/dlc_{EscapeLuaString(e.AudioHash)}\"");
            sb.AppendLine();
        }

        File.WriteAllText(path, sb.ToString().TrimEnd() + Environment.NewLine, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
    }

    private static string EscapeLuaString(string s)
    {
        if (s.Contains('\\', StringComparison.Ordinal) || s.Contains('"', StringComparison.Ordinal))
            throw new ArgumentException("Audio hash cannot contain \\ or \" characters.", nameof(s));
        return s;
    }
}