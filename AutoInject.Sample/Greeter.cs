namespace AutoInject.Sample;

public interface IGreeter
{
    string Greet(GreetingKind kind);
}

[AutoInject]
public class Greeter : IGreeter
{
    private readonly IFamiliarGreeter _familiarGreeter;
    private readonly IFormalGreeter _formalGreeter;
    private readonly IWeirdGreeter _weirdGreeter;
    private readonly AbstractGreeter _abstractGreeter;

    public Greeter(IFamiliarGreeter familiarGreeter, IFormalGreeter formalGreeter, IWeirdGreeter weirdGreeter, AbstractGreeter abstractGreeter)
    {
        _familiarGreeter = familiarGreeter;
        _formalGreeter = formalGreeter;
        _weirdGreeter = weirdGreeter;
        _abstractGreeter = abstractGreeter;
    }

    public string Greet(GreetingKind kind)
    {
        switch (kind)
        {
            case GreetingKind.Formal:
                return _formalGreeter.Greet();
            case GreetingKind.Familiar:
                return _familiarGreeter.Greet();
            case GreetingKind.Weird:
                return _weirdGreeter.Greet();
            case GreetingKind.Abstract:
                return _abstractGreeter.Greet();
            default:
                return "What?! Stick to the script!";
        }
    }
}