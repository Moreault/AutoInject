namespace ToolBX.AutoInject;

[Obsolete("Use the AutoConfig package from nuget.org instead. Will be removed in 3.0.0")]
[AttributeUsage(AttributeTargets.Class)]
    public class AutoConfigAttribute : Attribute
{
    public string Name { get; }

    public AutoConfigAttribute(string name)
    {
        Name = name;
    }
}