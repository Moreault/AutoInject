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

    public Greeter(IFamiliarGreeter familiarGreeter, IFormalGreeter formalGreeter)
    {
        _familiarGreeter = familiarGreeter;
        _formalGreeter = formalGreeter;
    }

    public string Greet(GreetingKind kind)
    {
        switch (kind)
        {
            case GreetingKind.Formal:
                return _formalGreeter.Greet();
            case GreetingKind.Familiar:
                return _familiarGreeter.Greet();
            default:
                return "What?! Stick to the script!";
        }
    }
}