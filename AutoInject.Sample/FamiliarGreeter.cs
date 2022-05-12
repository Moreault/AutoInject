namespace AutoInject.Sample;

public interface IFamiliarGreeter
{
    string Greet();
}

[AutoInject]
public class FamiliarGreeter : IFamiliarGreeter
{
    public string Greet() => "Sup!";
}