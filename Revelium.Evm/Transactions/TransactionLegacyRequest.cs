using Nethereum.Model;
using Nethereum.RPC.Eth.DTOs;
using Revelium.Evm.Transactions.Abstract;
using System.Numerics;

namespace Revelium.Evm.Transactions
{
    public class TransactionLegacyRequest : TransactionRequestBase
    {
        public BigInteger GasPrice { get; set; }

        public TransactionLegacyRequest() { }

        public TransactionLegacyRequest(TransactionInput txInput)
        {
            From     = txInput.From.ToLowerInvariant();
            To       = txInput.To.ToLowerInvariant();
            Value    = txInput.Value ?? BigInteger.Zero;
            Nonce    = txInput.Nonce ?? BigInteger.Zero;
            GasPrice = txInput.GasPrice ?? BigInteger.Zero;
            GasLimit = txInput.Gas ?? BigInteger.Zero;
            Data     = txInput.Data;
        }

        public override SignedTransaction GetTransaction()
        {
            var tx = new LegacyTransaction(
                to: To,
                amount: Value,
                nonce: Nonce,
                gasPrice: GasPrice,
                gasLimit: GasLimit,
                data: Data);

            if (R != null)
                tx.SetSignature(new Signature { R = R, S = S, V = V });

            return tx;
        }
    }
}
