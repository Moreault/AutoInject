namespace AutoInject.Sample;

public interface IGenericGreeter<out T>
{
    T Greet();
}

[AutoInject]
public class GenericGreeter : IGenericGreeter<string>
{
    public string Greet() => "Hi!";
}