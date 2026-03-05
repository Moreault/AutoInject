namespace ToolBX.AutoInject;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAutoInjectServices(this IServiceCollection services, Assembly assembly, AutoInjectOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(assembly);

        options ??= new AutoInjectOptions();

        var registrarType = assembly.GetType("ToolBX.AutoInject.Generated.AutoInjectRegistrar");
        if (registrarType != null)
        {
            var method = registrarType.GetMethod("Register", BindingFlags.Public | BindingFlags.Static);
            method?.Invoke(null, [services, options.DefaultLifetime]);
        }

        return services;
    }

    public static IServiceCollection AddAutoInjectServices(this IServiceCollection services, AutoInjectOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        options ??= new AutoInjectOptions();

        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            if (assembly.GetCustomAttribute<HasAutoInjectServicesAttribute>() != null)
                services.AddAutoInjectServices(assembly, options);
        }

        return services;
    }
}
