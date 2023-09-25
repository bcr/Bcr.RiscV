namespace Bcr.RiscV.Emulator.Console;

interface IMemory
{
    uint ReadInstruction(uint address);
}
