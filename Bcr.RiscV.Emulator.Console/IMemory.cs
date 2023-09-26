namespace Bcr.RiscV.Emulator.Console;

interface IMemory
{
    byte ReadByte(uint address);
    ushort ReadHalfword(uint address);
    uint ReadInstruction(uint address);
    uint ReadWord(uint address);
}
