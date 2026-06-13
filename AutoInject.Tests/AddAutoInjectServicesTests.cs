namespace AutoInject.Tests;

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
}
