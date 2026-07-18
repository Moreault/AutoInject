namespace ToolBX.AutoInject;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAutoInjectServices(this IServiceCollection services, Assembly assembly, AutoInjectOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(assembly);

        options ??= new AutoInjectOptions();

        // The generated AutoInjectRegistrar self-registers into AutoInjectRegistry from a module
        // initializer, so we can look its services up without reflection (trimming/AOT safe).
        foreach (var register in AutoInjectRegistry.For(assembly))
            register(services, options.DefaultLifetime);

        return services;
    }

    public static IServiceCollection AddAutoInjectServices(this IServiceCollection services, AutoInjectOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        options ??= new AutoInjectOptions();

        foreach (var register in AutoInjectRegistry.All())
            register(services, options.DefaultLifetime);

        return services;
    }
}
