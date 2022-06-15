namespace ToolBX.AutoInject;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAutoInjectServices(this IServiceCollection serviceCollection)
    {
        var types = TypeFetcher.Query().HasAttribute(typeof(AutoInjectAttribute)).ToList();

        foreach (var type in types)
        {
            var interfaces = type.GetInterfaces();
            if (!interfaces.Any()) throw new InvalidOperationException(string.Format(Exceptions.CannotInjectServiceBecauseNoInterface, type.Name));

            var attribute = (AutoInjectAttribute)Attribute.GetCustomAttribute(type, typeof(AutoInjectAttribute), true);

            Type implementation;
            if (attribute.Interface != null)
                implementation = attribute.Interface;
            else if (interfaces.Length == 1)
                implementation = interfaces.Single();
            else if (interfaces.Any(x => x.Name.Equals($"I{type.Name}", StringComparison.InvariantCultureIgnoreCase)))
                implementation = interfaces.Single(x => x.Name.Equals($"I{type.Name}", StringComparison.InvariantCultureIgnoreCase));
            else
            {
                var regex = new Regex(@"(?<=[A-Z])(?=[A-Z][a-z]) | (?<=[^A-Z])(?=[A-Z]) | (?<=[A-Za-z])(?=[^A-Za-z])", RegexOptions.IgnorePatternWhitespace);

                var splittedTypeName = regex.Replace(type.Name, " ").Split(' ');
                var searchResult = new List<InterfaceSearchResult>();

                var directInterfaces = type.GetDirectInterfaces();

                foreach (var i in interfaces)
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

                if (!searchResult.Any()) throw new Exception(string.Format(Exceptions.CannotInjectServiceBecauseNoSimilarInterface, type.Name, interfaces.Length));

                searchResult = searchResult.OrderBy(x => x.IsInherited).ThenByDescending(x => x.Similarities).ToList();
                if (searchResult.Count > 1 && searchResult[0].Similarities == searchResult[1].Similarities && searchResult[0].IsInherited == searchResult[1].IsInherited)
                    throw new Exception(string.Format(Exceptions.CannotInjectServiceBecauseOfAmbiguousNames, searchResult[0].Interface.Name, searchResult[1].Interface.Name, type.Name));

                implementation = searchResult.First().Interface;
            }

            if (implementation.IsGenericType && !implementation.IsGenericTypeDefinition)
                implementation = implementation.GetGenericTypeDefinition();

            switch (attribute.Lifetime)
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
                    throw new NotSupportedException(string.Format(Exceptions.CannotInjectServiceBecauseLifetimeNotSupported, attribute.Lifetime));
            }


        }
        return serviceCollection;
    }

    public static IServiceCollection AddAutoConfig(this IServiceCollection services, IConfiguration configuration)
    {
        var types = TypeFetcher.Query().IsNotInterface().IsNotAbstract().IsNotGenericTypeDefinition().IsNotGeneric().HasAttribute(typeof(AutoConfigAttribute)).ToList();

        foreach (var type in types)
        {
            var attribute = (AutoConfigAttribute)Attribute.GetCustomAttribute(type, typeof(AutoConfigAttribute), true);
            typeof(ServiceCollectionExtensions).GetMethod(nameof(Configure), BindingFlags.Static | BindingFlags.NonPublic).MakeGenericMethod(type)
                .Invoke(null, BindingFlags.Static | BindingFlags.NonPublic, null, new object[] { services, configuration, attribute.Name }, null);
        }

        return services;
    }

    private static IServiceCollection Configure<T>(IServiceCollection services, IConfiguration configuration, string name) where T : class => services.Configure<T>(x => configuration.GetSection(name).Bind(x));

    public static IReadOnlyList<T> GetAutoInjectServices<T>(this IServiceProvider serviceProvider)
    {
        var types = TypeFetcher.Query().HasAttribute(typeof(AutoInjectAttribute)).Implements(typeof(T)).ToList()
            .Select(x => x.GetInterfaces().SingleOrDefault(y => y.Name != typeof(T).Name && serviceProvider.GetService(y) is T))
            .Where(x => x != null);

        return types.Select(x => (T)serviceProvider.GetService(x) ?? throw new AutoInjectServiceNotFoundException(x, typeof(T))).ToList();
    }
}