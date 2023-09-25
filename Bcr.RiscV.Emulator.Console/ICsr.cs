namespace Bcr.RiscV.Emulator.Console;

interface ICsr
{
    uint Read(uint csr);
    uint ReadWrite(uint csr, uint value);
    uint ReadSet(uint csr, uint value);
}
