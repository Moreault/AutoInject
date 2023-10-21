namespace AutoInject.Sample;

public interface IOpenGenericGreeter<out T>
{
    T? Greet();
}

[AutoInject(Interface = typeof(IOpenGenericGreeter<int>))]
public class OpenGenericGreeter<T> : IOpenGenericGreeter<T>
{
    public T? Greet() => default;
}