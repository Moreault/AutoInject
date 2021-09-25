using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Reflection;

namespace ToolBX.AutoInject
{
    internal class AssemblyLoader
    {
        private static bool _areAssembliesLoaded;

        // Source: https://dotnetstories.com/blog/Dynamically-pre-load-assemblies-in-a-ASPNET-Core-or-any-C-project-en-7155735300
        public static void LoadAll(bool includeFramework = false)
        {
            if (_areAssembliesLoaded) return;

            var loaded = new ConcurrentDictionary<string, bool>();

            bool ShouldLoad(string assemblyName)
            {
                return (includeFramework || IsNotNetFramework(assemblyName)) && !loaded.ContainsKey(assemblyName);
            }

            bool IsNotNetFramework(string assemblyName)
            {
                return !assemblyName.StartsWith("Microsoft.")
                       && !assemblyName.StartsWith("System.")
                       && !assemblyName.StartsWith("Newtonsoft.")
                       && assemblyName != "netstandard";
            }

            void LoadReferencedAssembly(Assembly assembly)
            {
                // Check all referenced assemblies of the specified assembly
                foreach (var an in assembly.GetReferencedAssemblies().Where(a => ShouldLoad(a.FullName)))
                {
                    // Load the assembly and load its dependencies
                    LoadReferencedAssembly(Assembly.Load(an)); // AppDomain.CurrentDomain.Load(name)
                    loaded.TryAdd(an.FullName, true);
                }
            }

            foreach (var a in AppDomain.CurrentDomain.GetAssemblies().Where(a => ShouldLoad(a.FullName)))
            {
                loaded.TryAdd(a.FullName, true);
            }

            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies().Where(a => IsNotNetFramework(a.FullName)))
                LoadReferencedAssembly(assembly);

            _areAssembliesLoaded = true;
        }
    }
}