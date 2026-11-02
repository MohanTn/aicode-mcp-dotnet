namespace Aicode.Mcp.Cli;

/// <summary>Reads "--name value" options from an argument list.</summary>
public static class ArgumentReader
{
    public static string? Option(IReadOnlyList<string> args, string name)
    {
        for (var i = 0; i < args.Count - 1; i++)
        {
            if (args[i] == name)
            {
                return args[i + 1];
            }
        }

        return null;
    }
}
