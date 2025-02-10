using Incendium;
using Microsoft.Extensions.Logging;
using Revelium.Evm.Crypto.Abstract;
using Revelium.Evm.Rpc;
using Revelium.Evm.Rpc.Parameters;
using Revelium.Evm.Transactions;
using Revelium.Evm.Transactions.Abstract;
using System;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;

namespace Revelium.Evm.Common
{
    public static class RpcClientExtensions
    {
        public static Task<Result<string>> SendTransactionAsync(
            this RpcClient rpc,
            TransactionRequestBase transaction,
            CancellationToken cancellationToken = default)
        {
            return rpc.SendRawTransactionAsync(
                transaction.GetRlpEncoded(),
                cancellationToken);
        }

        public static async Task<Result<string>> SignAndSendTransactionAsync(
            this RpcClient rpc,
            TransactionRequestBase tx,
            ISigner signer,
            bool estimateGas = true,
            uint? estimateGasReserveInPercent = 0,
            string? networkId = null,
            ILogger? logger = null,
            CancellationToken cancellationToken = default)
        {
            var nonceManager = NonceManager.GetOrAddInstance(tx.From, networkId);

            var (nonceLock, nonceError) = await nonceManager.GetNonceAsync(
                rpc,
                pending: true,
                logger: logger,
                cancellationToken);

            if (nonceError != null)
                return nonceError;

            try
            {
                tx.Nonce = nonceLock.Nonce;

                logger?.LogDebug("Transaction nonce is {@nonce}", tx.Nonce.ToString());

                if (estimateGas)
                {
                    var (estimatedGas, estimateGasError) = tx switch
                    {
                        Transaction1559Request eip1559Tx => await rpc.EstimateGasAsync(eip1559Tx),
                        TransactionLegacyRequest legacyTx => await rpc.EstimateGasAsync(legacyTx),
                        _ => throw new NotImplementedException(),
                    };

                    if (estimateGasError != null)
                    {
                        nonceManager.Reset(tx.Nonce, logger);
                        return estimateGasError;
                    }

                    tx.GasLimit = estimatedGas;

                    if (estimateGasReserveInPercent != null && estimateGasReserveInPercent >= 0)
                        tx.GasLimit += tx.GasLimit / 100 * estimateGasReserveInPercent.Value;
                }

                signer.Sign(tx);

                if (!tx.Verify())
                {
                    nonceManager.Reset(tx.Nonce, logger);
                    return new Error(Errors.TX_VERIFY_ERROR, "Can't verify transaction");
                }

                var (txId, txSendError) = await rpc.SendTransactionAsync(tx, cancellationToken);

                if (txSendError != null)
                {
                    nonceManager.Reset(tx.Nonce, logger);
                    return new Error(Errors.TX_SEND_ERROR, "Transaction sending error");
                }

                return txId;
            }
            finally
            {
                nonceLock?.Dispose();
            }
        }

        public static Task<Result<BigInteger>> EstimateGasAsync(
            this RpcClient rpc,
            TransactionLegacyRequest tx,
            CancellationToken cancellationToken = default)
        {
            return rpc.EstimateGasAsync(
                to: tx.To,
                from: tx.From,
                gasPrice: tx.GasPrice,
                value: tx.Value,
                data: tx.Data,
                block: BlockNumber.Latest,
                cancellationToken: cancellationToken);
        }

        public static Task<Result<BigInteger>> EstimateGasAsync(
            this RpcClient rpc,
            Transaction1559Request tx,
            CancellationToken cancellationToken = default)
        {
            return rpc.EstimateGasAsync(
                to: tx.To,
                from: tx.From,
                maxPriorityFeePerGas: tx.MaxPriorityFeePerGas,
                maxFeePerGas: tx.MaxFeePerGas,
                value: tx.Value,
                data: tx.Data,
                block: BlockNumber.Latest,
                cancellationToken: cancellationToken);
        }
    }
}
