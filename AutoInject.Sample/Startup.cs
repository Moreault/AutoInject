using Microsoft.Extensions.DependencyInjection;

namespace AutoInject.Sample;

public class Startup : ConsoleStartup
{
    public Startup(IConfiguration configuration) : base(configuration)
    {
    }

    public override void Run(IServiceProvider serviceProvider)
    {
        var greeter = serviceProvider.GetRequiredService<IGreeter>();

        Console.WriteLine("What kind of greeting do you want?");
        Console.WriteLine("1. Formal");
        Console.WriteLine("2. Familiar");
        while (true)
        {
            var key = Console.ReadKey(true);
            if (key.Key == ConsoleKey.D1)
            {
                Console.WriteLine(greeter.Greet(GreetingKind.Formal));
                break;
            }

            if (key.Key == ConsoleKey.D2)
            {
                Console.WriteLine(greeter.Greet(GreetingKind.Familiar));
                break;
            }
        }

        Console.WriteLine("Now get the hell of my lawn!");
    }
}