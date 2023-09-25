using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Bcr.RiscV.Emulator.Console;

internal class Program
{
    private static int Main(string[] args)
    {
        string? filename = null;
        foreach (var arg in args)
        {
            if (!arg.StartsWith("-"))
            {
                // Filename
                filename = arg;
            }
            else
            {
                // Option of some sort
            }
        }

        var builder = Host.CreateDefaultBuilder(args).ConfigureServices(services => {
            services.AddSingleton<EmulatorService>();
            services.AddSingleton<IMemory>(new ELFMemory(filename!));
            services.AddTransient<ICsr, DefaultCsr>();
            services.AddTransient<IEcall, DefaultEcall>();
            services.AddTransient<IEmulator, Emulator>();
        });

        IHost host = builder.Build();

        var myClass = host.Services.GetRequiredService<EmulatorService>();
        var returnValue = myClass.Run();
        // host.Run();
        return returnValue;
    }
}
