using ELFSharp.ELF;
using ELFSharp.ELF.Sections;

namespace Bcr.RiscV.Emulator.Console;

internal class Program
{
    private static void Main(string[] args)
    {
        string? filename = null;
        foreach (var arg in args)
        {
            if (!arg.StartsWith("-"))
            {
                // Filename
                filename = arg;
            }
            else
            {
                // Option of some sort
            }
        }
        // Load ELF file
        System.Console.WriteLine($"Loading {filename}");
        var elf = ELFReader.Load(filename);
        var start = ((ISymbolTable)elf.GetSection(".symtab")).Entries.Where(x => x.Name == "_start").First();
        var startAddress = ((ProgBitsSection<UInt32>)start.PointedSection).LoadAddress;
        System.Console.WriteLine($"_start = {startAddress:X8}");
    }
}
