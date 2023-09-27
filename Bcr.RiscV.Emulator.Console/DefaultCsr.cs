using Microsoft.Extensions.Logging;

namespace Bcr.RiscV.Emulator.Console;

// https://five-embeddev.com/riscv-isa-manual/latest/priv-csrs.html
// https://five-embeddev.com/riscv-isa-manual/latest/machine.html#otherpriv
// 180 satp
// 300 mstatus
// 302 medeleg
// 303 mideleg
// 304 mie
// 305 mtvec
// 341 mepc
// 3A0 pmpcfg0
// 3B0 pmpaddr0
// 744 mnstatus?
// F14 mhartid

class DefaultCsr : ICsr
{
    private Dictionary<uint, uint> _csrDictionary = new();
    private ILogger<DefaultCsr> _logger;

    public DefaultCsr(ILogger<DefaultCsr> logger) => _logger = logger;

    public uint Read(uint csr)
    {
        _csrDictionary.TryGetValue(csr, out uint currentValue);
        return currentValue;
    }

    public uint ReadSet(uint csr, uint value)
    {
        _csrDictionary.TryGetValue(csr, out uint currentValue);
        _csrDictionary[csr] = currentValue | value;
        return currentValue;
    }

    public uint ReadWrite(uint csr, uint value)
    {
        _csrDictionary.TryGetValue(csr, out uint currentValue);
        _csrDictionary[csr] = value;
        return currentValue;
    }
}
