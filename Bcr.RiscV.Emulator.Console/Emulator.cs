using Microsoft.Extensions.Logging;

namespace Bcr.RiscV.Emulator.Console;

class Emulator : IEmulator
{
    const uint startAddress = 0x8000_0000;

    private ILogger<Emulator> _logger;
    private IMemory _memory;

    public Emulator(ILogger<Emulator> logger, IMemory memory)
    {
        _logger = logger;
        _memory = memory;
    }

    public void Run()
    {
        var PC = startAddress;
        var registers = new uint[32];
        _logger.LogInformation("Emulator starting");
        while (true)
        {
            // Read next instruction
            var instruction = _memory.ReadInstruction(PC);
            _logger.LogInformation("Instruction is {instruction:X8}", instruction);
            var opcode = instruction & 0b111_1111;
            var rd = (instruction & 0b1111_1000_0000) >> 7;
            var rs1 = (instruction & 0b1111_1000_0000_0000_0000) >> 15;
            var funct3 = (instruction & 0b111_0000_0000_0000) >> 12;
            bool pcNeedsAdjusting = true;
            switch (opcode)
            {
                case 0b110_1111:
                    // JAL
                    _logger.LogInformation("{rd}", rd);
                    registers[rd] = PC + 4;
                    // Compute immediate
                    var offset = UJComputeImmediate(instruction);
                    // Add immediate to PC
                    PC = (offset < 0) ? (PC - (uint) (-offset)) : (PC + (uint) offset);
                    pcNeedsAdjusting = false;
                    break;
                case 0b001_0011:
                    // ALU
                    switch (funct3)
                    {
                        case 0b000:
                            // ADDI
                            var immediate = IComputeImmediate(instruction);
                            registers[rd] = (immediate < 0) ? (registers[rs1] - (uint) (-immediate)) : (registers[rs1] + (uint) immediate);
                            break;
                        default:
                            throw new NotImplementedException();
                    }
                    break;
                case 0b111_0011:
                    // CSR
                    switch (funct3)
                    {
                        case 0b010:
                            // CSRRS
                            // !!! TODO: Implement something smarter
                            registers[rd] = 0;
                            break;
                        default:
                            throw new NotImplementedException();
                    }
                    break;
                default:
                    throw new NotImplementedException();
            }
            // Let people write to x0 but then fix it
            registers[0] = 0;
            // Execute instruction
            // Adjust PC if required
            if (pcNeedsAdjusting)
            {
                PC += 4;
            }
        }
    }

    private int SignExtend(int value, int bitPosition)
    {
        var finalValue = value;

        if ((value & (1 << bitPosition)) != 0)
        {
            var oneBits = 0xFFFF_FFFF << (bitPosition + 1);
            finalValue |= (int) oneBits;
        }
        return finalValue;
    }

    private int UJComputeImmediate(uint instruction)
    {
        // imm[20|10:1|11|19:12]
        Range[] ranges = {
            new System.Range(20, 20),
            new System.Range(10, 1),
            new System.Range(11, 11),
            new System.Range(19, 12),
        };
        int offset = 31;
        int finalValue = 0;

        foreach (var range in ranges)
        {
            var rangeLength = range.Start.Value - range.End.Value + 1;

            var mask = (1 << rangeLength) - 1;
            mask <<= offset - (rangeLength - 1);
            var value = instruction & mask;
            value >>= (offset - range.Start.Value);
            finalValue |= (int) value;

            offset -= rangeLength;
        }

        finalValue = SignExtend(finalValue, 20);
        return finalValue;
    }

    private int IComputeImmediate(uint instruction)
    {
        Range[] ranges = {
            new System.Range(11, 0),
        };
        int offset = 31;
        int finalValue = 0;

        foreach (var range in ranges)
        {
            var rangeLength = range.Start.Value - range.End.Value + 1;

            var mask = (1 << rangeLength) - 1;
            mask <<= offset - (rangeLength - 1);
            var value = instruction & mask;
            value >>= (offset - range.Start.Value);
            finalValue |= (int) value;

            offset -= rangeLength;
        }

        finalValue = SignExtend(finalValue, 11);
        return finalValue;
    }
}
