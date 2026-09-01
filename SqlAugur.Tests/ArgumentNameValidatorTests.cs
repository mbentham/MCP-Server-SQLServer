using System.Text.Json;
using SqlAugur.Services;

namespace SqlAugur.Tests;

public class ArgumentNameValidatorTests
{
    // Mirrors the schema the SDK generates for read_data.
    private static readonly JsonElement ReadDataSchema = JsonDocument.Parse("""
        {
            "type": "object",
            "properties": {
                "serverName": { "type": "string" },
                "query": { "type": "string" },
                "databaseName": { "type": "string" }
            },
            "required": ["serverName", "query", "databaseName"]
        }
        """).RootElement;

    // ───────────────────────────────────────────────
    // Valid calls — should return null (no error)
    // ───────────────────────────────────────────────

    [Fact]
    public void CorrectNames_IsValid()
    {
        Assert.Null(ArgumentNameValidator.Validate(
            ReadDataSchema, ["serverName", "query", "databaseName"]));
    }

    [Fact]
    public void OptionalParameterOmitted_IsValid()
    {
        var schema = JsonDocument.Parse("""
            {
                "type": "object",
                "properties": {
                    "serverName": { "type": "string" },
                    "schemaName": { "type": "string" }
                },
                "required": ["serverName"]
            }
            """).RootElement;

        Assert.Null(ArgumentNameValidator.Validate(schema, ["serverName"]));
    }

    [Fact]
    public void NoParametersToolWithNoArguments_IsValid()
    {
        var schema = JsonDocument.Parse("""{ "type": "object" }""").RootElement;

        Assert.Null(ArgumentNameValidator.Validate(schema, null));
        Assert.Null(ArgumentNameValidator.Validate(schema, []));
    }

    [Fact]
    public void SchemaWithoutProperties_SkipsValidation()
    {
        var schema = JsonDocument.Parse("""{ "type": "object" }""").RootElement;

        Assert.Null(ArgumentNameValidator.Validate(schema, ["anything"]));
    }

    // ───────────────────────────────────────────────
    // Wrong names — should return a corrective message
    // ───────────────────────────────────────────────

    [Fact]
    public void ShortenedNames_SuggestsFullNames()
    {
        var error = ArgumentNameValidator.Validate(
            ReadDataSchema, ["server", "query", "database"]);

        Assert.NotNull(error);
        Assert.Contains("Unknown argument 'server' - did you mean 'serverName'?", error);
        Assert.Contains("Unknown argument 'database' - did you mean 'databaseName'?", error);
        Assert.Contains("Expected arguments: serverName, query, databaseName.", error);
    }

    [Fact]
    public void CaseSlip_SuggestsCorrectCasing()
    {
        var error = ArgumentNameValidator.Validate(
            ReadDataSchema, ["ServerName", "query", "databaseName"]);

        Assert.NotNull(error);
        Assert.Contains("did you mean 'serverName'?", error);
    }

    [Fact]
    public void UnrecognisableName_ListsExpectedArguments()
    {
        var error = ArgumentNameValidator.Validate(
            ReadDataSchema, ["db", "query", "serverName"]);

        Assert.NotNull(error);
        Assert.Contains("Unknown argument 'db'.", error);
        Assert.Contains("Expected arguments: serverName, query, databaseName.", error);
    }

    [Fact]
    public void MissingRequiredArgument_IsNamed()
    {
        var error = ArgumentNameValidator.Validate(ReadDataSchema, ["serverName", "query"]);

        Assert.NotNull(error);
        Assert.Contains("Missing required argument: 'databaseName'.", error);
    }

    [Fact]
    public void MultipleMissingRequiredArguments_ArePluralised()
    {
        var error = ArgumentNameValidator.Validate(ReadDataSchema, ["query"]);

        Assert.NotNull(error);
        Assert.Contains("Missing required arguments: 'serverName', 'databaseName'.", error);
    }

    [Fact]
    public void NoArgumentsForRequiredSchema_ReportsAllMissing()
    {
        var error = ArgumentNameValidator.Validate(ReadDataSchema, null);

        Assert.NotNull(error);
        Assert.Contains("Missing required arguments: 'serverName', 'query', 'databaseName'.", error);
    }
}
