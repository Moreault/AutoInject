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
    private readonly IComplexGreeter<ComplexGreeter> _complexGreeter;
    private readonly IGenericGreeter<string> _genericGreeter;
    private readonly IOpenGenericGreeter<int> _openGenericGreeter;

    public Greeter(IFamiliarGreeter familiarGreeter, IFormalGreeter formalGreeter, IOpenGenericGreeter<int> openGenericGreeter , IComplexGreeter<ComplexGreeter> complexGreeter, IGenericGreeter<string> genericGreeter)
    {
        _familiarGreeter = familiarGreeter;
        _formalGreeter = formalGreeter;
        _openGenericGreeter = openGenericGreeter;
        _complexGreeter = complexGreeter;
        _genericGreeter = genericGreeter;
    }

    public string Greet(GreetingKind kind)
    {
        switch (kind)
        {
            case GreetingKind.Formal:
                return _formalGreeter.Greet();
            case GreetingKind.Familiar:
                return _familiarGreeter.Greet();
            case GreetingKind.Complex:
                return _complexGreeter.Greet();
            case GreetingKind.Generic:
                return _genericGreeter.Greet();
            case GreetingKind.OpenGeneric:
                return _openGenericGreeter.Greet().ToString();
            default:
                return "What?! Stick to the script!";
        }
    }
}