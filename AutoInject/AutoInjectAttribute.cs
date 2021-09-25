using Microsoft.Extensions.DependencyInjection;
using System;

namespace ToolBX.AutoInject
{
    [AttributeUsage(AttributeTargets.Class)]
    public class AutoInjectAttribute : Attribute
    {
        public ServiceLifetime Lifetime { get; init; }

        public Type Interface { get; init; }
    }
}