namespace ToolBX.AutoInject;

/// <summary>
/// Collects the source-generated registration callbacks produced for each assembly that declares
/// <c>[AutoInject]</c> services. The generated <c>AutoInjectRegistrar</c> self-registers here from a
/// <see cref="System.Runtime.CompilerServices.ModuleInitializerAttribute"/>, which keeps service
/// discovery reflection-free and therefore trimming and Native AOT safe.
/// </summary>
public static class AutoInjectRegistry
{
    private static readonly List<Registration> Registrations = new();

    private static readonly object Lock = new();

    /// <summary>
    /// Called by generated code to register an assembly's <c>[AutoInject]</c> services. Not intended to be
    /// called directly.
    /// </summary>
    public static void Register(Assembly assembly, Action<IServiceCollection, ServiceLifetime> register)
    {
        ArgumentNullException.ThrowIfNull(assembly);
        ArgumentNullException.ThrowIfNull(register);
        lock (Lock)
            Registrations.Add(new Registration(assembly, register));
    }

    internal static IReadOnlyList<Action<IServiceCollection, ServiceLifetime>> For(Assembly assembly)
    {
        lock (Lock)
        {
            var result = new List<Action<IServiceCollection, ServiceLifetime>>();
            foreach (var registration in Registrations)
                if (Equals(registration.Assembly, assembly))
                    result.Add(registration.Register);
            return result;
        }
    }

    internal static IReadOnlyList<Action<IServiceCollection, ServiceLifetime>> All()
    {
        lock (Lock)
            return Registrations.ConvertAll(x => x.Register);
    }

    private sealed record Registration(Assembly Assembly, Action<IServiceCollection, ServiceLifetime> Register);
}
