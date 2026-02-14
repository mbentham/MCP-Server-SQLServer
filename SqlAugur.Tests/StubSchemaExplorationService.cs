using SqlAugur.Services;

namespace SqlAugur.Tests;

internal sealed class StubSchemaExplorationService : ISchemaExplorationService
{
    public string ListResult { get; set; } = "[]";
    public string DefinitionResult { get; set; } = "";
    public string ExtendedPropertiesResult { get; set; } = "[]";
    public string DependenciesResult { get; set; } = """{"references":[],"referencedBy":[]}""";
    public Exception? ExceptionToThrow { get; set; }

    // Captured parameters
    public string? CapturedServerName { get; private set; }
    public string? CapturedDatabaseName { get; private set; }
    public string? CapturedObjectName { get; private set; }
    public string? CapturedSchemaName { get; private set; }
    public IReadOnlyList<string>? CapturedIncludeSchemas { get; private set; }
    public IReadOnlyList<string>? CapturedExcludeSchemas { get; private set; }
    public IReadOnlyList<string>? CapturedObjectTypes { get; private set; }
    public string? CapturedEpSchemaName { get; private set; }
    public string? CapturedEpTableName { get; private set; }
    public string? CapturedEpColumnName { get; private set; }

    public Task<string> ListProgrammableObjectsAsync(
        string serverName, string databaseName,
        IReadOnlyList<string>? includeSchemas, IReadOnlyList<string>? excludeSchemas,
        IReadOnlyList<string>? objectTypes,
        CancellationToken cancellationToken)
    {
        if (ExceptionToThrow is not null) throw ExceptionToThrow;
        CapturedServerName = serverName;
        CapturedDatabaseName = databaseName;
        CapturedIncludeSchemas = includeSchemas;
        CapturedExcludeSchemas = excludeSchemas;
        CapturedObjectTypes = objectTypes;
        return Task.FromResult(ListResult);
    }

    public Task<string> GetObjectDefinitionAsync(
        string serverName, string databaseName,
        string objectName, string schemaName,
        CancellationToken cancellationToken)
    {
        if (ExceptionToThrow is not null) throw ExceptionToThrow;
        CapturedServerName = serverName;
        CapturedDatabaseName = databaseName;
        CapturedObjectName = objectName;
        CapturedSchemaName = schemaName;
        return Task.FromResult(DefinitionResult);
    }

    public Task<string> GetExtendedPropertiesAsync(
        string serverName, string databaseName,
        string? schemaName, string? tableName, string? columnName,
        CancellationToken cancellationToken)
    {
        if (ExceptionToThrow is not null) throw ExceptionToThrow;
        CapturedServerName = serverName;
        CapturedDatabaseName = databaseName;
        CapturedEpSchemaName = schemaName;
        CapturedEpTableName = tableName;
        CapturedEpColumnName = columnName;
        return Task.FromResult(ExtendedPropertiesResult);
    }

    public Task<string> GetObjectDependenciesAsync(
        string serverName, string databaseName,
        string objectName, string schemaName,
        CancellationToken cancellationToken)
    {
        if (ExceptionToThrow is not null) throw ExceptionToThrow;
        CapturedServerName = serverName;
        CapturedDatabaseName = databaseName;
        CapturedObjectName = objectName;
        CapturedSchemaName = schemaName;
        return Task.FromResult(DependenciesResult);
    }
}
