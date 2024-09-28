using System;

namespace ToolBX.AutoInject;

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
            var implementation = GetImplementation(type);
            var attribute = (AutoInjectAttributeBase)Attribute.GetCustomAttribute(type, typeof(AutoInjectAttributeBase), true)!;

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

    private static Type GetImplementation(Type type)
    {
        var candidates = type.GetInterfaces().Concat([type.BaseType]).Where(x => x != null).ToArray();
        if (!candidates.Any()) throw new InvalidOperationException(string.Format(Exceptions.CannotInjectServiceBecauseNoInterface, type.Name));

        var attribute = (AutoInjectAttributeBase)Attribute.GetCustomAttribute(type, typeof(AutoInjectAttributeBase), true)!;

        Type implementation;
        if (attribute.GetType().GetGenericArguments().Any())
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

        return implementation;
    }

    private static IServiceCollection Configure<T>(IServiceCollection services, IConfiguration configuration, string name) where T : class => services.Configure<T>(x => configuration.GetSection(name).Bind(x));

    public static IReadOnlyList<T> GetAutoInjectServices<T>(this IServiceProvider serviceProvider)
    {
        if (serviceProvider is null) throw new ArgumentNullException(nameof(serviceProvider));
        var types = Types.Where(x => x.HasAttribute<AutoInjectAttributeBase>() && x.Implements<T>())
            .Select(GetImplementation)
            .Where(x => x != null!)
            .ToList();

        return types.SelectMany(x => serviceProvider.GetServices(x).OfType<T>()! ?? throw new AutoInjectServiceNotFoundException(x, typeof(T))).Distinct().ToList();
    }
}