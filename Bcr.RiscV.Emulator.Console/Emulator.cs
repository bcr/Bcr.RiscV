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
            var funct7 = (instruction & 0b1111_1110_0000_0000_0000_0000_0000_0000) >> 25;
            var funct12 = (instruction & 0b1111_1111_1111_0000_0000_0000_0000_0000) >> 20;
            var shamt = rs2;
            var csr = (instruction & 0b1111_1111_1111_0000_0000_0000_0000_0000) >> 20;
            var csrimm = rs1;
            bool pcNeedsAdjusting = true;
            int immediate = 0;
            switch (opcode)
            {
                case 0b000_0011:
                    immediate = IComputeImmediate(instruction);
                    uint readAddress = (uint) (registers[rs1] + immediate);
                    registers[rd] = funct3 switch
                    {
                        0b000 => (uint)SignExtend(_memory.ReadByte(readAddress), 7), // LB
                        _ => throw new IllegalInstructionException(PC, instruction),
                    };
                    break;
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
                    immediate = IComputeImmediate(instruction);
                    registers[rd] = funct3 switch
                    {
                        0b000 => registers[rs1] + (uint)immediate, // ADDI
                        0b001 => registers[rs1] << (int)shamt, // SLLI
                        0b010 => (uint)(((int)registers[rs1] < immediate) ? 1 : 0), // SLTI
                        0b011 => (uint)((registers[rs1] < (uint)immediate) ? 1 : 0), // SLTIU
                        0b100 => registers[rs1] ^ (uint)immediate, // XORI
                        0b101 => funct7 switch
                        {
                            0b0000000 => registers[rs1] >> (int)shamt, // SRLI
                            0b0100000 => (uint)SignExtend((int)(registers[rs1] >> (int)shamt), (int)(31 - shamt)), // SRAI
                            _ => throw new IllegalInstructionException(PC, instruction),
                        },
                        0b110 => registers[rs1] | (uint)immediate, // ORI
                        0b111 => registers[rs1] & (uint)immediate, // ANDI
                        _ => throw new IllegalInstructionException(PC, instruction),
                    };
                    break;
                case 0b011_0011:
                    registers[rd] = funct3 switch
                    {
                        0b000 => funct7 switch
                        {
                            0b0000000 => registers[rs1] + registers[rs2], // ADD
                            0b0100000 => registers[rs1] - registers[rs2], // SUB
                            _ => throw new IllegalInstructionException(PC, instruction),
                        },
                        0b001 => registers[rs1] << (int) registers[rs2], // SLL
                        0b010 => (uint) (((int) registers[rs1] < (int) registers[rs2]) ? 1 : 0), // SLT
                        0b011 => (uint) ((registers[rs1] < registers[rs2]) ? 1 : 0), // SLTU
                        0b100 => registers[rs1] ^ registers[rs2], // XOR
                        0b101 => funct7 switch
                        {
                            0b0000000 => registers[rs1] >> (int) registers[rs2], // SRL
                            0b0100000 => (uint) SignExtend((int) registers[rs1] >> (int) registers[rs2], (int) (31 - (registers[rs2] % 32))), // SRA
                            _ => throw new IllegalInstructionException(PC, instruction),
                        },
                        0b110 => registers[rs1] | registers[rs2], // OR
                        0b111 => registers[rs1] & registers[rs2], // AND
                        _ => throw new IllegalInstructionException(PC, instruction),
                    };
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
                                    throw new IllegalInstructionException(PC, instruction);
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
                            throw new IllegalInstructionException(PC, instruction);
                    }
                    break;
                case 0b110_0011:
                    // Branch
                    bool conditionMet = funct3 switch
                    {
                        0b000 => registers[rs1] == registers[rs2], // BEQ
                        0b001 => registers[rs1] != registers[rs2], // BNE
                        0b100 => (int)registers[rs1] < (int)registers[rs2], // BLT
                        0b101 => (int)registers[rs1] >= (int)registers[rs2], // BGE
                        0b110 => registers[rs1] < registers[rs2], // BLTU
                        0b111 => registers[rs1] >= registers[rs2], // BGEU
                        _ => throw new IllegalInstructionException(PC, instruction),
                    };
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
                case 0b110_0111:
                    // JALR
                    immediate = IComputeImmediate(instruction);
                    var oldPC = PC;
                    PC = registers[rs1] + (uint) immediate;
                    registers[rd] = oldPC + 4;
                    pcNeedsAdjusting = false;
                    break;
                default:
                    throw new IllegalInstructionException(PC, instruction);
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

        if ((bitPosition < 31) && ((value & (1 << bitPosition)) != 0))
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
