using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using SqlServerMcp.Configuration;
using SqlServerMcp.Services;

namespace SqlServerMcp.Tests;

public class SchemaExplorationServiceTests
{
    private static SchemaExplorationService CreateService(SqlServerMcpOptions? options = null)
    {
        options ??= new SqlServerMcpOptions
        {
            Servers = new Dictionary<string, SqlServerConnection>
            {
                ["test"] = new() { ConnectionString = "Server=test-only;Encrypt=True;TrustServerCertificate=False;" }
            }
        };
        return new SchemaExplorationService(Options.Create(options), NullLogger<SchemaExplorationService>.Instance);
    }

    [Fact]
    public async Task ListProgrammableObjects_InvalidServer_ThrowsArgumentException()
    {
        var service = CreateService();

        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.ListProgrammableObjectsAsync("nonexistent", "db", null, null, null, CancellationToken.None));
    }

    [Fact]
    public async Task GetObjectDefinition_InvalidServer_ThrowsArgumentException()
    {
        var service = CreateService();

        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.GetObjectDefinitionAsync("nonexistent", "db", "proc", "dbo", CancellationToken.None));
    }

    [Fact]
    public async Task GetExtendedProperties_InvalidServer_ThrowsArgumentException()
    {
        var service = CreateService();

        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.GetExtendedPropertiesAsync("nonexistent", "db", null, null, null, CancellationToken.None));
    }

    [Fact]
    public async Task GetObjectDependencies_InvalidServer_ThrowsArgumentException()
    {
        var service = CreateService();

        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.GetObjectDependenciesAsync("nonexistent", "db", "obj", "dbo", CancellationToken.None));
    }

    [Fact]
    public async Task ListProgrammableObjects_InvalidObjectType_ThrowsArgumentException()
    {
        var service = CreateService();

        var ex = await Assert.ThrowsAsync<ArgumentException>(() =>
            service.ListProgrammableObjectsAsync("test", "db", null, null, ["INVALID_TYPE"], CancellationToken.None));

        Assert.Contains("Unrecognized object type", ex.Message);
        Assert.Contains("INVALID_TYPE", ex.Message);
    }
}
