namespace ToolBX.AutoInject;

internal class InterfaceSearchResult
{
    public Type Interface { get; init; }
    public int Similarities { get; init; }
    public bool IsInherited { get; init; }
}