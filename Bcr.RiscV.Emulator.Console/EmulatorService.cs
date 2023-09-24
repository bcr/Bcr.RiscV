using Microsoft.Extensions.Logging;

namespace Bcr.RiscV.Emulator.Console;

class EmulatorService
{
    private ILogger<EmulatorService> _logger;

    public EmulatorService(ILogger<EmulatorService> logger) => this._logger = logger;

    public void Run()
    {
        _logger.LogCritical("You should implement this");
        // throw new NotImplementedException();
    }
}
