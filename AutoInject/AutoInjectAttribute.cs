﻿namespace ToolBX.AutoInject;

[AttributeUsage(AttributeTargets.Class)]
public sealed class AutoInjectAttribute : AutoInjectAttributeBase
{
    [Obsolete("Use AutoInject<T> instead. Will be removed in 3.0.0")]
    public Type? Interface { get; init; }
}

// ReSharper disable once UnusedTypeParameter : It is used via reflection and implementing types outside its assembly
public sealed class AutoInjectAttribute<T> : AutoInjectAttributeBase
{

}

public abstract class AutoInjectAttributeBase : Attribute
{
    public ServiceLifetime Lifetime { get; init; }

    internal AutoInjectAttributeBase() { }
}