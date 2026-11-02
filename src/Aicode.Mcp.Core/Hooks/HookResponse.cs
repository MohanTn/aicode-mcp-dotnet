namespace Aicode.Mcp.Core.Hooks;

/// <summary>Decision is "allow", "deny" or "skip" (the hook has nothing to say).</summary>
public record HookResponse(string Decision, string Message)
{
    public const string Allow = "allow";
    public const string Deny = "deny";
    public const string Skip = "skip";

    public static HookResponse Skipped(string message) => new(Skip, message);

    public static HookResponse Allowed(string message) => new(Allow, message);

    public static HookResponse Denied(string message) => new(Deny, message);
}
