using Revelium.Evm.Common;
using Revelium.Evm.Crypto.Abstract;
using Revelium.Evm.Transactions.Abstract;
using System.Collections.Concurrent;
using System.Threading;

namespace Revelium.Evm.Rpc
{
    public class TransactionRequestParams
    {
        public TransactionRequestBase Tx { get; init; } = default!;
        public bool EstimateGas { get; init; }
        public uint? EstimateGasReserveInPercent { get; init; }
    }

    public class RpcCallSequencer(
        RpcClient rpc,
        ISigner signer,
        int capacity,
        string? networkId = null) : BoundedCallSequencer<TransactionRequestParams, string>(
            CreateHandlerCallback(rpc, signer), capacity)
    {
        private static ConcurrentDictionary<string, RpcCallSequencer>? _instances;

        public string? NetworkId { get; } = networkId;
        public string Address { get; } = signer.GetAddress();

        public static RpcCallSequencer GetOrAddInstance(
            RpcClient rpc,
            ISigner signer,
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

            return instances.GetOrAdd(instanceId, id => new RpcCallSequencer(rpc, signer, capacity, networkId));
        }

        private static HandlerCallback<TransactionRequestParams, string> CreateHandlerCallback(
            RpcClient rpc,
            ISigner signer)
        {
            return new HandlerCallback<TransactionRequestParams, string>(
                (@params, cancellationToken) => rpc.SignAndSendTransactionAsync(
                    tx: @params.Tx,
                    signer: signer,
                    estimateGas: @params.EstimateGas,
                    estimateGasReserveInPercent: @params.EstimateGasReserveInPercent,
                    cancellationToken: cancellationToken));
        }
    }
}
