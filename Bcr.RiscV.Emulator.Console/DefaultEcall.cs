namespace Bcr.RiscV.Emulator.Console;

class DefaultEcall : IEcall
{
    class SyscallNumbers
    {
        public const uint exit = 93;
    }

    public bool HandleEcall(uint[] registers, out int returnCode)
    {
        if (registers[Registers.a7] == SyscallNumbers.exit)
        {
            returnCode = (int) registers[Registers.a0];
            return true;
        }
        throw new NotImplementedException();
    }
}
