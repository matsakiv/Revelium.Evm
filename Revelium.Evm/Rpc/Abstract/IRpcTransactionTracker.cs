using Revelium.Evm.Rpc.Events;
using Revelium.Evm.Rpc.Models;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Revelium.Evm.Rpc.Abstract;

public interface IRpcTransactionTracker
{
    event EventHandler<ErrorEventArgs>? Canceled;
    event EventHandler<ErrorEventArgs>? ErrorReceived;
    event EventHandler<TransactionReceipt>? ReceiptReceived;

    Task TrackTransactionAsync(
        string txId,
        TimeSpan updateInterval,
        TimeSpan? timeOut = null,
        CancellationToken cancellationToken = default);
}
