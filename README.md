![autoinject](https://user-images.githubusercontent.com/5889860/134765804-86714a82-be40-4434-8aa9-d570a4735ae6.png)

# AutoInject
A lightweight .NET library designed to make it easier for you to inject services without having to add a new line to a configuration class every time you create an injectable service.

```cs
var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddTransient<ISomeService, SomeService>();
builder.Services.AddScoped<ISomeOtherService, SomeOtherService>();
builder.Services.AddSingleton<IAnotherService, AnotherService>();
builder.Services.AddSingleton<IYetAnotherService, YetAnotherService>();

//TODO Remember to add new services here manually like some sort of animal
```

What year is this? 2008? What if you have dozens or hundreds of services to inject? With [AutoInject] you can instead do it like this.

```cs
var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddAutoInjectServices();

public interface ISomeService { }

[AutoInject(ServiceLifetime.Transient)]
public class SomeService : ISomeService { }

public interface ISomeOtherService { }

[AutoInject(ServiceLifetime.Scoped)]
public class SomeOtherService : ISomeOtherService { }

public interface IAnotherService { }

//It uses Singleton by default so no need to specify it
[AutoInject]
public class AnotherService : IAnotherService { }

public interface IYetAnotherService { }

//But knock yourself out if that's what you're into
[AutoInject(ServiceLifetime.Singleton)]
public class YetAnotherService : IYetAnotherService { }
```

AutoInject also supports injection via a base class rather than an interface. You have to use the generic `AutoInject<T>` attribute so that it knows what class to inject itself as.

```cs
public abstract class AbstractGreeter
{
   public abstract string Greet();
}

[AutoInject<AbstractGreeter>]
public class ConcreteGreeter : AbstractGreeter, IWeirdGreeter
{
    public override string Greet() => "Hello, theoretically";
}
```

## Getting started

Placing `[AutoInject]` attributes on every class in your project by itself will do very little (nothing) if you don't _configure_ it properly. You must add the following line to your startup code in order for AutoInject to work :

```cs
var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddAutoInjectServices();
```

This will also add AutoInject support for every other loaded assembly so you only need to call it once and everything that uses the `[AutoInject]` attribute _everywhere_ will be injected.

You can also register services from a specific assembly :

```cs
builder.Services.AddAutoInjectServices(typeof(SomeType).Assembly);
```

## How it works

As of version 4.0.0, AutoInject uses a **Roslyn source generator** to produce DI registration code at compile time. This means:

- No runtime reflection to scan assemblies for attributed types
- Faster application startup
- Compile-time errors when service types can't be resolved (instead of runtime exceptions)

The source generator runs during compilation and emits a `Register` method containing all the `ServiceDescriptor` registrations for your `[AutoInject]`-attributed classes.

### Project setup

Any project that contains `[AutoInject]`-attributed classes needs a reference to both `AutoInject` and its source generator. When consuming AutoInject as a NuGet package, the generator is included automatically. When using project references, you need to reference the generator project as well:

```xml
<ProjectReference Include="..\AutoInject\AutoInject.csproj" />
<ProjectReference Include="..\AutoInject.Generators\AutoInject.Generators.csproj"
                  OutputItemType="Analyzer"
                  ReferenceOutputAssembly="false" />
```

## Overriding default lifetime
By default, that is if you don't specify anything and just use `[AutoInject]`, your services will be injected as `Singleton`. You can override this behavior by using `AutoInjectOptions` when adding AutoInject support in your startup code.

```cs
builder.Services.AddAutoInjectServices(new AutoInjectOptions { DefaultLifetime = ServiceLifetime.Scoped });
```

## Automatic type resolution vs explicit

The generic `AutoInject<T>` should be used whenever there is ambiguity between two or more types. Here is how AutoInject will otherwise resolve your types for injection :

1. If the class has only one implementation or base class, that implementation or base class is used
2. From here on, base types will be ignored and only interfaces will be considered
3. If the class has multiple implementations, it will first look for `"IMyName"`
4. If it does not implement an `"IMyName"` interface, it will look for an interface with a similar name (It's not very smart or reliable and I would avoid defaulting to this as much as possible!)
5. Emits a compile-time error since it can't possibly guess which interface or base type to use

Using `AutoInject<T>` will bypass automatic resolution entirely. I don't necessarily recommend using `AutoInject<T>` for every use case but it's quite all right if you want to always be absolutely certain. I personally only use it as a last resort and default to regular `AutoInject`.

## Breaking changes in 4.0.0

- **Source generator replaces runtime reflection.** The `ToolBX.Reflection4Humans.TypeFetcher` dependency has been removed entirely. Service type resolution now happens at compile time.
- **`GetAutoInjectServices<T>()` has been removed.** Use `IEnumerable<T>` from the DI container instead (e.g. `serviceProvider.GetServices<T>()`).
- **Runtime exceptions are now compile-time diagnostics.** If a service type can't be resolved (no interface, ambiguous names, etc.), you'll get a compiler error instead of a runtime exception.
- **`AutoInjectServiceNotFoundException` has been removed** along with all resource files (`Exceptions.resx`, etc.).
- **`Microsoft.Extensions.Configuration.Json`, `Microsoft.Extensions.Configuration.Binder`, and `Microsoft.Extensions.Options` package dependencies have been removed.**

## Core of the ToolBX micro framework
`[AutoInject]` is used by every ToolBX library that requires DI and it may not have to be manually added to your project if you already use one such library. It ensures that all ToolBX types are always injected no matter what. I do encourage you to hop on the train and use it as well but it's ultimately your decision which the framework respects by not tying you down in any way.

`AddAutoInjectServices` is never called by a ToolBX library so you always have to do that one step yourself.
