﻿namespace ToolBX.AutoInject;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Add all services annotated with <see cref="AutoInjectAttribute"/> or <see cref="AutoInjectAttribute{T}"/> from the specified assembly.
    /// </summary>
    public static IServiceCollection AddAutoInjectServices(this IServiceCollection serviceCollection, Assembly assembly, AutoInjectOptions? options = null)
    {
        if (serviceCollection == null) throw new ArgumentNullException(nameof(serviceCollection));
        if (assembly == null) throw new ArgumentNullException(nameof(assembly));
        var types = Types.From(assembly).Where(x => x.HasAttribute<AutoInjectAttribute>() || x.HasAttribute(typeof(AutoInjectAttribute<>)));
        return serviceCollection.AddAutoInjectServices(types, options);
    }

    /// <summary>
    /// Add all services annotated with <see cref="AutoInjectAttribute"/> or <see cref="AutoInjectAttribute{T}"/> from all assemblies.
    /// </summary>
    public static IServiceCollection AddAutoInjectServices(this IServiceCollection serviceCollection, AutoInjectOptions? options = null)
    {
        if (serviceCollection == null) throw new ArgumentNullException(nameof(serviceCollection));
        var types = Types.Where(x => x.HasAttribute<AutoInjectAttribute>() || x.HasAttribute(typeof(AutoInjectAttribute<>)));
        return serviceCollection.AddAutoInjectServices(types, options);
    }

    private static IServiceCollection AddAutoInjectServices(this IServiceCollection serviceCollection, IEnumerable<Type> types, AutoInjectOptions? options = null)
    {
        options ??= new AutoInjectOptions();
        foreach (var type in types)
        {
            var candidates = type.GetInterfaces().Concat(new[] { type.BaseType }).Where(x => x != null).ToArray();
            if (!candidates.Any()) throw new InvalidOperationException(string.Format(Exceptions.CannotInjectServiceBecauseNoInterface, type.Name));

            var attribute = (AutoInjectAttributeBase)Attribute.GetCustomAttribute(type, typeof(AutoInjectAttributeBase), true)!;

            Type implementation;
            if (attribute is AutoInjectAttribute nonGeneric && nonGeneric.Interface != null)
                implementation = nonGeneric.Interface;
            else if (attribute.GetType().GetGenericArguments().Any())
                implementation = attribute.GetType().GetGenericArguments().Single();
            else if (candidates.Length == 1)
                implementation = candidates.Single()!;
            else if (candidates.Count(x => x!.IsInterface) == 1)
                implementation = candidates.Single(x => x!.IsInterface)!;
            else if (candidates.Any(x => x!.IsInterface && x.Name.Equals($"I{type.Name}", StringComparison.InvariantCultureIgnoreCase)))
                implementation = candidates.Single(x => x!.Name.Equals($"I{type.Name}", StringComparison.InvariantCultureIgnoreCase))!;
            else
            {
                var regex = new Regex(@"(?<=[A-Z])(?=[A-Z][a-z]) | (?<=[^A-Z])(?=[A-Z]) | (?<=[A-Za-z])(?=[^A-Za-z])", RegexOptions.IgnorePatternWhitespace);

                var splittedTypeName = regex.Replace(type.Name, " ").Split(' ');
                var searchResult = new List<InterfaceSearchResult>();

                var directInterfaces = type.GetDirectInterfaces();

                foreach (var i in candidates.Where(x => x!.IsInterface))
                {
                    var splittedInterfaceName = regex.Replace(i.Name, " ").Split(' ');
                    var similarities = splittedInterfaceName.Sum(x => splittedTypeName.Count(y => x.Contains(y, StringComparison.InvariantCultureIgnoreCase)));

                    if (similarities > 0)
                        searchResult.Add(new InterfaceSearchResult
                        {
                            Interface = i,
                            Similarities = similarities,
                            IsInherited = !directInterfaces.Contains(i)
                        });
                }

                if (!searchResult.Any()) throw new Exception(string.Format(Exceptions.CannotInjectServiceBecauseNoSimilarInterface, type.Name, candidates.Length));

                searchResult = searchResult.OrderBy(x => x.IsInherited).ThenByDescending(x => x.Similarities).ToList();
                if (searchResult.Count > 1 && searchResult[0].Similarities == searchResult[1].Similarities && searchResult[0].IsInherited == searchResult[1].IsInherited)
                    throw new Exception(string.Format(Exceptions.CannotInjectServiceBecauseOfAmbiguousNames, searchResult[0].Interface.Name, searchResult[1].Interface.Name, type.Name));

                implementation = searchResult.First().Interface;
            }

            if (type.IsGenericType && !type.GenericTypeArguments.Any() && implementation.IsGenericType && !implementation.IsGenericTypeDefinition)
                implementation = implementation.GetGenericTypeDefinition();

            var lifetime = attribute.RealLifetime ?? options.DefaultLifetime;
            switch (lifetime)
            {
                case ServiceLifetime.Singleton:
                    serviceCollection.AddSingleton(implementation, type);
                    break;
                case ServiceLifetime.Scoped:
                    serviceCollection.AddScoped(implementation, type);
                    break;
                case ServiceLifetime.Transient:
                    serviceCollection.AddTransient(implementation, type);
                    break;
                default:
                    throw new NotSupportedException(string.Format(Exceptions.CannotInjectServiceBecauseLifetimeNotSupported, lifetime));
            }
        }
        return serviceCollection;
    }

    [Obsolete("Use the AutoConfig package from nuget.org instead. Will be removed in 3.0.0")]
    public static IServiceCollection AddAutoConfig(this IServiceCollection services, Assembly assembly, IConfiguration configuration)
    {
        if (services is null) throw new ArgumentNullException(nameof(services));
        if (assembly is null) throw new ArgumentNullException(nameof(assembly));
        if (configuration is null) throw new ArgumentNullException(nameof(configuration));

        var types = Types.From(assembly).Where(x => !x.IsInterface && !x.IsAbstract && !x.IsGenericTypeDefinition && !x.IsGenericType && x.HasAttribute<AutoConfigAttribute>());
        return services.AddAutoConfig(configuration, types);
    }

    [Obsolete("Use the AutoConfig package from nuget.org instead. Will be removed in 3.0.0")]
    public static IServiceCollection AddAutoConfig(this IServiceCollection services, IConfiguration configuration)
    {
        if (services is null) throw new ArgumentNullException(nameof(services));
        if (configuration is null) throw new ArgumentNullException(nameof(configuration));

        var types = Types.Where(x => !x.IsInterface && !x.IsAbstract && !x.IsGenericTypeDefinition && !x.IsGenericType && x.HasAttribute<AutoConfigAttribute>());
        return services.AddAutoConfig(configuration, types);
    }

    private static IServiceCollection AddAutoConfig(this IServiceCollection services, IConfiguration configuration, IEnumerable<Type> types)
    {
        foreach (var type in types)
        {
            var attribute = (AutoConfigAttribute)Attribute.GetCustomAttribute(type, typeof(AutoConfigAttribute), true)!;
            typeof(ServiceCollectionExtensions).GetMethod(nameof(Configure), BindingFlags.Static | BindingFlags.NonPublic)!.MakeGenericMethod(type)
                .Invoke(null, BindingFlags.Static | BindingFlags.NonPublic, null, new object[] { services, configuration, attribute.Name }, null);
        }

        return services;
    }

    private static IServiceCollection Configure<T>(IServiceCollection services, IConfiguration configuration, string name) where T : class => services.Configure<T>(x => configuration.GetSection(name).Bind(x));

    public static IReadOnlyList<T> GetAutoInjectServices<T>(this IServiceProvider serviceProvider)
    {
        var types = Types.Where(x => x.HasAttribute<AutoInjectAttribute>() && x.Implements<T>())
            .Select(x => x.GetInterfaces().SingleOrDefault(y => y.Name != typeof(T).Name && serviceProvider.GetService(y) is T))
            .Where(x => x != null);

        return types.Select(x => (T)serviceProvider.GetService(x!)! ?? throw new AutoInjectServiceNotFoundException(x!, typeof(T))).ToList();
    }


}