namespace SqlAugur.Services;

public interface IToolsetManager
{
    string GetToolsetSummaries();
    string GetToolsetDetails(string toolsetName);
    string EnableToolset(string toolsetName);
}
