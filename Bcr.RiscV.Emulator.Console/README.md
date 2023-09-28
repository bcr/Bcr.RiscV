# C# RISC-V Emulator

In order to better understand the [RISC-V ISA](https://riscv.org/), I
implemented an emulator in C#.

## Does It Work?

Well, that's a good question. In order to check for "working" I found a set of
tests at the [riscv-tests](https://github.com/riscv-software-src/riscv-tests)
repo. My intent initially was to make "the simplest possible RISC-V emulator"
and implement the smallest conforming profile which would be the RV32I
instruction set. The `rv32ui` test suite is aligned with what I need and what
I implemented, with some quirks:

* This test framework requires the `MRET` instruction as well as
  all of the CSR instructions (Zicsr) which are not part of the RV32I
  instruction set. This requires nontrivial implementation of the CSR
  registers, most notably the `mepc` CSR for handling the `MRET` instruction.
* The provided Makefile isn't particularly interested in testing just the
  `rv32ui-p` tests (single core, no virtual memory.) I patched it, but it's
  still not quite right (wants to run the `-v` tests also, which I don't have
  any interest in running.)
* The expected input to the simulator under test is an ELF executable, so I got
  to learn about that.

My Makefile patch runs all of the `-p` tests before exploding when moving on to
the `-v` tests. But right up until then, it works great.

```diff
diff --git a/isa/Makefile b/isa/Makefile
index d66b901..e785a15 100644
--- a/isa/Makefile
+++ b/isa/Makefile
@@ -108,6 +108,7 @@ tests_out = $(addsuffix .out, $(filter rv64%,$(tests)))
 tests32_out = $(addsuffix .out32, $(filter rv32%,$(tests)))
 
 run: $(tests_out) $(tests32_out)
+run32: $(tests32_out)
 
 junk += $(tests) $(tests_dump) $(tests_hex) $(tests_out) $(tests32_out)
 
```

## So How Does It Work?

The trickiest part is getting code into the thing. Because the test suite uses
ELF executables, I implemented a memory abstraction that maps everything in the
ELF into read/write memory and throws a hissy fit if you access memory that's
not in an ELF segment.

I hardcoded the start address to `0x8000_0000` which is what the test framework
generates. If I was less lazy I'd resolve the symbol `_start` which is probably
smarter.

It runs until it gets an `ECALL` with a7=93 (which is the Linux
`exit` syscall number) and then exits the emulator with an exit code of
whatever is in `a0`. The testing framework uses this exit code to indicate any
problems (0 is all good, nonzero is generally "two times the failing test number,
plus one".) So an exit code of 5 means that test 2 failed in the code.

But it just loads an instruction, decodes it, then executes it. The trickiest
part of decoding is working with immediate values which take a half a dozen
different forms.

## Working
rv32ui-p-simple
rv32ui-p-addi
rv32ui-p-slli
rv32ui-p-ori
rv32ui-p-jal
rv32ui-p-beq
rv32ui-p-bne
rv32ui-p-blt
rv32ui-p-lui
rv32ui-p-auipc
rv32ui-p-sub
rv32ui-p-andi
rv32ui-p-xori
rv32ui-p-slti
rv32ui-p-sltiu
rv32ui-p-add
rv32ui-p-sll
rv32ui-p-slt
rv32ui-p-sltu
rv32ui-p-xor
rv32ui-p-or
rv32ui-p-and
rv32ui-p-srl
rv32ui-p-sra
rv32ui-p-jalr
rv32ui-p-bge
rv32ui-p-bltu
rv32ui-p-bgeu
rv32ui-p-srai
rv32ui-p-srli
rv32ui-p-lb
rv32ui-p-lbu
rv32ui-p-lh
rv32ui-p-lhu
rv32ui-p-lw
rv32ui-p-sb
rv32ui-p-sh
rv32ui-p-sw
rv32ui-p-ma_data
rv32ui-p-fence_i

## Broken

(None known)
