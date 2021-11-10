namespace ToolBX.AutoInject;

[AttributeUsage(AttributeTargets.Class)]
public class AutoConfigAttribute : Attribute
{
    public string Name { get; }

    public AutoConfigAttribute(string name)
    {
        Name = name;
    }
}