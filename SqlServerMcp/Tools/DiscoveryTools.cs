using System.ComponentModel;
using ModelContextProtocol.Server;
using SqlServerMcp.Services;

namespace SqlServerMcp.Tools;

[McpServerToolType]
public sealed class DiscoveryTools
{
    private readonly IToolsetManager _toolsetManager;

    public DiscoveryTools(IToolsetManager toolsetManager)
    {
        _toolsetManager = toolsetManager;
    }

    [McpServerTool(
        Name = "list_toolsets",
        Title = "List Toolsets",
        ReadOnly = true,
        Idempotent = true)]
    [Description("List available toolsets and their current status. Use this to discover what additional tools can be enabled for schema exploration, diagrams, and database performance analysis.")]
    public string ListToolsets()
    {
        return _toolsetManager.GetToolsetSummaries();
    }

    [McpServerTool(
        Name = "get_toolset_tools",
        Title = "Get Toolset Details",
        ReadOnly = true,
        Idempotent = true)]
    [Description("Get detailed information about a specific toolset's tools and parameters before enabling it.")]
    public string GetToolsetTools(
        [Description("Toolset name: 'schema_exploration', 'diagrams', 'first_responder_kit', 'darling_data', or 'whoisactive'")]
        string toolsetName)
    {
        return _toolsetManager.GetToolsetDetails(toolsetName);
    }

    [McpServerTool(
        Name = "enable_toolset",
        Title = "Enable Toolset",
        ReadOnly = true)]
    [Description("Enable a toolset, making its tools available for use. Some toolsets require server-side configuration.")]
    public string EnableToolset(
        [Description("Toolset name: 'schema_exploration', 'diagrams', 'first_responder_kit', 'darling_data', or 'whoisactive'")]
        string toolsetName)
    {
        return _toolsetManager.EnableToolset(toolsetName);
    }
}
