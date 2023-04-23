namespace ToolBX.AutoInject;

internal record InterfaceSearchResult
{
    public required Type Interface { get; init; }
    public int Similarities { get; init; }
    public bool IsInherited { get; init; }
}