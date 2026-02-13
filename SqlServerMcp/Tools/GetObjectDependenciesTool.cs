using System.ComponentModel;
using ModelContextProtocol.Server;
using SqlServerMcp.Services;

namespace SqlServerMcp.Tools;

[McpServerToolType]
public sealed class GetObjectDependenciesTool
{
    private readonly ISchemaExplorationService _service;
    private readonly IRateLimitingService _rateLimiter;

    public GetObjectDependenciesTool(ISchemaExplorationService service, IRateLimitingService rateLimiter)
    {
        _service = service;
        _rateLimiter = rateLimiter;
    }

    [McpServerTool(
        Name = "get_object_dependencies",
        Title = "Get Object Dependencies",
        ReadOnly = true,
        Destructive = false,
        Idempotent = true)]
    [Description("Show what a database object references and what references it. Returns JSON with 'references' and 'referencedBy' arrays for dependency analysis.")]
    public async Task<string> GetDependencies(
        [Description("Server name from list_servers")] string serverName,
        [Description("Database name from list_databases")] string databaseName,
        [Description("Object name (e.g. 'vw_ActiveProducts')")] string objectName,
        [Description("Schema name (default 'dbo')")] string schemaName = "dbo",
        CancellationToken cancellationToken = default)
    {
        return await ToolHelper.ExecuteAsync(_rateLimiter, () =>
            _service.GetObjectDependenciesAsync(
                serverName, databaseName,
                objectName, schemaName,
                cancellationToken), cancellationToken);
    }
}
