using System.ComponentModel;
using ModelContextProtocol.Server;
using SqlServerMcp.Services;

namespace SqlServerMcp.Tools;

[McpServerToolType]
public sealed class ListProgrammableObjectsTool
{
    private readonly ISchemaExplorationService _service;
    private readonly IRateLimitingService _rateLimiter;

    public ListProgrammableObjectsTool(ISchemaExplorationService service, IRateLimitingService rateLimiter)
    {
        _service = service;
        _rateLimiter = rateLimiter;
    }

    [McpServerTool(
        Name = "list_programmable_objects",
        Title = "List Programmable Objects",
        ReadOnly = true,
        Destructive = false,
        Idempotent = true)]
    [Description("List stored procedures, functions, views, and triggers in a database. Returns JSON with schema, name, type, and create/modify dates.")]
    public async Task<string> ListObjects(
        [Description("Server name from list_servers")] string serverName,
        [Description("Database name from list_databases")] string databaseName,
        [Description("Optional comma-separated schemas to include (e.g. 'dbo,sales'). Overrides excludeSchemas.")] string? includeSchemas = null,
        [Description("Optional comma-separated schemas to exclude (e.g. 'sys,INFORMATION_SCHEMA'). Ignored if includeSchemas set.")] string? excludeSchemas = null,
        [Description("Optional comma-separated object types to filter: PROCEDURE, FUNCTION, VIEW, TRIGGER")] string? objectTypes = null,
        CancellationToken cancellationToken = default)
    {
        return await ToolHelper.ExecuteAsync(_rateLimiter, () =>
            _service.ListProgrammableObjectsAsync(
                serverName, databaseName,
                ToolHelper.ParseCommaSeparatedList(includeSchemas),
                ToolHelper.ParseCommaSeparatedList(excludeSchemas),
                ToolHelper.ParseCommaSeparatedList(objectTypes),
                cancellationToken), cancellationToken);
    }
}
