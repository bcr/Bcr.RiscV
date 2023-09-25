using ELFSharp.ELF;
using ELFSharp.ELF.Sections;

namespace Bcr.RiscV.Emulator.Console;

class ELFMemory : IMemory
{
    public ELFMemory(string filename)
    {
        // _logger.LogInformation("Loading {filename}", filename);
        var elf = ELFReader.Load(filename);
        var start = ((ISymbolTable)elf.GetSection(".symtab")).Entries.Where(x => x.Name == "_start").First();
        var startAddress = ((ProgBitsSection<UInt32>)start.PointedSection).LoadAddress;
        // _logger.LogInformation("_start = {startAddress:X8}", startAddress);
    }

    public uint ReadInstruction(uint address)
    {
        throw new NotImplementedException();
    }
}
