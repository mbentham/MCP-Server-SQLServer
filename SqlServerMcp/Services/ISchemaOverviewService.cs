namespace SqlServerMcp.Services;

public interface ISchemaOverviewService
{
    Task<string> GenerateOverviewAsync(string serverName, string databaseName,
        string? schemaFilter, int maxTables, CancellationToken cancellationToken);
}
