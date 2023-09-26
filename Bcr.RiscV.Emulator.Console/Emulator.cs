using Microsoft.Extensions.Logging;

namespace Bcr.RiscV.Emulator.Console;

class Emulator : IEmulator
{
    const uint startAddress = 0x8000_0000;

    private ILogger<Emulator> _logger;
    private IMemory _memory;
    private ICsr _csr;
    private IEcall _ecall;

    public Emulator(ILogger<Emulator> logger, IMemory memory, ICsr csr, IEcall ecall)
    {
        _logger = logger;
        _memory = memory;
        _csr = csr;
        _ecall = ecall;
    }

    public int Run()
    {
        var PC = startAddress;
        var registers = new uint[32];
        int returnValue = 0;
        bool running = true;
        _logger.LogInformation("Emulator starting");
        while (running)
        {
            // Read next instruction
            var instruction = _memory.ReadInstruction(PC);
            var opcode = instruction & 0b111_1111;
            var rd = (instruction & 0b1111_1000_0000) >> 7;
            var rs1 = (instruction & 0b1111_1000_0000_0000_0000) >> 15;
            var rs2 = (instruction & 0b1_1111_0000_0000_0000_0000_0000)>> 20;
            var funct3 = (instruction & 0b111_0000_0000_0000) >> 12;
            var funct12 = (instruction & 0b1111_1111_1111_0000_0000_0000_0000_0000) >> 20;
            var shamt = rs2;
            var csr = (instruction & 0b1111_1111_1111_0000_0000_0000_0000_0000) >> 20;
            var csrimm = rs1;
            bool pcNeedsAdjusting = true;
            int immediate = 0;
            switch (opcode)
            {
                case 0b110_1111:
                    // JAL
                    registers[rd] = PC + 4;
                    // Compute immediate
                    immediate = UJComputeImmediate(instruction);
                    // Add immediate to PC
                    PC += (uint) immediate;
                    pcNeedsAdjusting = false;
                    break;
                case 0b001_0011:
                    // ALU
                    switch (funct3)
                    {
                        case 0b000:
                            // ADDI
                            immediate = IComputeImmediate(instruction);
                            registers[rd] = registers[rs1] + (uint) immediate;
                            break;
                        case 0b001:
                            // SLLI
                            registers[rd] = registers[rs1] << (int) shamt;
                            break;
                        case 0b110:
                            // ORI
                            immediate = IComputeImmediate(instruction);
                            registers[rd] = registers[rs1] | (uint) immediate;
                            break;
                        default:
                            throw new NotImplementedException();
                    }
                    break;
                case 0b111_0011:
                    // SYSTEM
                    switch (funct3)
                    {
                        case 0b000:
                            // xRET
                            switch (funct12)
                            {
                                case 0b0000_0000_0000:
                                    // ECALL
                                    running = !(_ecall.HandleEcall(registers, out returnValue));
                                    break;
                                case 0b0011_0000_0010:
                                    // MRET
                                    PC = _csr.Read(CsrRegisters.mepc);
                                    pcNeedsAdjusting = false;
                                    break;
                                default:
                                    throw new NotImplementedException();
                            }
                            break;
                        case 0b001:
                            // CSRRW
                            registers[rd] = _csr.ReadWrite(csr, registers[rs1]);
                            break;
                        case 0b010:
                            // CSRRS
                            registers[rd] = _csr.ReadSet(csr, registers[rs1]);
                            break;
                        case 0b101:
                            // CSRRWI
                            registers[rd] = _csr.ReadWrite(csr, csrimm);
                            break;
                        default:
                            throw new NotImplementedException();
                    }
                    break;
                case 0b110_0011:
                    var conditionMet = false;
                    // Branch
                    switch (funct3)
                    {
                        case 0b000:
                            // BEQ
                            conditionMet = registers[rs1] == registers[rs2];
                            break;
                        case 0b001:
                            // BNE
                            conditionMet = registers[rs1] != registers[rs2];
                            break;
                        case 0b100:
                            // BLT
                            conditionMet = (int) registers[rs1] < (int) registers[rs2];
                            break;
                        default:
                            throw new NotImplementedException();
                    }
                    if (conditionMet)
                    {
                        immediate = SBComputeImmediate(instruction);
                        PC += (uint) immediate;
                        pcNeedsAdjusting = false;
                    }
                    break;
                case 0b001_0111:
                    // AUIPC
                    immediate = UComputeImmediate(instruction);
                    registers[rd] = PC + (uint) immediate;
                    break;
                case 0b011_0111:
                    // LUI
                    immediate = UComputeImmediate(instruction);
                    registers[rd] = (uint) immediate;
                    break;
                case 0b000_1111:
                    // FENCE
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
        return returnValue;
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
            if (offset >= range.Start.Value)
            {
                value >>= (offset - range.Start.Value);
            }
            else
            {
                value <<= (range.Start.Value - offset);
            }
            finalValue |= (int) value;

            offset -= rangeLength;
        }

        return finalValue;
    }

    private int UJComputeImmediate(uint instruction)
    {
        // imm[20|10:1|11|19:12]
        Range[] ranges = {
            new(20, 20),
            new(10, 1),
            new(11, 11),
            new(19, 12),
        };
        return SignExtend(ComputeImmediate(instruction, ranges, 31, 0), ranges[0].Start.Value);
    }

    private int IComputeImmediate(uint instruction)
    {
        Range[] ranges = {
            new(11, 0),
        };
        return SignExtend(ComputeImmediate(instruction, ranges, 31, 0), ranges[0].Start.Value);
    }

    private int SBComputeImmediate(uint instruction)
    {
        // imm[20|10:1|11|19:12]
        Range[] ranges = {
            new(12, 12),
            new(10, 5),
        };
        var returnValue = ComputeImmediate(instruction, ranges, 31, 0);
        Range[] ranges2 = {
            new(4, 1),
            new(11, 11),
        };
        return SignExtend(ComputeImmediate(instruction, ranges2, 11, returnValue), 12);
    }

    private int UComputeImmediate(uint instruction)
    {
        Range[] ranges = {
            new(31, 12),
        };
        return ComputeImmediate(instruction, ranges, 31, 0);
    }
}
