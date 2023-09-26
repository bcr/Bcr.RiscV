using ELFSharp.ELF;
using ELFSharp.ELF.Sections;

namespace Bcr.RiscV.Emulator.Console;

class ELFMemory : IMemory
{
    private Dictionary<uint, byte[]> _programChunks = new Dictionary<uint, byte[]>();

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

    public uint ReadInstruction(uint address)
    {
        return BitConverter.ToUInt32(LocateSpan(address, 4));
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
}
