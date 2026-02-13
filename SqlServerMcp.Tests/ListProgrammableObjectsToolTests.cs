using ModelContextProtocol;
using SqlServerMcp.Services;
using SqlServerMcp.Tools;

namespace SqlServerMcp.Tests;

public class ListProgrammableObjectsToolTests
{
    private readonly StubSchemaExplorationService _stub = new();
    private readonly ListProgrammableObjectsTool _tool;

    public ListProgrammableObjectsToolTests()
    {
        _tool = new ListProgrammableObjectsTool(_stub, new NoOpRateLimiter());
    }

    [Fact]
    public async Task ListObjects_DelegatesToService()
    {
        _stub.ListResult = """[{"name":"test"}]""";

        var result = await _tool.ListObjects("srv", "db", cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal("""[{"name":"test"}]""", result);
        Assert.Equal("srv", _stub.CapturedServerName);
        Assert.Equal("db", _stub.CapturedDatabaseName);
    }

    [Fact]
    public async Task ListObjects_ParsesSchemaFilters()
    {
        _stub.ListResult = "[]";

        await _tool.ListObjects("srv", "db", includeSchemas: "dbo,sales", cancellationToken: TestContext.Current.CancellationToken);

        Assert.NotNull(_stub.CapturedIncludeSchemas);
        Assert.Equal(2, _stub.CapturedIncludeSchemas!.Count);
        Assert.Equal("dbo", _stub.CapturedIncludeSchemas[0]);
        Assert.Equal("sales", _stub.CapturedIncludeSchemas[1]);
    }

    [Fact]
    public async Task ListObjects_ParsesObjectTypes()
    {
        _stub.ListResult = "[]";

        await _tool.ListObjects("srv", "db", objectTypes: "PROCEDURE,VIEW", cancellationToken: TestContext.Current.CancellationToken);

        Assert.NotNull(_stub.CapturedObjectTypes);
        Assert.Equal(2, _stub.CapturedObjectTypes!.Count);
    }

    [Fact]
    public async Task ListObjects_ArgumentException_WrappedAsMcpException()
    {
        _stub.ExceptionToThrow = new ArgumentException("Server 'bad' not found.");

        var ex = await Assert.ThrowsAsync<McpException>(
            () => _tool.ListObjects("bad", "db", cancellationToken: TestContext.Current.CancellationToken));

        Assert.Contains("Server 'bad' not found", ex.Message);
    }
}
