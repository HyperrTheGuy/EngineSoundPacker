namespace ScoobSoundPacker;

/// <summary>Simple, consistent colored console output for Windows terminals.</summary>
internal static class ConsoleUi
{
    internal static void Banner()
    {
        Line(ConsoleColor.DarkCyan, "ScoobSoundPacker");
        Line(ConsoleColor.DarkGray, "Combine multiple FiveM engine audio resources into one folder.");
        Console.WriteLine();
    }

    internal static void Header(string text)
    {
        Line(ConsoleColor.Cyan, text);
    }

    internal static void Info(string text)
    {
        Line(ConsoleColor.Gray, text);
    }

    internal static void Muted(string text)
    {
        Line(ConsoleColor.DarkGray, text);
    }

    internal static void Success(string text)
    {
        Line(ConsoleColor.Green, text);
    }

    internal static void Warning(string text)
    {
        Line(ConsoleColor.Yellow, text);
    }

    internal static void Error(string text)
    {
        Line(ConsoleColor.Red, text, stderr: true);
    }

    internal static void Prompt(string label)
    {
        Console.ForegroundColor = ConsoleColor.White;
        Console.Write(label);
        Console.ResetColor();
    }

    private static void Line(ConsoleColor color, string text, bool stderr = false)
    {
        var w = stderr ? Console.Error : Console.Out;
        Console.ForegroundColor = color;
        w.WriteLine(text);
        Console.ResetColor();
    }
}