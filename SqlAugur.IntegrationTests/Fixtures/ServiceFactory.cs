using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using SqlAugur.Configuration;
using SqlAugur.Services;

namespace SqlAugur.IntegrationTests.Fixtures;

internal static class ServiceFactory
{
    internal static SqlAugurOptions BuildOptions(string connectionString, int maxRows = 1000)
    {
        return new SqlAugurOptions
        {
            Servers = new Dictionary<string, SqlServerConnection>
            {
                [SqlServerContainerFixture.ServerName] = new() { ConnectionString = connectionString }
            },
            MaxRows = maxRows,
            CommandTimeoutSeconds = 30
        };
    }

    internal static SqlServerService CreateSqlServerService(string connectionString, int maxRows = 1000)
    {
        var options = Options.Create(BuildOptions(connectionString, maxRows));
        return new SqlServerService(options, NullLogger<SqlServerService>.Instance);
    }

    internal static DiagramService CreateDiagramService(string connectionString)
    {
        var options = Options.Create(BuildOptions(connectionString));
        return new DiagramService(options, NullLogger<DiagramService>.Instance);
    }

    internal static SchemaOverviewService CreateSchemaOverviewService(string connectionString)
    {
        var options = Options.Create(BuildOptions(connectionString));
        return new SchemaOverviewService(options, NullLogger<SchemaOverviewService>.Instance);
    }

    internal static TableDescribeService CreateTableDescribeService(string connectionString)
    {
        var options = Options.Create(BuildOptions(connectionString));
        return new TableDescribeService(options, NullLogger<TableDescribeService>.Instance);
    }

    internal static SchemaExplorationService CreateSchemaExplorationService(string connectionString)
    {
        var options = Options.Create(BuildOptions(connectionString));
        return new SchemaExplorationService(options, NullLogger<SchemaExplorationService>.Instance);
    }
}
