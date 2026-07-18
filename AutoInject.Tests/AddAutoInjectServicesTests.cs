using System.Collections.Concurrent;

namespace AutoInject.Tests;

public interface IConcurrencyMarker;

public class ConcurrencyMarker : IConcurrencyMarker;

public interface IService1;

[AutoInject]
public class Service1 : IService1;

[AutoInject<IService1>]
public class Service2 : IService1;

public interface IService3 : IService1;

[AutoInject]
public class Service3 : IService3;

public interface IServiceA;

[AutoInject]
public class ServiceA : IServiceA;

public interface IScopedService;

[AutoInject(ServiceLifetime.Scoped)]
public class ScopedService : IScopedService;

public interface ITransientService;

[AutoInject(ServiceLifetime.Transient)]
public class TransientService : ITransientService;

[TestClass]
public class AddAutoInjectServicesTests
{
    [TestMethod]
    public void WhenServicesIsNull_Throw()
    {
        // Arrange
        IServiceCollection services = null!;

        // Act
        var action = () => services.AddAutoInjectServices();

        // Assert
        action.Should().Throw<ArgumentNullException>();
    }

    [TestMethod]
    public void WhenAssemblyOverload_ServicesIsNull_Throw()
    {
        // Arrange
        IServiceCollection services = null!;

        // Act
        var action = () => services.AddAutoInjectServices(typeof(AddAutoInjectServicesTests).Assembly);

        // Assert
        action.Should().Throw<ArgumentNullException>();
    }

    [TestMethod]
    public void WhenAssemblyOverload_AssemblyIsNull_Throw()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var action = () => services.AddAutoInjectServices((System.Reflection.Assembly)null!);

        // Assert
        action.Should().Throw<ArgumentNullException>();
    }

    [TestMethod]
    public void WhenUsingAutoInjectWithoutExplicitType_RegistersWithMatchingInterface()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddAutoInjectServices(typeof(AddAutoInjectServicesTests).Assembly);
        var provider = services.BuildServiceProvider();

        // Assert
        var allService1 = provider.GetServices<IService1>().ToList();
        allService1.Should().Contain(x => x.GetType() == typeof(Service1));
    }

    [TestMethod]
    public void WhenUsingAutoInjectGeneric_RegistersWithSpecifiedType()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddAutoInjectServices(typeof(AddAutoInjectServicesTests).Assembly);
        var provider = services.BuildServiceProvider();

        // Assert
        var allService1 = provider.GetServices<IService1>().ToList();
        allService1.Should().Contain(x => x.GetType() == typeof(Service2));
    }

    [TestMethod]
    public void WhenUsingAutoInjectWithScopedLifetime_RegistersAsScoped()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddAutoInjectServices(typeof(AddAutoInjectServicesTests).Assembly);

        // Assert
        var descriptor = services.First(d => d.ImplementationType == typeof(ScopedService));
        descriptor.Lifetime.Should().Be(ServiceLifetime.Scoped);
    }

    [TestMethod]
    public void WhenUsingAutoInjectWithTransientLifetime_RegistersAsTransient()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddAutoInjectServices(typeof(AddAutoInjectServicesTests).Assembly);

        // Assert
        var descriptor = services.First(d => d.ImplementationType == typeof(TransientService));
        descriptor.Lifetime.Should().Be(ServiceLifetime.Transient);
    }

    [TestMethod]
    public void WhenNoExplicitLifetime_UsesDefaultFromOptions()
    {
        // Arrange
        var services = new ServiceCollection();
        var options = new AutoInjectOptions { DefaultLifetime = ServiceLifetime.Transient };

        // Act
        services.AddAutoInjectServices(typeof(AddAutoInjectServicesTests).Assembly, options);

        // Assert
        var descriptor = services.First(d => d.ImplementationType == typeof(Service1));
        descriptor.Lifetime.Should().Be(ServiceLifetime.Transient);
    }

    [TestMethod]
    public void WhenDefaultOptionsUsed_DefaultLifetimeIsSingleton()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddAutoInjectServices(typeof(AddAutoInjectServicesTests).Assembly);

        // Assert
        var descriptor = services.First(d => d.ImplementationType == typeof(Service1));
        descriptor.Lifetime.Should().Be(ServiceLifetime.Singleton);
    }

    [TestMethod]
    public void WhenInheritedInterface_RegistersCorrectly()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddAutoInjectServices(typeof(AddAutoInjectServicesTests).Assembly);
        var provider = services.BuildServiceProvider();

        // Assert
        provider.GetService<IService3>().Should().BeOfType<Service3>();
    }

    [TestMethod]
    public void WhenMultipleAutoInjectServices_AllAreRegistered()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddAutoInjectServices(typeof(AddAutoInjectServicesTests).Assembly);

        // Assert
        var registeredTypes = services.Select(d => d.ImplementationType).ToList();
        registeredTypes.Should().Contain(typeof(Service1));
        registeredTypes.Should().Contain(typeof(Service2));
        registeredTypes.Should().Contain(typeof(Service3));
        registeredTypes.Should().Contain(typeof(ServiceA));
        registeredTypes.Should().Contain(typeof(ScopedService));
        registeredTypes.Should().Contain(typeof(TransientService));
    }

    [TestMethod]
    public void WhenRegisteringConcurrentlyWhileReading_LosesNoRegistrationsAndNeverThrows()
    {
        // Arrange
        // Register is called from generated [ModuleInitializer] code, which the runtime can run
        // concurrently on multiple threads. This reproduces that: writers hammer Register while
        // readers enumerate the registry via the public no-arg overload (which reads through All()).
        const int writerCount = 1000;
        const int readerCount = 200;
        var assembly = typeof(AddAutoInjectServicesTests).Assembly;
        var exceptions = new ConcurrentQueue<Exception>();

        // A gate so every task starts at the same instant, maximizing contention.
        using var gate = new ManualResetEventSlim(false);

        var writers = Enumerable.Range(0, writerCount).Select(_ => Task.Run(() =>
        {
            gate.Wait();
            try
            {
                AutoInjectRegistry.Register(assembly, (services, _) =>
                    services.AddSingleton<IConcurrencyMarker, ConcurrencyMarker>());
            }
            catch (Exception e)
            {
                exceptions.Enqueue(e);
            }
        }));

        var readers = Enumerable.Range(0, readerCount).Select(_ => Task.Run(() =>
        {
            gate.Wait();
            try
            {
                new ServiceCollection().AddAutoInjectServices();
            }
            catch (Exception e)
            {
                exceptions.Enqueue(e);
            }
        }));

        var all = writers.Concat(readers).ToArray();

        // Act
        gate.Set();
        Task.WaitAll(all);

        // Assert
        exceptions.Should().BeEmpty();

        // Every writer's callback must have survived: running All() once should invoke each of the
        // registered marker callbacks exactly once, so we see exactly writerCount marker descriptors.
        var finalServices = new ServiceCollection();
        finalServices.AddAutoInjectServices();
        finalServices.Count(d => d.ServiceType == typeof(IConcurrencyMarker)).Should().Be(writerCount);
    }
}
