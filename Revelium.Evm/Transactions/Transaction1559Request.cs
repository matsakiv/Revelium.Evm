using Nethereum.Model;
using Nethereum.RPC.Eth.DTOs;
using Revelium.Evm.Common;
using Revelium.Evm.Transactions.Abstract;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Revelium.Evm.Transactions
{
    public class AccessList
    {
        public string? Address { get; init; }
        public List<string>? StorageKeys { get; init; }
    }

    public class Transaction1559Request : TransactionRequestBase
    {
        public BigInteger MaxFeePerGas { get; set; }
        public BigInteger MaxPriorityFeePerGas { get; set; }
        public List<AccessList>? AccessList { get; set; }

        public Transaction1559Request() { }

        public Transaction1559Request(TransactionInput txInput, BigInteger chainId)
        {
            From                 = txInput.From.ToLowerInvariant();
            To                   = txInput.To.ToLowerInvariant();
            Value                = txInput.Value ?? BigInteger.Zero;
            Nonce                = txInput.Nonce ?? BigInteger.Zero;
            MaxFeePerGas         = txInput.MaxFeePerGas ?? BigInteger.Zero;
            MaxPriorityFeePerGas = txInput.MaxPriorityFeePerGas ?? BigInteger.Zero;
            GasLimit             = txInput.Gas ?? BigInteger.Zero;
            Data                 = txInput.Data;
            ChainId              = chainId;
            AccessList           = txInput.AccessList
                ?.Select(a => new AccessList
                {
                    Address = a.Address,
                    StorageKeys = a.StorageKeys
                })
                .ToList();
        }

        public override SignedTransaction GetTransaction()
        {
            var signature = R != null
                ? new Signature(r: R, s: S, v: V)
                : null;

            var accessList = AccessList
                ?.Select(a => new AccessListItem
                {
                    Address = a.Address,
                    StorageKeys = a.StorageKeys
                        .Select(k => Hex.FromString(k))
                        .ToList()
                })
                .ToList();

            return new Transaction1559(
                chainId: ChainId,
                nonce: Nonce,
                maxPriorityFeePerGas: MaxPriorityFeePerGas,
                maxFeePerGas: MaxFeePerGas,
                gasLimit: GasLimit,
                receiverAddress: To,
                amount: Value,
                data: Data,
                accessList: accessList,
                signature: signature);
        }
    }
}
