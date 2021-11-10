namespace ToolBX.AutoInject;

public static class TypeExtensions
{
    public static IList<Type> GetDirectInterfaces(this Type type)
    {
        if (type == null) throw new ArgumentNullException(nameof(type));

        var allInterfaces = new List<Type>();
        var childInterfaces = new List<Type>();

        foreach (var i in type.GetInterfaces())
        {
            allInterfaces.Add(i);
            childInterfaces.AddRange(i.GetInterfaces());
        }

        var baseTypeInterfaces = type.BaseType?.GetInterfaces() ?? Array.Empty<Type>();
        childInterfaces.AddRange(baseTypeInterfaces);
        return allInterfaces.Except(childInterfaces).ToList();
    }
}