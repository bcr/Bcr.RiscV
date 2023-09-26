namespace Bcr.RiscV.Emulator.Console;

interface IMemory
{
    byte ReadByte(uint address);
    uint ReadInstruction(uint address);
}
