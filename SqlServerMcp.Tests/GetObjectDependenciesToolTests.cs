using ModelContextProtocol;
using SqlServerMcp.Services;
using SqlServerMcp.Tools;

namespace SqlServerMcp.Tests;

public class GetObjectDependenciesToolTests
{
    private readonly StubSchemaExplorationService _stub = new();
    private readonly GetObjectDependenciesTool _tool;

    public GetObjectDependenciesToolTests()
    {
        _tool = new GetObjectDependenciesTool(_stub, new NoOpRateLimiter());
    }

    [Fact]
    public async Task GetDependencies_DelegatesToService()
    {
        _stub.DependenciesResult = """{"references":[],"referencedBy":[]}""";

        var result = await _tool.GetDependencies("srv", "db", "vw_Test", cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal("""{"references":[],"referencedBy":[]}""", result);
        Assert.Equal("srv", _stub.CapturedServerName);
        Assert.Equal("db", _stub.CapturedDatabaseName);
        Assert.Equal("vw_Test", _stub.CapturedObjectName);
        Assert.Equal("dbo", _stub.CapturedSchemaName);
    }

    [Fact]
    public async Task GetDependencies_UsesExplicitSchema()
    {
        _stub.DependenciesResult = """{"references":[],"referencedBy":[]}""";

        await _tool.GetDependencies("srv", "db", "proc", "sales", cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal("sales", _stub.CapturedSchemaName);
    }

    [Fact]
    public async Task GetDependencies_ArgumentException_WrappedAsMcpException()
    {
        _stub.ExceptionToThrow = new ArgumentException("Server not found.");

        var ex = await Assert.ThrowsAsync<McpException>(
            () => _tool.GetDependencies("bad", "db", "obj", cancellationToken: TestContext.Current.CancellationToken));

        Assert.Contains("Server not found", ex.Message);
    }
}
