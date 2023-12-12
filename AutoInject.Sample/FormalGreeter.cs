namespace AutoInject.Sample;

public interface IFormalGreeter
{
    string Greet();
}

[AutoInject(ServiceLifetime.Scoped)]
public class FormalGreeter : IFormalGreeter
{
    public string Greet() => "Hello";
}