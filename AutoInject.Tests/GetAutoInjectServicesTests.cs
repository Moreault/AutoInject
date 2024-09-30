using Microsoft.Extensions.DependencyInjection;

namespace AutoInject.Tests;

[TestClass]
public class GetAutoInjectServicesTests : Tester
{
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

    [TestMethod]
    public void WhenServiceProviderIsNull_Throw()
    {
        //Arrange
        IServiceProvider serviceProvider = null!;

        //Act
        var action = () => serviceProvider.GetAutoInjectServices<IService1>();

        //Assert
        action.Should().Throw<ArgumentNullException>().WithParameterName(nameof(serviceProvider));
    }

    [TestMethod]
    public void WhenServiceProviderIsNotNull_ReturnAllServices()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddAutoInjectServices();

        var serviceProvider = services.BuildServiceProvider();

        // Act
        var result = serviceProvider.GetAutoInjectServices<IService1>();

        // Assert
        result.Should().HaveCount(3);
        result.Should().Contain(x => x.GetType() == typeof(Service1));
        result.Should().Contain(x => x.GetType() == typeof(Service2));
        result.Should().Contain(x => x.GetType() == typeof(Service3));
    }
}