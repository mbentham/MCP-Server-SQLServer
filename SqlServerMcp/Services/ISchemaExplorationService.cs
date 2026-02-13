namespace SqlServerMcp.Services;

public interface ISchemaExplorationService
{
    Task<string> ListProgrammableObjectsAsync(
        string serverName, string databaseName,
        IReadOnlyList<string>? includeSchemas, IReadOnlyList<string>? excludeSchemas,
        IReadOnlyList<string>? objectTypes,
        CancellationToken cancellationToken);

    Task<string> GetObjectDefinitionAsync(
        string serverName, string databaseName,
        string objectName, string schemaName,
        CancellationToken cancellationToken);

    Task<string> GetExtendedPropertiesAsync(
        string serverName, string databaseName,
        string? schemaName, string? tableName, string? columnName,
        CancellationToken cancellationToken);

    Task<string> GetObjectDependenciesAsync(
        string serverName, string databaseName,
        string objectName, string schemaName,
        CancellationToken cancellationToken);
}
