using Revelium.Evm.Common;
using Revelium.Evm.Crypto.Abstract;
using Revelium.Evm.Rpc.Abstract;
using Revelium.Evm.Services;
using System.Collections.Concurrent;
using System.Threading;

namespace Revelium.Evm.Rpc;

/// <summary>
/// A call sequencer for RPC calls.
/// </summary>
public class RpcCallSequencer(
    IRpcClient rpc,
    ISigner signer,
    NonceManager nonceManager,
    int capacity,
    string? networkId = null) : BoundedCallSequencer<TransactionRequestParams, string>(
        CreateHandlerCallback(rpc, signer, nonceManager), capacity)
{
    private static ConcurrentDictionary<string, RpcCallSequencer>? _instances;

    public string? NetworkId { get; } = networkId;
    public string Address { get; } = signer.GetAddress();

    /// <summary>
    /// Gets or adds an instance of the call sequencer.
    /// </summary>
    /// <param name="rpc">The RPC client.</param>
    /// <param name="signer">The signer.</param>
    /// <param name="nonceManager">Nonce manager.</param>
    /// <param name="capacity">The capacity of the call sequencer.</param>
    /// <param name="networkId">The network ID.</param>
    /// <returns>The call sequencer.</returns>
    public static RpcCallSequencer GetOrAddInstance(
        IRpcClient rpc,
        ISigner signer,
        NonceManager nonceManager,
        int capacity,
        string? networkId = null)
    {
        var instances = _instances;

        if (instances == null)
        {
            Interlocked.CompareExchange(ref _instances, [], null);
            instances = _instances;
        }

        var instanceId = $"{networkId ?? ""}:{signer.GetAddress()}";

        return instances.GetOrAdd(
            instanceId,
            id => new RpcCallSequencer(rpc, signer, nonceManager, capacity, networkId));
    }

    private static HandlerCallback<TransactionRequestParams, string> CreateHandlerCallback(
        IRpcClient rpc,
        ISigner signer,
        NonceManager nonceManager)
    {
        return new HandlerCallback<TransactionRequestParams, string>(
            (@params, ct) => rpc.SignAndSendTransactionAsync(
                tx: @params.Tx,
                signer: signer,
                nonceManager: nonceManager,
                estimateGas: @params.EstimateGas,
                estimateGasReserveInPercent: @params.EstimateGasReserveInPercent,
                ct: ct));
    }
}
