using System.Net.Sockets;
using System.Reflection;
using Microsoft.Data.SqlClient;
using ModelContextProtocol;
using SqlAugur.Services;
using SqlAugur.Tools;

namespace SqlAugur.Tests;

public class ToolHelperTests
{
    // ───────────────────────────────────────────────
    // ExecuteAsync — exception wrapping
    // ───────────────────────────────────────────────

    [Fact]
    public async Task ExecuteAsync_SqlException_WrapsAsMcpException()
    {
        var rateLimiter = new NoOpRateLimiter();
        var sqlEx = CreateSqlException("Test SQL error");

        var mcpEx = await Assert.ThrowsAsync<McpException>(
            () => ToolHelper.ExecuteAsync(rateLimiter, () => throw sqlEx, TestContext.Current.CancellationToken));

        Assert.Contains("Test SQL error", mcpEx.Message);
    }

    [Fact]
    public async Task ExecuteAsync_SocketException_WrapsAsMcpExceptionWithDetail()
    {
        // A SocketException is a transport-level exception type that ToolHelper does not explicitly
        // translate (it is not Argument/InvalidOperation/SqlException), so it used to escape as an
        // opaque "An error occurred invoking '<tool>'." with no detail. The broad catch-all must now
        // surface it as an McpException carrying the exception type and message so any such failure
        // is diagnosable rather than indistinguishable from a generic error.
        var rateLimiter = new NoOpRateLimiter();
        var socketEx = new SocketException(10055); // any socket error code — the value is irrelevant to the test

        var mcpEx = await Assert.ThrowsAsync<McpException>(
            () => ToolHelper.ExecuteAsync(rateLimiter, () => throw socketEx, TestContext.Current.CancellationToken));

        Assert.Contains(nameof(SocketException), mcpEx.Message);
    }

    [Fact]
    public async Task ExecuteAsync_RateLimiterThrows_WrapsAsMcpExceptionWithMessage()
    {
        // Rate-limit rejections ("Rate limit exceeded...", "Too many concurrent queries...") are
        // thrown by AcquireAsync. They carry a useful message but used to escape uncaught because
        // the lease was acquired outside the try, so the framework returned an opaque generic error.
        // The message must now be surfaced via McpException.
        var rateLimiter = new ThrowingRateLimiter(
            new InvalidOperationException("Rate limit exceeded. Too many queries per minute. Please wait and try again."));

        var mcpEx = await Assert.ThrowsAsync<McpException>(
            () => ToolHelper.ExecuteAsync(rateLimiter, () => Task.FromResult("unreached"), TestContext.Current.CancellationToken));

        Assert.Contains("Rate limit exceeded", mcpEx.Message);
    }

    private sealed class ThrowingRateLimiter(Exception toThrow) : IRateLimitingService
    {
        public Task<IDisposable> AcquireAsync(CancellationToken cancellationToken)
            => Task.FromException<IDisposable>(toThrow);
    }

    [Fact]
    public async Task ExecuteAsync_OperationCanceled_WhenTokenCancelled_SurfacesClearCancellationMessage()
    {
        // When the request's token fires, the MCP client cancelled the call (e.g. its per-call timeout
        // elapsed while the call queued behind the concurrency limiter). Re-throwing the bare
        // OperationCanceledException is rendered by the framework as the opaque "An error occurred
        // invoking '<tool>'." — indistinguishable from a real connection fault, which got this
        // misdiagnosed as a server/network/VPN outage. It must instead surface a clear McpException
        // identifying it as a client-side cancellation.
        var rateLimiter = new NoOpRateLimiter();
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        var mcpEx = await Assert.ThrowsAsync<McpException>(
            () => ToolHelper.ExecuteAsync(rateLimiter,
                () => throw new OperationCanceledException(cts.Token), cts.Token));

        Assert.Contains("cancelled by the MCP client", mcpEx.Message);
        Assert.Contains("not a SQL Server or network fault", mcpEx.Message);
    }

    [Fact]
    public async Task ExecuteAsync_OperationCanceled_WhenTokenNotCancelled_WrapsAsMcpExceptionWithType()
    {
        // An OperationCanceledException whose token was NOT cancelled is not a client cancellation
        // (e.g. an internal/library timeout surfacing as OCE). The IsCancellationRequested filter must
        // exclude it so it is not mislabelled as a client cancellation — it falls through to the
        // catch-all and surfaces its own exception type so the real cause stays visible.
        var rateLimiter = new NoOpRateLimiter();
        using var cts = new CancellationTokenSource(); // never cancelled

        var mcpEx = await Assert.ThrowsAsync<McpException>(
            () => ToolHelper.ExecuteAsync(rateLimiter,
                () => throw new OperationCanceledException("inner timeout, token not cancelled"),
                cts.Token));

        Assert.Contains(nameof(OperationCanceledException), mcpEx.Message);
    }

    private static SqlException CreateSqlException(string message)
    {
        // SqlException has no public constructor; use reflection to build one for testing.
        var errorCollection = (SqlErrorCollection)Activator.CreateInstance(typeof(SqlErrorCollection), nonPublic: true)!;

        var errorCtors = typeof(SqlError).GetConstructors(BindingFlags.NonPublic | BindingFlags.Instance);
        var error = (SqlError)errorCtors[0].Invoke([50000, (byte)1, (byte)17, "server", message, "", 0, 0, null]);

        typeof(SqlErrorCollection)
            .GetMethod("Add", BindingFlags.NonPublic | BindingFlags.Instance)!
            .Invoke(errorCollection, [error]);

        var createMethod = typeof(SqlException)
            .GetMethod("CreateException", BindingFlags.NonPublic | BindingFlags.Static,
                null, [typeof(SqlErrorCollection), typeof(string)], null)!;

        return (SqlException)createMethod.Invoke(null, [errorCollection, "16.0.0"])!;
    }

    // ───────────────────────────────────────────────
    // SaveToFileAsync
    // ───────────────────────────────────────────────

    [Fact]
    public async Task SaveToFileAsync_ValidWrite_ReturnsConfirmation()
    {
        var dir = Path.Combine(Path.GetTempPath(), $"th_{Guid.NewGuid():N}");
        try
        {
            var path = Path.Combine(dir, "test.sqlplan");
            var content = "line1\nline2\nline3";

            var result = await ToolHelper.SaveToFileAsync(content, path, ".sqlplan", "Execution plan", TestContext.Current.CancellationToken);

            Assert.Contains("Execution plan saved to", result);
            Assert.Contains(Path.GetFullPath(path), result);
            Assert.Contains("2 lines", result);
            Assert.Equal(content, await File.ReadAllTextAsync(path, TestContext.Current.CancellationToken));
        }
        finally
        {
            if (Directory.Exists(dir))
                Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public async Task SaveToFileAsync_WrongExtension_ThrowsMcpException()
    {
        var path = Path.Combine(Path.GetTempPath(), "test.txt");

        var ex = await Assert.ThrowsAsync<McpException>(
            () => ToolHelper.SaveToFileAsync("content", path, ".sqlplan", "Plan", TestContext.Current.CancellationToken));

        Assert.Contains(".sqlplan", ex.Message);
    }

    [Fact]
    public async Task SaveToFileAsync_CreatesDirectory()
    {
        var dir = Path.Combine(Path.GetTempPath(), $"th_{Guid.NewGuid():N}", "sub");
        try
        {
            var path = Path.Combine(dir, "output.puml");

            await ToolHelper.SaveToFileAsync("@startuml\n@enduml", path, ".puml", "Diagram", TestContext.Current.CancellationToken);

            Assert.True(File.Exists(path));
        }
        finally
        {
            var parent = Path.GetDirectoryName(dir)!;
            if (Directory.Exists(parent))
                Directory.Delete(parent, recursive: true);
        }
    }

    // ───────────────────────────────────────────────
    // ParseCommaSeparatedList
    // ───────────────────────────────────────────────

    [Fact]
    public void ParseCommaSeparatedList_Null_ReturnsNull()
    {
        Assert.Null(ToolHelper.ParseCommaSeparatedList(null));
    }

    [Fact]
    public void ParseCommaSeparatedList_Empty_ReturnsNull()
    {
        Assert.Null(ToolHelper.ParseCommaSeparatedList(""));
    }

    [Fact]
    public void ParseCommaSeparatedList_Whitespace_ReturnsNull()
    {
        Assert.Null(ToolHelper.ParseCommaSeparatedList("   "));
    }

    [Fact]
    public void ParseCommaSeparatedList_SingleValue_ReturnsSingleElement()
    {
        var result = ToolHelper.ParseCommaSeparatedList("audit");

        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Equal("audit", result[0]);
    }

    [Fact]
    public void ParseCommaSeparatedList_MultipleValues_ReturnsAll()
    {
        var result = ToolHelper.ParseCommaSeparatedList("audit,staging,temp");

        Assert.NotNull(result);
        Assert.Equal(3, result.Count);
        Assert.Equal("audit", result[0]);
        Assert.Equal("staging", result[1]);
        Assert.Equal("temp", result[2]);
    }

    [Fact]
    public void ParseCommaSeparatedList_TrimsWhitespace()
    {
        var result = ToolHelper.ParseCommaSeparatedList(" audit , staging ");

        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.Equal("audit", result[0]);
        Assert.Equal("staging", result[1]);
    }

    [Fact]
    public void ParseCommaSeparatedList_Duplicates_CaseInsensitive_Deduplicates()
    {
        var result = ToolHelper.ParseCommaSeparatedList("audit,Audit,AUDIT");

        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Equal("audit", result[0]);
    }

    [Fact]
    public void ParseCommaSeparatedList_ConsecutiveCommas_SkipsEmptyEntries()
    {
        var result = ToolHelper.ParseCommaSeparatedList("audit,,staging,,,temp");

        Assert.NotNull(result);
        Assert.Equal(3, result.Count);
        Assert.Equal("audit", result[0]);
        Assert.Equal("staging", result[1]);
        Assert.Equal("temp", result[2]);
    }

    [Fact]
    public void ParseCommaSeparatedList_OnlyCommas_ReturnsNull()
    {
        Assert.Null(ToolHelper.ParseCommaSeparatedList(",,,"));
    }
}
