namespace SqlAugur.Configuration;

internal sealed record ToolsetDefinition(
    string Name,
    string Description,
    Func<SqlAugurOptions, bool> IsConfigured,
    Type[] ToolTypes);
