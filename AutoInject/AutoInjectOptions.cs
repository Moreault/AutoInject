namespace ToolBX.AutoInject;

public sealed record AutoInjectOptions
{
    public ServiceLifetime DefaultLifetime { get; init; } = ServiceLifetime.Singleton;
}