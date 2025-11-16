using Nethereum.Model;
using Nethereum.RPC.Eth.DTOs;
using Revelium.Evm.Common;
using Revelium.Evm.Transactions.Abstract;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Revelium.Evm.Transactions;

public class AccessList
{
    public string? Address { get; init; }
    public List<string>? StorageKeys { get; init; }
}

public class Transaction1559Request : TransactionRequestBase
{
    public BigInteger? MaxFeePerGas { get; set; }
    public BigInteger? MaxPriorityFeePerGas { get; set; }
    public List<AccessList>? AccessList { get; set; }

    public Transaction1559Request() { }

    public Transaction1559Request(TransactionInput txInput)
    {
        From = txInput.From.ToLowerInvariant();
        To = txInput.To.ToLowerInvariant();
        Value = txInput.Value?.Value ?? BigInteger.Zero;
        Nonce = txInput.Nonce?.Value ?? BigInteger.Zero;
        MaxFeePerGas = txInput.MaxFeePerGas?.Value ?? null;
        MaxPriorityFeePerGas = txInput.MaxPriorityFeePerGas?.Value ?? null;
        GasLimit = txInput.Gas?.Value ?? BigInteger.Zero;
        Data = txInput.Data;
        ChainId = txInput.ChainId?.Value ?? BigInteger.Zero;
        AccessList = txInput.AccessList?.Select(a => new AccessList
        {
            Address = a.Address,
            StorageKeys = a.StorageKeys
        }).ToList();
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
                StorageKeys = [.. a.StorageKeys.Select(k => Hex.FromString(k))]
            })
            .ToList();

        var tx = new Transaction1559(
            chainId: ChainId,
            nonce: Nonce,
            maxPriorityFeePerGas: MaxPriorityFeePerGas,
            maxFeePerGas: MaxFeePerGas,
            gasLimit: GasLimit,
            receiverAddress: To,
            amount: Value,
            data: Data,
            accessList: accessList);

        tx.SetSignature(signature);

        return tx;
    }
}
