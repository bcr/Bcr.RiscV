namespace Bcr.RiscV.Emulator.Console;

interface IEcall
{
    bool HandleEcall(uint[] registers, out int returnCode);
}
