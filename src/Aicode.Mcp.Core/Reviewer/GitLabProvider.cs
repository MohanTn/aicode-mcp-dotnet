using System.Net.Http.Json;
using System.Text.Json;

namespace Aicode.Mcp.Core.Reviewer;

public class GitLabProvider(HttpClient http, GitLabOptions options) : IMergeRequestProvider
{
    public async Task<MergeRequestInfo> GetMergeRequestAsync(string project, int mergeRequestIid, CancellationToken cancellationToken)
    {
        using var request = CreateRequest(HttpMethod.Get, project, mergeRequestIid, "changes");
        using var response = await http.SendAsync(request, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);

        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
        var root = doc.RootElement;
        var changes = root.TryGetProperty("changes", out var array) && array.ValueKind == JsonValueKind.Array
            ? array.EnumerateArray().Select(ToChange).ToList()
            : new List<MergeRequestChange>();

        return new MergeRequestInfo(
            Text(root, "title") ?? "",
            Text(root, "description"),
            Text(root, "source_branch") ?? "",
            Text(root, "target_branch") ?? "",
            Text(root, "web_url") ?? "",
            changes);
    }

    public async Task PostNoteAsync(string project, int mergeRequestIid, string body, CancellationToken cancellationToken)
    {
        using var request = CreateRequest(HttpMethod.Post, project, mergeRequestIid, "notes");
        request.Content = JsonContent.Create(new { body });
        using var response = await http.SendAsync(request, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
    }

    private HttpRequestMessage CreateRequest(HttpMethod method, string project, int iid, string suffix)
    {
        if (string.IsNullOrWhiteSpace(options.Token))
        {
            throw new InvalidOperationException("GITLAB_TOKEN is not set. Create a token with the api scope and export it.");
        }

        var url = $"{options.BaseUrl.TrimEnd('/')}/api/v4/projects/{Uri.EscapeDataString(project)}/merge_requests/{iid}/{suffix}";
        var request = new HttpRequestMessage(method, url);
        request.Headers.Add("PRIVATE-TOKEN", options.Token);
        return request;
    }

    private static async Task EnsureSuccessAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new HttpRequestException($"GitLab returned {(int)response.StatusCode}: {body}");
        }
    }

    private static MergeRequestChange ToChange(JsonElement e) => new(
        Text(e, "old_path") ?? "",
        Text(e, "new_path") ?? "",
        Flag(e, "new_file"),
        Flag(e, "deleted_file"),
        Flag(e, "renamed_file"),
        Text(e, "diff") ?? "");

    private static string? Text(JsonElement e, string name) =>
        e.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;

    private static bool Flag(JsonElement e, string name) =>
        e.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.True;
}
