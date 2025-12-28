using Avalonia;
using System;

namespace CattosTracker;

class Program
{
    [STAThread]
    public static void Main(string[] args)
    {

        try
        {
            Console.WriteLine("[Program] Starting CattosTracker...");
            BuildAvaloniaApp()
                .StartWithClassicDesktopLifetime(args);
            Console.WriteLine("[Program] Application exited normally");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[FATAL ERROR] Application crashed!");
            Console.WriteLine($"Exception: {ex.GetType().Name}");
            Console.WriteLine($"Message: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
            Console.WriteLine("\nPress any key to exit...");
            Console.ReadKey();
            throw;
        }
    }

    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
}
