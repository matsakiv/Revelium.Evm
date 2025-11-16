using Incendium;

namespace Revelium.Evm.Rpc.Events;

public class ErrorEventArgs
{
    public string TxId { get; init; } = default!;
    public Error Error { get; init; } = default!;
}
