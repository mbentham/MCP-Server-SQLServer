using System.ComponentModel;
using ModelContextProtocol.Server;
using SqlAugur.Services;

namespace SqlAugur.Tools;

[McpServerToolType]
public sealed class GetObjectDefinitionTool
{
    private readonly ISchemaExplorationService _service;
    private readonly IRateLimitingService _rateLimiter;

    public GetObjectDefinitionTool(ISchemaExplorationService service, IRateLimitingService rateLimiter)
    {
        _service = service;
        _rateLimiter = rateLimiter;
    }

    [McpServerTool(
        Name = "get_object_definition",
        Title = "Get Object Definition",
        ReadOnly = true,
        Destructive = false,
        Idempotent = true)]
    [Description("Get the T-SQL source code of a stored procedure, function, view, or trigger. Returns Markdown with the object type and definition.")]
    public async Task<string> GetDefinition(
        [Description("Server name from list_servers")] string serverName,
        [Description("Database name from list_databases")] string databaseName,
        [Description("Object name (e.g. 'usp_GetOrders')")] string objectName,
        [Description("Schema name (default 'dbo')")] string schemaName = "dbo",
        CancellationToken cancellationToken = default)
    {
        return await ToolHelper.ExecuteAsync(_rateLimiter, () =>
            _service.GetObjectDefinitionAsync(
                serverName, databaseName,
                objectName, schemaName,
                cancellationToken), cancellationToken);
    }
}
