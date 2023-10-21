namespace AutoInject.Sample;

public interface IComplexGreeter<T>
{
    string Greet();
}

[AutoInject]
public class ComplexGreeter : IComplexGreeter<ComplexGreeter>
{
    public string Greet() => "Thou art verily hello'd";
}