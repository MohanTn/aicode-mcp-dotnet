using Aicode.Mcp.Core.Architect;
using Aicode.Mcp.Core.Projects;

namespace Aicode.Mcp.Core.Reviewer;

/// <summary>
/// Pairs every changed file in a merge request with the architect rules for its template.
/// The calling agent does the judging, then posts findings back with the post-review-comments tool.
/// </summary>
public class MergeRequestReviewer(
    IMergeRequestProvider provider,
    RuleFinder ruleFinder,
    DesignPatternFinder patternFinder)
{
    public const int MaxDiffCharacters = 8192;

    private const string Instructions =
        "Check each file's diff against its rules. Report must_not_do and must_do violations as blocking, " +
        "should_do as suggestions. Files listed in files_without_rules have no matching rules, so review them for general quality. " +
        "When done, call post-review-comments with a short summary.";

    public async Task<MergeRequestReview> ReviewAsync(
        string project,
        int mergeRequestIid,
        string workspacePath,
        CancellationToken cancellationToken)
    {
        var mr = await provider.GetMergeRequestAsync(project, mergeRequestIid, cancellationToken);
        var files = new List<FileReview>();
        var withoutRules = new List<string>();

        foreach (var change in mr.Changes)
        {
            var diff = change.Diff.Length > MaxDiffCharacters ? change.Diff[..MaxDiffCharacters] : change.Diff;
            var truncated = diff.Length < change.Diff.Length;
            if (change.DeletedFile)
            {
                files.Add(new FileReview(change.OldPath, change.Status, null, diff, truncated, Array.Empty<DesignPatternMatch>(), null));
                continue;
            }

            var context = ProjectContext.Resolve(change.NewPath, workspacePath);
            var template = context.Project?.SourceTemplate;
            var patterns = template is null
                ? Array.Empty<DesignPatternMatch>()
                : patternFinder.Find(context.TemplatesPath, template, context.RelativePath);
            var rules = template is null ? null : ruleFinder.Find(context.TemplatesPath, template, context.RelativePath);

            if (rules is null)
            {
                withoutRules.Add(change.NewPath);
            }

            files.Add(new FileReview(change.NewPath, change.Status, template, diff, truncated, patterns, rules));
        }

        return new MergeRequestReview(mr.Title, mr.SourceBranch, mr.TargetBranch, mr.WebUrl, Instructions, files, withoutRules);
    }

    public Task PostSummaryAsync(string project, int mergeRequestIid, string body, CancellationToken cancellationToken) =>
        provider.PostNoteAsync(project, mergeRequestIid, body, cancellationToken);
}
