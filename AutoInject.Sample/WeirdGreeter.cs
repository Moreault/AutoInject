namespace AutoInject.Sample;

public interface IWeirdGreeter
{
    string Greet();
}

//We need to provide the interface's type to AutoInject since it has a wildly different name from its interface and implements multiple
[AutoInject<IWeirdGreeter>]
public class Greta : IWeirdGreeter, IFormalGreeter
{
    public string Greet() => "Siap!";

    string IFormalGreeter.Greet() => Greet();
}