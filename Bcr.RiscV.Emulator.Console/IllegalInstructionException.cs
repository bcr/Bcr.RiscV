namespace Bcr.RiscV.Emulator.Console;

class IllegalInstructionException : Exception
{
    public IllegalInstructionException() : base() { }
    public IllegalInstructionException(string message) : base(message) { }
    public IllegalInstructionException(string message, Exception inner) : base(message, inner) { }
    public IllegalInstructionException(uint PC, uint instruction) : this($"Illegal instruction {instruction:X8} at address {PC:X8}") {}
}
