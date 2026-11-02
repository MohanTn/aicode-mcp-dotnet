using System.Collections.Concurrent;
using System.Text;
using System.Text.RegularExpressions;

namespace Aicode.Mcp.Core.Infrastructure;

/// <summary>
/// Glob matching on forward-slash paths. Supports **, *, ?, {a,b} and a leading ! for exclusions.
/// A path matches a list when it matches at least one positive glob and no negated glob.
/// </summary>
public static class GlobMatcher
{
    private static readonly ConcurrentDictionary<string, Regex> Cache = new();

    public static bool Matches(string path, IEnumerable<string>? globs)
    {
        if (globs is null)
        {
            return false;
        }

        var normalized = path.Replace('\\', '/');
        var list = globs.ToList();
        var positive = list.Where(g => !g.StartsWith('!')).ToList();
        var negative = list.Where(g => g.StartsWith('!')).Select(g => g[1..]).ToList();

        return positive.Any(g => IsMatch(normalized, g)) && !negative.Any(g => IsMatch(normalized, g));
    }

    public static bool IsMatch(string path, string glob) =>
        Cache.GetOrAdd(glob, ToRegex).IsMatch(path.Replace('\\', '/'));

    private static Regex ToRegex(string glob)
    {
        var sb = new StringBuilder("^");
        var braceDepth = 0;
        for (var i = 0; i < glob.Length; i++)
        {
            var c = glob[i];
            switch (c)
            {
                case '*' when i + 1 < glob.Length && glob[i + 1] == '*':
                    i++;
                    if (i + 1 < glob.Length && glob[i + 1] == '/')
                    {
                        i++;
                        sb.Append("(?:.*/)?");
                    }
                    else
                    {
                        sb.Append(".*");
                    }

                    break;
                case '*':
                    sb.Append("[^/]*");
                    break;
                case '?':
                    sb.Append("[^/]");
                    break;
                case '{':
                    braceDepth++;
                    sb.Append("(?:");
                    break;
                case '}' when braceDepth > 0:
                    braceDepth--;
                    sb.Append(')');
                    break;
                case ',' when braceDepth > 0:
                    sb.Append('|');
                    break;
                default:
                    sb.Append(Regex.Escape(c.ToString()));
                    break;
            }
        }

        sb.Append('$');
        return new Regex(sb.ToString(), RegexOptions.Compiled | RegexOptions.CultureInvariant);
    }
}
