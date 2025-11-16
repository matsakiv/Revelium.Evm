using Nethereum.Contracts;
using Nethereum.Hex.HexTypes;
using Nethereum.RPC.Eth.DTOs;
using System.Numerics;

namespace Revelium.Evm.Common;

public static class TransactionInputExtensions
{
    public static TransactionInput CreateTransactionInput<TContractMessage>(
        this TContractMessage contractMessage,
        string contractAddress,
        BigInteger chainId) where TContractMessage : FunctionMessage
    {
        var txInput = contractMessage.CreateTransactionInput(contractAddress);
        txInput.ChainId = new HexBigInteger(chainId);

        return txInput;
    }
}
