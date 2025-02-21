﻿namespace ToolBX.AutoInject;

[AttributeUsage(AttributeTargets.Class)]
public sealed class AutoInjectAttribute : AutoInjectAttributeBase
{
    public AutoInjectAttribute() { }
    public AutoInjectAttribute(ServiceLifetime lifetime) : base(lifetime) { }
}

// ReSharper disable once UnusedTypeParameter : It is used via reflection and implementing types outside its assembly
public sealed class AutoInjectAttribute<T> : AutoInjectAttributeBase
{
    public AutoInjectAttribute() { }
    public AutoInjectAttribute(ServiceLifetime lifetime) : base(lifetime) { }
}

public abstract class AutoInjectAttributeBase : Attribute
{
    public ServiceLifetime Lifetime
    {
        get => RealLifetime ?? (ServiceLifetime)(-1);
        init => RealLifetime = value;
    }
    internal ServiceLifetime? RealLifetime;

    internal AutoInjectAttributeBase() { }

    internal AutoInjectAttributeBase(ServiceLifetime lifetime)
    {
        Lifetime = lifetime;
    }
}