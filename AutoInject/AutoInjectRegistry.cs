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

    /// <summary>
    /// Called by generated code to register an assembly's <c>[AutoInject]</c> services. Not intended to be
    /// called directly.
    /// </summary>
    public static void Register(Assembly assembly, Action<IServiceCollection, ServiceLifetime> register)
    {
        ArgumentNullException.ThrowIfNull(assembly);
        ArgumentNullException.ThrowIfNull(register);
        Registrations.Add(new Registration(assembly, register));
    }

    internal static IEnumerable<Action<IServiceCollection, ServiceLifetime>> For(Assembly assembly)
    {
        foreach (var registration in Registrations)
            if (Equals(registration.Assembly, assembly))
                yield return registration.Register;
    }

    internal static IEnumerable<Action<IServiceCollection, ServiceLifetime>> All()
    {
        foreach (var registration in Registrations)
            yield return registration.Register;
    }

    private sealed record Registration(Assembly Assembly, Action<IServiceCollection, ServiceLifetime> Register);
}
