using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Bcr.RiscV.Emulator.Console;

class EmulatorHost : IHostedService
{
    private ILogger<EmulatorHost> logger;

    public EmulatorHost(ILogger<EmulatorHost> logger) => this.logger = logger;

    public Task StartAsync(CancellationToken cancellationToken)
    {
        this.logger.LogCritical("You should implement this");
        throw new NotImplementedException();
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}