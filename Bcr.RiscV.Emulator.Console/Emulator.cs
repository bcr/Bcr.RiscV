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
            var rs2 = (instruction & 0b1_1111_0000_0000_0000_0000_0000)>> 20;
            var funct3 = (instruction & 0b111_0000_0000_0000) >> 12;
            bool pcNeedsAdjusting = true;
            int immediate = 0;
            switch (opcode)
            {
                case 0b110_1111:
                    // JAL
                    _logger.LogInformation("{rd}", rd);
                    registers[rd] = PC + 4;
                    // Compute immediate
                    immediate = UJComputeImmediate(instruction);
                    // Add immediate to PC
                    PC = (immediate < 0) ? (PC - (uint) (-immediate)) : (PC + (uint) immediate);
                    pcNeedsAdjusting = false;
                    break;
                case 0b001_0011:
                    // ALU
                    switch (funct3)
                    {
                        case 0b000:
                            // ADDI
                            immediate = IComputeImmediate(instruction);
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
                case 0b110_0011:
                    // Branch
                    switch (funct3)
                    {
                        case 0b001:
                            // BNE
                            if (registers[rs1] != registers[rs2])
                            {
                                immediate = SBComputeImmediate(instruction);
                                PC = (immediate < 0) ? (PC - (uint) (-immediate)) : (PC + (uint) immediate);
                                pcNeedsAdjusting = false;
                            }
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

    private int ComputeImmediate(uint instruction, Range[] ranges, int offset, int initialValue)
    {
        int finalValue = initialValue;

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
        return SignExtend(ComputeImmediate(instruction, ranges, 31, 0), ranges[0].Start.Value);
    }

    private int IComputeImmediate(uint instruction)
    {
        Range[] ranges = {
            new System.Range(11, 0),
        };
        return SignExtend(ComputeImmediate(instruction, ranges, 31, 0), ranges[0].Start.Value);
    }

    private int SBComputeImmediate(uint instruction)
    {
        // imm[20|10:1|11|19:12]
        Range[] ranges = {
            new System.Range(12, 12),
            new System.Range(10, 5),
        };
        var returnValue = ComputeImmediate(instruction, ranges, 31, 0);
        Range[] ranges2 = {
            new System.Range(4, 1),
            new System.Range(11, 11),
        };
        return SignExtend(ComputeImmediate(instruction, ranges2, 11, returnValue), 12);
    }
}
