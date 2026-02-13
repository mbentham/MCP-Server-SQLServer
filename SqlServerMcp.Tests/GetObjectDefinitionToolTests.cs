using ModelContextProtocol;
using SqlServerMcp.Services;
using SqlServerMcp.Tools;

namespace SqlServerMcp.Tests;

public class GetObjectDefinitionToolTests
{
    private readonly StubSchemaExplorationService _stub = new();
    private readonly GetObjectDefinitionTool _tool;

    public GetObjectDefinitionToolTests()
    {
        _tool = new GetObjectDefinitionTool(_stub, new NoOpRateLimiter());
    }

    [Fact]
    public async Task GetDefinition_DelegatesToService()
    {
        _stub.DefinitionResult = "## PROCEDURE: [dbo].[test]";

        var result = await _tool.GetDefinition("srv", "db", "test", cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal("## PROCEDURE: [dbo].[test]", result);
        Assert.Equal("srv", _stub.CapturedServerName);
        Assert.Equal("db", _stub.CapturedDatabaseName);
        Assert.Equal("test", _stub.CapturedObjectName);
        Assert.Equal("dbo", _stub.CapturedSchemaName);
    }

    [Fact]
    public async Task GetDefinition_UsesExplicitSchema()
    {
        _stub.DefinitionResult = "";

        await _tool.GetDefinition("srv", "db", "proc", "sales", cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal("sales", _stub.CapturedSchemaName);
    }

    [Fact]
    public async Task GetDefinition_ObjectNotFound_WrappedAsMcpException()
    {
        _stub.ExceptionToThrow = new ArgumentException("Object not found.");

        var ex = await Assert.ThrowsAsync<McpException>(
            () => _tool.GetDefinition("srv", "db", "missing", cancellationToken: TestContext.Current.CancellationToken));

        Assert.Contains("Object not found", ex.Message);
    }
}
