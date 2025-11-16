using Nethereum.Model;
using Nethereum.RPC.Eth.DTOs;
using Revelium.Evm.Transactions.Abstract;
using System.Numerics;

namespace Revelium.Evm.Transactions;

public class TransactionLegacyRequest : TransactionRequestBase
{
    public BigInteger GasPrice { get; set; }

    public TransactionLegacyRequest() { }

    public TransactionLegacyRequest(TransactionInput txInput)
    {
        From = txInput.From.ToLowerInvariant();
        To = txInput.To.ToLowerInvariant();
        Value = txInput.Value?.Value ?? BigInteger.Zero;
        Nonce = txInput.Nonce?.Value ?? BigInteger.Zero;
        GasPrice = txInput.GasPrice?.Value ?? BigInteger.Zero;
        GasLimit = txInput.Gas?.Value ?? BigInteger.Zero;
        Data = txInput.Data;
        ChainId = txInput.ChainId?.Value ?? BigInteger.Zero;
    }

    public override SignedTransaction GetTransaction()
    {
        var tx = new LegacyTransactionChainId(
            to: To,
            amount: Value,
            nonce: Nonce,
            gasPrice: GasPrice,
            gasLimit: GasLimit,
            data: Data,
            chainId: ChainId);

        if (R != null)
            tx.SetSignature(new Signature { R = R, S = S, V = V });

        return tx;
    }
}
