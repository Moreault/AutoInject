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
        Console.WriteLine("3. Complex");
        Console.WriteLine("4. Generic");
        Console.WriteLine("5. Open generic");
        Console.WriteLine("6. Weird");
        Console.WriteLine("7. Abstract");
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

            if (key.Key == ConsoleKey.D3)
            {
                Console.WriteLine(greeter.Greet(GreetingKind.Complex));
                break;
            }

            if (key.Key == ConsoleKey.D4)
            {
                Console.WriteLine(greeter.Greet(GreetingKind.Generic));
                break;
            }

            if (key.Key == ConsoleKey.D5)
            {
                Console.WriteLine(greeter.Greet(GreetingKind.Weird));
                break;
            }

            if (key.Key == ConsoleKey.D6)
            {
                Console.WriteLine(greeter.Greet(GreetingKind.Abstract));
                break;
            }
        }

        Console.WriteLine("Now get the hell of my lawn!");
    }
}