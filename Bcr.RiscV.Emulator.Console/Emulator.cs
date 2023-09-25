using Microsoft.Extensions.Logging;

namespace Bcr.RiscV.Emulator.Console;

class Emulator : IEmulator
{
    const uint startAddress = 0x8000_0000;

    private ILogger<Emulator> _logger;
    private IMemory _memory;

    public Emulator(ILogger<Emulator> logger, IMemory memory)
    {
        _logger = logger;
        _memory = memory;
    }

    public void Run()
    {
        var IP = startAddress;
        _logger.LogInformation("Emulator starting");
        // Read next instruction
        var instruction = _memory.ReadInstruction(IP);
        // Execute instruction
        // Adjust PC if required
    }
}
