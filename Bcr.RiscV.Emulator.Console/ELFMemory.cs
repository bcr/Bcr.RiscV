using ELFSharp.ELF;
using ELFSharp.ELF.Sections;

namespace Bcr.RiscV.Emulator.Console;

class ELFMemory : IMemory
{
    private readonly Dictionary<uint, byte[]> _programChunks = new Dictionary<uint, byte[]>();

    public ELFMemory(string filename)
    {
        var elf = ELFReader.Load(filename);
        var sectionsToLoad = elf.GetSections<ProgBitsSection<uint>>();
        foreach (var section in sectionsToLoad)
        {
            _programChunks[section.LoadAddress] = section.GetContents();
        }
    }

    private Span<byte> LocateSpan(uint address, int length)
    {
        foreach (var startAddress in _programChunks.Keys)
        {
            if ((address >= startAddress) &&
                ((address + (length - 1)) <= (startAddress + _programChunks[startAddress].Length)))
            {
                return new Span<byte>(_programChunks[startAddress], (int) (address - startAddress), length);
            }
        }
        throw new NotImplementedException();
    }

    public byte ReadByte(uint address)
    {
        return LocateSpan(address, 1)[0];
    }

    // All the 32-bit instructions in the base ISA have their lowest two bits
    // set to "11". The optional compressed 16-bit instruction-set extensions
    // have their lowest two bits equal to 00, 01, or 10.
    private static bool IsCompressedInstruction(byte firstInstructionByte) => (firstInstructionByte & 0b11) != 0b11;

    public uint ReadInstruction(uint address, out int instructionLength)
    {
        if (IsCompressedInstruction(ReadByte(address)))
        {
            var instruction = ReadHalfword(address);
            instructionLength = 2;
            return instruction;
        }
        else
        {
            var instruction = ReadWord(address);
            instructionLength = 4;
            return instruction;
        }
    }

    public ushort ReadHalfword(uint address)
    {
        return BitConverter.ToUInt16(LocateSpan(address, 2));
    }

    public uint ReadWord(uint address)
    {
        return BitConverter.ToUInt32(LocateSpan(address, 4));
    }

    public void WriteByte(uint address, byte value)
    {
        LocateSpan(address, 1)[0] = value;
    }

    public void WriteHalfword(uint address, ushort value)
    {
        BitConverter.GetBytes(value).CopyTo(LocateSpan(address, 2));
    }

    public void WriteWord(uint address, uint value)
    {
        BitConverter.GetBytes(value).CopyTo(LocateSpan(address, 4));
    }
}
