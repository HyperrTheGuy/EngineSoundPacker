using ScoobSoundPacker.Models;

namespace ScoobSoundPacker;

internal static class Program
{
    private static int Main(string[] args)
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;

        try
        {
            if (args.Length == 0)
                return RunInteractive();

            if (IsHelp(args[0]))
            {
                PrintHelp();
                return 0;
            }

            if (args[0].Equals("pack", StringComparison.OrdinalIgnoreCase))
                return RunPack(ParsePackArgs(args.AsSpan(1)), showBanner: true);

            return UnknownCommand(args[0]);
        }
        catch (Exception ex)
        {
            ConsoleUi.Error(ex.Message);
            return 1;
        }
    }

    private static int UnknownCommand(string cmd)
    {
        ConsoleUi.Error($"Unknown command: {cmd}");
        PrintHelp();
        return 1;
    }

    private static void PrintHelp()
    {
        ConsoleUi.Banner();
        ConsoleUi.Header("Usage");
        ConsoleUi.Info("  ScoobSoundPacker pack --input <folder> --output <folder> [--overwrite] [--dat10 h1,h2]");
        ConsoleUi.Info("  ScoobSoundPacker pack -i <folder> -o <folder> [--overwrite] [--dat10 h1,h2]");
        ConsoleUi.Info("  ScoobSoundPacker");
        ConsoleUi.Info("      Run without arguments for guided prompts.");
        Console.WriteLine();
        ConsoleUi.Header("Input folder");
        ConsoleUi.Info("  Should contain one subfolder per engine sound resource (audioconfig / sfx / fxmanifest.lua / __resource.lua).");
        ConsoleUi.Info("  If there are no subfolders, the input folder itself is treated as a single resource.");
        Console.WriteLine();
        ConsoleUi.Header("Options");
        ConsoleUi.Info("  --overwrite   Replace files when the same path exists in multiple sources.");
        ConsoleUi.Info("  --dat10       Comma-separated audio hashes to force synth/dat10 lines in the manifest.");
        Console.WriteLine();
    }

    private static bool IsHelp(string s) =>
        s is "-h" or "--help" or "/?" or "help";

    private sealed record PackArgs(string InputRoot, string OutputRoot, bool Overwrite, HashSet<string> Dat10Hashes);

    private static PackArgs ParsePackArgs(ReadOnlySpan<string> args)
    {
        string input = "";
        string output = "";
        var overwrite = false;
        var dat10 = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        for (var i = 0; i < args.Length; i++)
        {
            var a = args[i];
            if (a is "-i" or "--input")
                input = Expect(args, ref i, "input folder");
            else if (a is "-o" or "--output")
                output = Expect(args, ref i, "output folder");
            else if (a is "--overwrite")
                overwrite = true;
            else if (a is "--dat10")
                foreach (var h in Expect(args, ref i, "comma-separated hashes").Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                    dat10.Add(h);
            else
                throw new ArgumentException($"Unknown option: {a}");
        }

        if (string.IsNullOrWhiteSpace(input))
            throw new ArgumentException("Missing --input <folder>.");
        if (string.IsNullOrWhiteSpace(output))
            throw new ArgumentException("Missing --output <folder>.");

        return new PackArgs(input.Trim(), output.Trim(), overwrite, dat10);
    }

    private static string Expect(ReadOnlySpan<string> args, ref int i, string label)
    {
        if (i + 1 >= args.Length)
            throw new ArgumentException($"Missing value after option ({label}).");
        i++;
        return args[i].Trim();
    }

    private static int RunPack(PackArgs a, bool showBanner)
    {
        if (showBanner)
            ConsoleUi.Banner();

        var sources = ResourceDiscovery.ResolveSourceFolders(a.InputRoot);
        var svc = new ResourceCombineService();
        var result = svc.MergeFolders(sources, a.OutputRoot, a.Overwrite);

        PrintPerSourcePackReport(result);

        var entries = ApplyDat10Overrides(result.Entries, a.Dat10Hashes);
        var outRoot = Path.GetFullPath(a.OutputRoot);
        ResourceCombineService.EnsureFolderLayout(outRoot, entries);
        FxManifestWriter.Write(outRoot, entries);

        Console.WriteLine();
        ConsoleUi.Success("Done.");

        ConsoleUi.Header("Summary");
        var totalPacked = result.SourceReports.Sum(r => r.CopiedRelativePaths.Count);
        var totalSkipped = result.SourceReports.Sum(r => r.SkippedRelativePaths.Count);
        ConsoleUi.Info($"  Input root:      {Path.GetFullPath(a.InputRoot)}");
        ConsoleUi.Info($"  Output folder:   {outRoot}");
        ConsoleUi.Info($"  Resources:       {sources.Count}");
        ConsoleUi.Info($"  Files copied:    {totalPacked}");
        if (totalSkipped > 0)
            ConsoleUi.Warning($"  Files skipped:   {totalSkipped} (already existed; use --overwrite to replace)");
        ConsoleUi.Info($"  Sound slots:     {entries.Count}");

        Console.WriteLine();
        ConsoleUi.Info("Add the output folder as a single resource in server.cfg.");
        return 0;
    }

    private static void PrintPerSourcePackReport(CombineResult result)
    {
        ConsoleUi.Header("Packing");

        foreach (var report in result.SourceReports)
        {
            Console.WriteLine();
            ConsoleUi.Header($"{report.Index}/{report.Total}  {report.DisplayName}");
            ConsoleUi.Muted($"  {report.SourcePath}");

            foreach (var p in report.Problems)
                ConsoleUi.Warning($"  Problem: {p}");

            if (report.CopiedRelativePaths.Count > 0)
            {
                ConsoleUi.Info($"  Copied ({report.CopiedRelativePaths.Count}):");
                foreach (var rel in report.CopiedRelativePaths)
                    ConsoleUi.Muted($"    {rel}");
            }
            else if (report.Problems.Count == 0)
                ConsoleUi.Info("  Copied: (none)");

            if (report.SkippedRelativePaths.Count > 0)
            {
                ConsoleUi.Warning($"  Skipped — already exists ({report.SkippedRelativePaths.Count}):");
                foreach (var rel in report.SkippedRelativePaths)
                    ConsoleUi.Warning($"    {rel}");
            }
        }

        if (result.GlobalWarnings.Count > 0)
        {
            Console.WriteLine();
            ConsoleUi.Header("Notices");
            foreach (var w in result.GlobalWarnings)
                ConsoleUi.Warning($"  {w}");
        }
    }

    private static IReadOnlyList<SoundPackEntry> ApplyDat10Overrides(
        IReadOnlyList<SoundPackEntry> entries,
        HashSet<string> dat10Hashes)
    {
        if (dat10Hashes.Count == 0)
            return entries;

        var map = entries.ToDictionary(e => e.AudioHash, StringComparer.OrdinalIgnoreCase);
        foreach (var h in dat10Hashes)
        {
            ValidateHash(h);
            if (map.TryGetValue(h, out var e))
                map[h] = new SoundPackEntry { AudioHash = e.AudioHash, HasDat10 = true };
            else
                map[h] = new SoundPackEntry { AudioHash = h, HasDat10 = true };
        }

        return map.Values.OrderBy(v => v.AudioHash, StringComparer.OrdinalIgnoreCase).ToList();
    }

    private static int RunInteractive()
    {
        ConsoleUi.Banner();

        ConsoleUi.Header("Input");
        ConsoleUi.Info("Folder that contains your engine sound resources (each in its own subfolder).");
        ConsoleUi.Prompt("Path: ");
        var input = ReadLineOrEmpty();
        if (string.IsNullOrWhiteSpace(input))
        {
            ConsoleUi.Error("Input path is required.");
            return 1;
        }

        if (!Directory.Exists(input.Trim()))
        {
            ConsoleUi.Error("That folder does not exist.");
            return 1;
        }

        Console.WriteLine();
        ConsoleUi.Header("Output");
        ConsoleUi.Info("Folder for the combined resource (created if needed).");
        ConsoleUi.Prompt("Path: ");
        var output = ReadLineOrEmpty();
        if (string.IsNullOrWhiteSpace(output))
        {
            ConsoleUi.Error("Output path is required.");
            return 1;
        }

        Console.WriteLine();
        ConsoleUi.Prompt("Overwrite duplicate files from different sources? [y/N]: ");
        var overwrite = (ReadLineOrEmpty() ?? "").Trim().Equals("y", StringComparison.OrdinalIgnoreCase);

        Console.WriteLine();
        ConsoleUi.Info("Optional: comma-separated hashes that need dat10/synth lines (press Enter to skip).");
        ConsoleUi.Prompt("Hashes: ");
        var dat10Line = ReadLineOrEmpty() ?? "";
        var dat10 = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var h in dat10Line.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            dat10.Add(h);

        Console.WriteLine();
        return RunPack(new PackArgs(input.Trim(), output.Trim(), overwrite, dat10), showBanner: false);
    }

    private static string? ReadLineOrEmpty() => Console.ReadLine();

    private static void ValidateHash(string hash)
    {
        if (string.IsNullOrWhiteSpace(hash))
            throw new ArgumentException("Audio hash cannot be empty.");
        if (hash.Contains('\\') || hash.Contains('"') || hash.Contains('/') || hash.Contains(' '))
            throw new ArgumentException("Audio hash cannot contain spaces, slashes, or quotes.");
    }
}