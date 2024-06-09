namespace Bcr.RiscV.Emulator.Console;

interface IMemory
{
    byte ReadByte(uint address);
    ushort ReadHalfword(uint address);
    uint ReadInstruction(uint address, out int instructionLength);
    uint ReadWord(uint address);
    void WriteByte(uint address, byte value);
    void WriteHalfword(uint address, ushort value);
    void WriteWord(uint address, uint value);
}
