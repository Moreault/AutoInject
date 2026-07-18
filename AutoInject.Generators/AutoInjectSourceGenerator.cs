using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace ToolBX.AutoInject.Generators;

[Generator]
public class AutoInjectSourceGenerator : IIncrementalGenerator
{
    private const string AutoInjectAttributeName = "ToolBX.AutoInject.AutoInjectAttribute";
    private const string AutoInjectGenericAttributeName = "ToolBX.AutoInject.AutoInjectAttribute<T>";

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var classDeclarations = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (node, _) => node is ClassDeclarationSyntax cds && cds.AttributeLists.Count > 0,
                transform: static (ctx, _) => GetAutoInjectInfo(ctx))
            .Where(static info => info is not null);

        var collected = classDeclarations.Collect();

        context.RegisterSourceOutput(collected, static (spc, infos) => Execute(spc, infos!));
    }

    private static AutoInjectInfo? GetAutoInjectInfo(GeneratorSyntaxContext context)
    {
        var classDeclaration = (ClassDeclarationSyntax)context.Node;
        var classSymbol = context.SemanticModel.GetDeclaredSymbol(classDeclaration);
        if (classSymbol is null || classSymbol.IsAbstract)
            return null;

        AttributeData? autoInjectAttribute = null;
        foreach (var attr in classSymbol.GetAttributes())
        {
            var attrClass = attr.AttributeClass;
            if (attrClass is null) continue;

            var fullName = attrClass.ConstructedFrom.ToDisplayString();
            if (fullName == AutoInjectAttributeName || fullName == AutoInjectGenericAttributeName)
            {
                autoInjectAttribute = attr;
                break;
            }
        }

        if (autoInjectAttribute is null)
            return null;

        return new AutoInjectInfo
        {
            ClassSymbol = classSymbol,
            Attribute = autoInjectAttribute,
            Location = classDeclaration.GetLocation()
        };
    }

    private static void Execute(SourceProductionContext context, ImmutableArray<AutoInjectInfo?> infos)
    {
        if (infos.IsDefaultOrEmpty)
            return;

        var registrations = new List<RegistrationEntry>();

        foreach (var info in infos)
        {
            if (info is null) continue;

            var classSymbol = info.ClassSymbol;
            var attribute = info.Attribute;
            var location = info.Location;

            INamedTypeSymbol? serviceType = null;

            if (attribute.AttributeClass!.IsGenericType &&
                attribute.AttributeClass.TypeArguments.Length == 1)
            {
                serviceType = attribute.AttributeClass.TypeArguments[0] as INamedTypeSymbol;
            }

            if (serviceType is null)
            {
                serviceType = ResolveServiceType(context, classSymbol, location);
                if (serviceType is null)
                    continue;
            }

            string? explicitLifetime = null;
            if (attribute.ConstructorArguments.Length > 0 &&
                attribute.ConstructorArguments[0].Value is int lifetimeValue)
            {
                explicitLifetime = lifetimeValue switch
                {
                    0 => "Microsoft.Extensions.DependencyInjection.ServiceLifetime.Singleton",
                    1 => "Microsoft.Extensions.DependencyInjection.ServiceLifetime.Scoped",
                    2 => "Microsoft.Extensions.DependencyInjection.ServiceLifetime.Transient",
                    _ => null
                };
            }

            if (explicitLifetime is null)
            {
                foreach (var namedArg in attribute.NamedArguments)
                {
                    if (namedArg.Key == "Lifetime" && namedArg.Value.Value is int namedLifetimeValue)
                    {
                        explicitLifetime = namedLifetimeValue switch
                        {
                            0 => "Microsoft.Extensions.DependencyInjection.ServiceLifetime.Singleton",
                            1 => "Microsoft.Extensions.DependencyInjection.ServiceLifetime.Scoped",
                            2 => "Microsoft.Extensions.DependencyInjection.ServiceLifetime.Transient",
                            _ => null
                        };
                    }
                }
            }

            var serviceTypeStr = GetFullyQualifiedTypeName(serviceType);
            var implTypeStr = GetFullyQualifiedTypeName(classSymbol);

            if (classSymbol.IsGenericType && classSymbol.TypeParameters.Length > 0 &&
                serviceType.IsGenericType && !IsConstructedGenericType(serviceType))
            {
                // Both are open generics, use as-is
            }
            else if (classSymbol.IsGenericType && classSymbol.TypeParameters.Length > 0)
            {
                // Implementation is open generic - get the generic type definition of the service
                if (serviceType.IsGenericType)
                {
                    serviceTypeStr = GetFullyQualifiedTypeName(serviceType.ConstructedFrom);
                }
            }

            registrations.Add(new RegistrationEntry
            {
                ServiceType = serviceTypeStr,
                ImplementationType = implTypeStr,
                ExplicitLifetime = explicitLifetime
            });
        }

        if (registrations.Count == 0)
            return;

        var sb = new StringBuilder();
        sb.AppendLine("// <auto-generated/>");
        sb.AppendLine("#pragma warning disable CS1591");
        sb.AppendLine();
        sb.AppendLine("[assembly: ToolBX.AutoInject.HasAutoInjectServices]");
        sb.AppendLine();
        sb.AppendLine("namespace ToolBX.AutoInject.Generated");
        sb.AppendLine("{");
        sb.AppendLine("    internal static class AutoInjectRegistrar");
        sb.AppendLine("    {");
        sb.AppendLine("        [System.Runtime.CompilerServices.ModuleInitializer]");
        sb.AppendLine("        internal static void Initialize()");
        sb.AppendLine("        {");
        sb.AppendLine("            global::ToolBX.AutoInject.AutoInjectRegistry.Register(typeof(AutoInjectRegistrar).Assembly, Register);");
        sb.AppendLine("        }");
        sb.AppendLine();
        sb.AppendLine("        public static void Register(");
        sb.AppendLine("            Microsoft.Extensions.DependencyInjection.IServiceCollection services,");
        sb.AppendLine("            Microsoft.Extensions.DependencyInjection.ServiceLifetime defaultLifetime)");
        sb.AppendLine("        {");

        foreach (var reg in registrations)
        {
            if (reg.ExplicitLifetime is not null)
            {
                sb.AppendLine($"            services.Add(new Microsoft.Extensions.DependencyInjection.ServiceDescriptor(typeof({reg.ServiceType}), typeof({reg.ImplementationType}), {reg.ExplicitLifetime}));");
            }
            else
            {
                sb.AppendLine($"            services.Add(new Microsoft.Extensions.DependencyInjection.ServiceDescriptor(typeof({reg.ServiceType}), typeof({reg.ImplementationType}), defaultLifetime));");
            }
        }

        sb.AppendLine("        }");
        sb.AppendLine("    }");
        sb.AppendLine("}");

        context.AddSource("AutoInjectRegistrar.g.cs", sb.ToString());
    }

    private static INamedTypeSymbol? ResolveServiceType(SourceProductionContext context, INamedTypeSymbol classSymbol, Location location)
    {
        var candidates = new List<INamedTypeSymbol>();

        foreach (var iface in classSymbol.AllInterfaces)
            candidates.Add(iface);

        if (classSymbol.BaseType is not null &&
            classSymbol.BaseType.SpecialType != SpecialType.System_Object)
            candidates.Add(classSymbol.BaseType);

        if (candidates.Count == 0)
        {
            context.ReportDiagnostic(Diagnostic.Create(
                new DiagnosticDescriptor(
                    "AUTOINJ001",
                    "No service type found",
                    "Cannot inject '{0}' automatically: it must implement at least one interface or have a base class",
                    "AutoInject",
                    DiagnosticSeverity.Error,
                    true),
                location,
                classSymbol.Name));
            return null;
        }

        if (candidates.Count == 1)
            return candidates[0];

        // Single interface (excluding base types of base)
        var interfaces = candidates.Where(c => c.TypeKind == TypeKind.Interface).ToList();
        if (interfaces.Count == 1)
            return interfaces[0];

        // I{ClassName} convention
        var className = classSymbol.Name;
        var conventionMatch = candidates.FirstOrDefault(c =>
            c.TypeKind == TypeKind.Interface &&
            string.Equals(c.Name, $"I{className}", StringComparison.OrdinalIgnoreCase));
        if (conventionMatch is not null)
            return conventionMatch;

        // Word-similarity matching
        var directInterfaces = classSymbol.Interfaces;
        var splitPattern = new Regex(@"(?<=[A-Z])(?=[A-Z][a-z]) | (?<=[^A-Z])(?=[A-Z]) | (?<=[A-Za-z])(?=[^A-Za-z])", RegexOptions.IgnorePatternWhitespace);
        var classWords = splitPattern.Replace(className, " ").Split(' ');

        var searchResults = new List<(INamedTypeSymbol Symbol, int Similarities, bool IsInherited)>();

        foreach (var candidate in candidates.Where(c => c.TypeKind == TypeKind.Interface))
        {
            var interfaceWords = splitPattern.Replace(candidate.Name, " ").Split(' ');
            var similarities = interfaceWords.Sum(iw => classWords.Count(cw =>
                iw.IndexOf(cw, StringComparison.OrdinalIgnoreCase) >= 0));

            if (similarities > 0)
            {
                var isInherited = !directInterfaces.Contains(candidate, SymbolEqualityComparer.Default);
                searchResults.Add((candidate, similarities, isInherited));
            }
        }

        if (searchResults.Count == 0)
        {
            context.ReportDiagnostic(Diagnostic.Create(
                new DiagnosticDescriptor(
                    "AUTOINJ002",
                    "No similar interface found",
                    "Cannot inject '{0}' automatically: it implements {1} interfaces but none have a similar name",
                    "AutoInject",
                    DiagnosticSeverity.Error,
                    true),
                location,
                classSymbol.Name,
                interfaces.Count));
            return null;
        }

        searchResults = searchResults
            .OrderBy(x => x.IsInherited)
            .ThenByDescending(x => x.Similarities)
            .ToList();

        if (searchResults.Count > 1 &&
            searchResults[0].Similarities == searchResults[1].Similarities &&
            searchResults[0].IsInherited == searchResults[1].IsInherited)
        {
            context.ReportDiagnostic(Diagnostic.Create(
                new DiagnosticDescriptor(
                    "AUTOINJ003",
                    "Ambiguous interface",
                    "Cannot inject '{0}' automatically: ambiguity between '{1}' and '{2}'. Specify the interface using AutoInject<T>",
                    "AutoInject",
                    DiagnosticSeverity.Error,
                    true),
                location,
                classSymbol.Name,
                searchResults[0].Symbol.Name,
                searchResults[1].Symbol.Name));
            return null;
        }

        return searchResults[0].Symbol;
    }

    private static string GetFullyQualifiedTypeName(INamedTypeSymbol symbol)
    {
        if (symbol.IsGenericType && symbol.TypeArguments.Length > 0)
        {
            if (symbol.TypeArguments.All(t => t is ITypeParameterSymbol))
            {
                // Open generic: Namespace.Type<,>
                var ns = symbol.ContainingNamespace.IsGlobalNamespace
                    ? ""
                    : symbol.ContainingNamespace.ToDisplayString() + ".";

                var containingTypes = GetContainingTypePrefix(symbol);

                var arity = symbol.TypeArguments.Length;
                var arityStr = arity > 1
                    ? "<" + new string(',', arity - 1) + ">"
                    : "<>";
                return $"global::{ns}{containingTypes}{symbol.Name}{arityStr}";
            }
            else
            {
                // Closed generic: Namespace.Type<Arg1, Arg2>
                var ns = symbol.ContainingNamespace.IsGlobalNamespace
                    ? ""
                    : symbol.ContainingNamespace.ToDisplayString() + ".";

                var containingTypes = GetContainingTypePrefix(symbol);

                var typeArgs = string.Join(", ", symbol.TypeArguments.Select(t =>
                {
                    if (t is INamedTypeSymbol namedType)
                        return GetFullyQualifiedTypeName(namedType);
                    return "global::" + t.ToDisplayString();
                }));
                return $"global::{ns}{containingTypes}{symbol.Name}<{typeArgs}>";
            }
        }

        var prefix = symbol.ContainingNamespace.IsGlobalNamespace
            ? ""
            : symbol.ContainingNamespace.ToDisplayString() + ".";

        var containingPrefix = GetContainingTypePrefix(symbol);

        return $"global::{prefix}{containingPrefix}{symbol.Name}";
    }

    private static string GetContainingTypePrefix(INamedTypeSymbol symbol)
    {
        var containingTypes = new List<string>();
        var containing = symbol.ContainingType;
        while (containing is not null)
        {
            containingTypes.Insert(0, containing.Name);
            containing = containing.ContainingType;
        }
        return containingTypes.Count > 0
            ? string.Join(".", containingTypes) + "."
            : "";
    }

    private static bool IsConstructedGenericType(INamedTypeSymbol symbol)
    {
        return symbol.IsGenericType && symbol.TypeArguments.All(t => t is ITypeParameterSymbol);
    }

    private class AutoInjectInfo
    {
        public INamedTypeSymbol ClassSymbol { get; set; } = null!;
        public AttributeData Attribute { get; set; } = null!;
        public Location Location { get; set; } = null!;
    }

    private class RegistrationEntry
    {
        public string ServiceType { get; set; } = "";
        public string ImplementationType { get; set; } = "";
        public string? ExplicitLifetime { get; set; }
    }
}
