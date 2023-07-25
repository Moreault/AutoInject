![autoinject](https://user-images.githubusercontent.com/5889860/134765804-86714a82-be40-4434-8aa9-d570a4735ae6.png)

# AutoInject
A lightweight .NET library designed to make it easier for you to inject services without having to add a new line to a configuration class every time you create an injectable service.

```cs
public void ConfigureServices(IServiceCollection services)
{
    services.AddTransient<ISomeService, SomeService>();
    services.AddScoped<ISomeOtherService, SomeOtherService>();
    services.AddSingleton<IAnotherService, AnotherService>();
    services.AddSingleton<IYetAnotherService, YetAnotherService>();
    
    //TODO Remember to add new services here manually like some sort of animal
}
```

What year is this? 2008? What if you have dozens or hundreds of services to inject? With [AutoInject] you can instead do it like this. 

```cs
public void ConfigureServices(IServiceCollection services)
{
    services.AddAutoInjectServices();
}

public interface ISomeService { }

[AutoInject(Lifetime = ServiceLifetime.Transient)]
public class SomeService : ISomeService { }

public interface ISomeOtherService { }

[AutoInject(Lifetime = ServiceLifetime.Scoped)]
public class SomeOtherService : ISomeOtherService { }

public interface IAnotherService { }

//It uses Singleton by default so no need to specify it
[AutoInject]
public class AnotherService : IAnotherService { }

public interface IYetAnotherService { }

//But knock yourself out if that's what you're into
[AutoInject(Lifetime = ServiceLifetime.Singleton)]
public class YetAnotherService : IYetAnotherService { }
```

As of 2.2.0, AutoInject supports injection via a base class rather than an interface. You have to use the generic AutoInject<T> attribute so that it knows what class to inject itself as. There is an example covering this use case in the sample project.

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

The generic `AutoInject<T>` should be used whenever there is ambiguity between two or more types. Here is how AutoInject will otherwise resolves your types for injection : 

1. If the class has only one implementation or base class, that implementation or base class is used
2. From here on, base types will be ignored and only interfaces will be considered
3. If the class has multiple implementations, it will first look for `"IMyName"`
4. If it does not implement an `"IMyName"` interface, it will look for an interface with a similar name (It's not very smart or reliable and I would avoid defaulting to this as much as possible!)
5. Throws an exception since it can't possibly guess which interface or base type to use

Using `AutoInject<T>` will bypass automatic resolution entirely. I don't necessarily recommend using `AutoInject<T>` for every use case but it's quite all right if you want to always be absolutely certain. I personally only use it as a last resort and default to regular `AutoInject`.

## Getting started

Placing `[AutoInject]` attributes on every class in your project by itself will do very little (nothing) if you don't _configure_ it properly. You must add the following line to your startup code in order for AutoInject to work :

```cs
public void ConfigureServices(IServiceCollection services)
{
    services.AddAutoInjectServices();
}
```

This will also add AutoInject support for every other loaded assembly so you only need to call it once and everything that uses the `[AutoInject]` attribute _everywhere_ will be injected.

## Core of ToolBX
`[AutoInject]` is used by every ToolBX library that requires DI and it may not have to be manually added to your project if you already use one such library. It ensures that all ToolBX types are always injected no matter what. I do encourage you to hop on the train and use it as well but it's ultimately your decision which the framework respects by not tying you down in any way.

`AddAutoInjectServices` is never called by a ToolBX library so you always have to do that one step yourself. 