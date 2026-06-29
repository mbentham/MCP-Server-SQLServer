using Microsoft.Data.SqlClient;
using ModelContextProtocol;
using SqlAugur.Services;

namespace SqlAugur.Tools;

/// <summary>
/// Helper methods for tool implementations.
/// </summary>
internal static class ToolHelper
{
    /// <summary>
    /// Executes an async tool operation with rate limiting and standardized exception handling.
    /// Acquires a rate limit lease before executing, and releases the concurrency slot on completion.
    /// A client cancellation (the request's token fired) is surfaced as an McpException with a clear
    /// "cancelled by the MCP client, not a server fault" message — re-throwing the bare
    /// OperationCanceledException would otherwise render as an opaque generic error. Converts
    /// ArgumentException, InvalidOperationException, and SqlException to McpException carrying their
    /// message; any other exception is converted to an McpException carrying its type name and message
    /// so failures are never opaque.
    /// </summary>
    public static async Task<string> ExecuteAsync(IRateLimitingService rateLimiter, Func<Task<string>> operation,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Acquired inside the try so rate-limit rejections (InvalidOperationException) are
            // translated to McpException with their message rather than escaping as an opaque error.
            using var lease = await rateLimiter.AcquireAsync(cancellationToken);
            return await operation();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // The request's cancellation token fired — the MCP client cancelled this call, typically
            // because its per-call timeout elapsed while the call was queued behind the concurrency
            // limiter, or because too many calls ran in parallel against the single stdio server.
            // Re-throwing the bare OperationCanceledException is rendered by the MCP framework as the
            // opaque "An error occurred invoking '<tool>'." — indistinguishable from a real connection
            // fault, which has caused this to be misdiagnosed as a SQL Server / network / VPN outage.
            // Surface a clear, self-identifying message instead so the cause is obvious from the client
            // and the logs. (An OperationCanceledException whose token was NOT cancelled is not a client
            // cancellation — the filter above excludes it, so it falls through to the catch-all and
            // surfaces its own type and message.)
            throw new McpException(
                "Request cancelled by the MCP client (typically its per-call timeout elapsed while this " +
                "call was queued behind the concurrency limit, or too many calls ran in parallel against " +
                "the single SqlAugur server). This is a client-side cancellation, not a SQL Server or " +
                "network fault — reduce concurrent calls or retry.");
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException or SqlException)
        {
            throw new McpException(ex.Message);
        }
        catch (Exception ex)
        {
            // Any other exception (e.g. SocketException from ephemeral-port exhaustion, IOException,
            // TimeoutException) would otherwise escape and the MCP framework would return an opaque
            // "An error occurred invoking '<tool>'." with no detail, masking the real cause. Surface
            // the exception type and message so the failure is diagnosable from the client and logs.
            throw new McpException($"{ex.GetType().Name}: {ex.Message}");
        }
    }

    /// <summary>
    /// Saves content to a file, validating the extension, creating directories as needed,
    /// and returning a confirmation message with the line count.
    /// </summary>
    public static async Task<string> SaveToFileAsync(
        string content, string outputPath, string allowedExtension,
        string description, CancellationToken cancellationToken)
    {
        var fullPath = Path.GetFullPath(outputPath);
        if (!Path.GetExtension(fullPath).Equals(allowedExtension, StringComparison.OrdinalIgnoreCase))
            throw new McpException($"Output path must have a {allowedExtension} file extension.");

        var directory = Path.GetDirectoryName(fullPath);
        if (directory is not null)
            Directory.CreateDirectory(directory);

        await File.WriteAllTextAsync(fullPath, content, cancellationToken);

        var lineCount = content.AsSpan().Count('\n');
        return $"{description} saved to {fullPath} ({lineCount} lines)";
    }

    /// <summary>
    /// Parses a comma-separated string into a deduplicated list.
    /// Returns null if the input is null, empty, or contains only whitespace/commas.
    /// </summary>
    public static IReadOnlyList<string>? ParseCommaSeparatedList(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return null;

        var items = input.Split(',')
            .Select(s => s.Trim())
            .Where(s => s.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        return items.Count > 0 ? items : null;
    }
}
