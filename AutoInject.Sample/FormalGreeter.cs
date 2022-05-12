namespace AutoInject.Sample;

public interface IFormalGreeter
{
    string Greet();
}

[AutoInject]
public class FormalGreeter : IFormalGreeter
{
    public string Greet() => "Hello";
}