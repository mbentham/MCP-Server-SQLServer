using ModelContextProtocol;
using SqlServerMcp.Services;
using SqlServerMcp.Tools;

namespace SqlServerMcp.Tests;

public class ExtendedPropertiesToolTests
{
    private readonly StubSchemaExplorationService _stub = new();
    private readonly ExtendedPropertiesTool _tool;

    public ExtendedPropertiesToolTests()
    {
        _tool = new ExtendedPropertiesTool(_stub, new NoOpRateLimiter());
    }

    [Fact]
    public async Task GetProperties_DelegatesToService()
    {
        _stub.ExtendedPropertiesResult = """[{"propertyName":"MS_Description"}]""";

        var result = await _tool.GetProperties("srv", "db", cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal("""[{"propertyName":"MS_Description"}]""", result);
        Assert.Equal("srv", _stub.CapturedServerName);
        Assert.Equal("db", _stub.CapturedDatabaseName);
    }

    [Fact]
    public async Task GetProperties_PassesFilters()
    {
        _stub.ExtendedPropertiesResult = "[]";

        await _tool.GetProperties("srv", "db", schemaName: "dbo", tableName: "Products", columnName: "Name",
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal("dbo", _stub.CapturedEpSchemaName);
        Assert.Equal("Products", _stub.CapturedEpTableName);
        Assert.Equal("Name", _stub.CapturedEpColumnName);
    }

    [Fact]
    public async Task GetProperties_NullFilters_PassedAsNull()
    {
        _stub.ExtendedPropertiesResult = "[]";

        await _tool.GetProperties("srv", "db", cancellationToken: TestContext.Current.CancellationToken);

        Assert.Null(_stub.CapturedEpSchemaName);
        Assert.Null(_stub.CapturedEpTableName);
        Assert.Null(_stub.CapturedEpColumnName);
    }

    [Fact]
    public async Task GetProperties_ArgumentException_WrappedAsMcpException()
    {
        _stub.ExceptionToThrow = new ArgumentException("Server not found.");

        var ex = await Assert.ThrowsAsync<McpException>(
            () => _tool.GetProperties("bad", "db", cancellationToken: TestContext.Current.CancellationToken));

        Assert.Contains("Server not found", ex.Message);
    }
}
