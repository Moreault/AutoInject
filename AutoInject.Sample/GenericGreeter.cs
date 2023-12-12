namespace AutoInject.Sample;

public interface IGenericGreeter<out T>
{
    T Greet();
}

[AutoInject(Lifetime = ServiceLifetime.Scoped)]
public class GenericGreeter : IGenericGreeter<string>
{
    public string Greet() => "Hi!";
}