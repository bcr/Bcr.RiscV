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
            System.Console.WriteLine(section);
        }
    }

    public uint ReadInstruction(uint address)
    {
        foreach (var startAddress in _programChunks.Keys)
        {
            var instructionLength = 4;
            if ((address >= startAddress) &&
                ((address + (instructionLength - 1)) <= (startAddress + _programChunks[startAddress].Length)))
            {
                var offset = address - startAddress;
                return BitConverter.ToUInt32(_programChunks[startAddress], (int) offset);
            }
        }
        throw new NotImplementedException();
    }
}
