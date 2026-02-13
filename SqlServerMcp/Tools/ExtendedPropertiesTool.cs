using System.ComponentModel;
using ModelContextProtocol.Server;
using SqlServerMcp.Services;

namespace SqlServerMcp.Tools;

[McpServerToolType]
public sealed class ExtendedPropertiesTool
{
    private readonly ISchemaExplorationService _service;
    private readonly IRateLimitingService _rateLimiter;

    public ExtendedPropertiesTool(ISchemaExplorationService service, IRateLimitingService rateLimiter)
    {
        _service = service;
        _rateLimiter = rateLimiter;
    }

    [McpServerTool(
        Name = "get_extended_properties",
        Title = "Get Extended Properties",
        ReadOnly = true,
        Destructive = false,
        Idempotent = true)]
    [Description("Read extended properties (descriptions, metadata) from tables and columns. Returns JSON with schema, table, column, property name, and value.")]
    public async Task<string> GetProperties(
        [Description("Server name from list_servers")] string serverName,
        [Description("Database name from list_databases")] string databaseName,
        [Description("Optional schema name filter")] string? schemaName = null,
        [Description("Optional table name filter")] string? tableName = null,
        [Description("Optional column name filter")] string? columnName = null,
        CancellationToken cancellationToken = default)
    {
        return await ToolHelper.ExecuteAsync(_rateLimiter, () =>
            _service.GetExtendedPropertiesAsync(
                serverName, databaseName,
                schemaName, tableName, columnName,
                cancellationToken), cancellationToken);
    }
}
