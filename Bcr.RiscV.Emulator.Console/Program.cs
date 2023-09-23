using ELFSharp.ELF;

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
        foreach(var header in elf.Sections)
        {
            System.Console.WriteLine(header);
        }
    }
}
