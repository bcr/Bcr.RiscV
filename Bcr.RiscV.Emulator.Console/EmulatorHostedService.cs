using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Bcr.RiscV.Emulator.Console;

class EmulatorHostedService : IHostedService
{
    private ILogger<EmulatorHostedService> _logger;

    public EmulatorHostedService(ILogger<EmulatorHostedService> logger) => this._logger = logger;

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogCritical("You should implement this");
        throw new NotImplementedException();
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}