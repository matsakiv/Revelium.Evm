using Incendium;
using Nethereum.Contracts;
using Nethereum.Hex.HexTypes;
using Revelium.Evm.Abi.Erc20;
using Revelium.Evm.Rpc.Abstract;
using Revelium.Evm.Rpc.Parameters;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;

namespace Revelium.Evm.Common;

public static class Erc20Helper
{
    public static async Task<Result<BigInteger>> GetAllowanceAsync(
        this IRpcClient rpc,
        string tokenContract,
        string owner,
        string spender,
        BlockNumber? block = null,
        CancellationToken cancellationToken = default)
    {
        var allowance = new Allowance
        {
            Owner = owner,
            Spender = spender
        };

        var input = rpc.ChainId != null
            ? allowance.CreateTransactionInput(tokenContract, rpc.ChainId.Value).Data
            : allowance.CreateTransactionInput(tokenContract).Data;

        var (hexResult, error) = await rpc.CallAsync<string>(
            to: tokenContract,
            input: input,
            block: block,
            cancellationToken: cancellationToken);

        if (error != null)
            return error;

        return new HexBigInteger(hexResult).Value;
    }

    public static async Task<Result<BigInteger>> GetErc20TokenBalanceAsync(
        this IRpcClient rpc,
        string tokenContract,
        string account,
        BlockNumber? block = null,
        CancellationToken cancellationToken = default)
    {
        var balanceOf = new BalanceOf()
        {
            Account = account
        };

        var input = rpc.ChainId != null
            ? balanceOf.CreateTransactionInput(tokenContract, rpc.ChainId.Value).Data
            : balanceOf.CreateTransactionInput(tokenContract).Data;

        var (hexResult, error) = await rpc.CallAsync<string>(
            to: tokenContract,
            input: input,
            block: block,
            cancellationToken: cancellationToken);

        if (error != null)
            return error;

        return new HexBigInteger(hexResult).Value;
    }
}
