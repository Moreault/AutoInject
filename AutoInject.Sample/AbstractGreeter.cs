namespace AutoInject.Sample;

public abstract class AbstractGreeter
{
   public abstract string Greet();
}

[AutoInject<AbstractGreeter>]
public class ConcreteGreeter : AbstractGreeter, IWeirdGreeter
{
    public override string Greet() => "Hello, theoretically";
}