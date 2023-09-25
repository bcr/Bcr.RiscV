using Microsoft.Extensions.Logging;

namespace Bcr.RiscV.Emulator.Console;

class EmulatorService
{
    private ILogger<EmulatorService> _logger;
    private IEmulator _emulator;

    public EmulatorService(ILogger<EmulatorService> logger, IEmulator emulator)
    {
        this._logger = logger;
        this._emulator = emulator;
    }

    public void Run()
    {
        _emulator.Run();
    }
}
