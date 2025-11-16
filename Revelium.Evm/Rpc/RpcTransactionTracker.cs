using Incendium;
using Microsoft.Extensions.Logging;
using Revelium.Evm.Rpc.Abstract;
using Revelium.Evm.Rpc.Events;
using Revelium.Evm.Rpc.Models;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Revelium.Evm.Rpc;

/// <summary>
/// A transaction tracker for RPC transactions.
/// </summary>
public class RpcTransactionTracker(
    IRpcClient rpc,
    ILogger<RpcTransactionTracker>? logger = null) : IRpcTransactionTracker
{
    public const int TRACKING_ERROR = 1;
    public const int TIMEOUT_REACHED_ERROR = 2;
    public const int TASK_CANCELED_ERROR = 3;

    public event EventHandler<TransactionReceipt>? ReceiptReceived;
    public event EventHandler<ErrorEventArgs>? ErrorReceived;
    public event EventHandler<ErrorEventArgs>? TimeOutReached;
    public event EventHandler<ErrorEventArgs>? Canceled;

    private readonly IRpcClient _rpc = rpc ?? throw new ArgumentNullException(nameof(rpc));
    private readonly ILogger<RpcTransactionTracker>? _logger = logger;

    /// <summary>
    /// Tracks a transaction.
    /// </summary>
    /// <param name="txId">The transaction ID.</param>
    /// <param name="updateInterval">The update interval.</param>
    /// <param name="timeOut">The timeout.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    public Task TrackTransactionAsync(
        string txId,
        TimeSpan updateInterval,
        TimeSpan? timeOut = null,
        CancellationToken cancellationToken = default)
    {
        return Task.Run(async () =>
        {
            try
            {
                var startTimeStamp = DateTimeOffset.UtcNow;

                while (!cancellationToken.IsCancellationRequested)
                {
                    if (timeOut != null && DateTimeOffset.UtcNow >= startTimeStamp + timeOut)
                    {
                        _logger?.LogWarning("Timeout reached for txId {txId}", txId);

                        TimeOutReached?.Invoke(this, new ErrorEventArgs
                        {
                            TxId = txId,
                            Error = new Error(TIMEOUT_REACHED_ERROR, "Timeout reached")
                        });
                        return;
                    }

                    var (receipt, error) = await _rpc
                        .GetTransactionReceiptAsync(txId, cancellationToken);

                    if (error != null)
                    {
                        ErrorReceived?.Invoke(this, new ErrorEventArgs
                        {
                            TxId = txId,
                            Error = error
                        });
                        return;
                    }

                    if (receipt != null)
                    {
                        ReceiptReceived?.Invoke(this, receipt);
                        return;
                    }

                    _logger?.LogInformation("Waiting for {txId} receipt", txId);

                    await Task.Delay(updateInterval, cancellationToken);
                }

                Canceled?.Invoke(this, new ErrorEventArgs
                {
                    TxId = txId,
                    Error = new Error(TASK_CANCELED_ERROR, "Task canceled")
                });

            }
            catch (OperationCanceledException)
            {
                Canceled?.Invoke(this, new ErrorEventArgs
                {
                    TxId = txId,
                    Error = new Error(TASK_CANCELED_ERROR, "Task canceled")
                });
            }
            catch (Exception e)
            {
                ErrorReceived?.Invoke(this, new ErrorEventArgs
                {
                    TxId = txId,
                    Error = new Error(TRACKING_ERROR, "Transaction tracker error", e)
                });
            }
        }, cancellationToken);
    }
}
