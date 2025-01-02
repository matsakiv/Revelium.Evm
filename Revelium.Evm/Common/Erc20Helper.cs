using Incendium;
using Nethereum.Contracts;
using Nethereum.Hex.HexTypes;
using Revelium.Evm.Abi.Erc20;
using Revelium.Evm.Rpc;
using Revelium.Evm.Rpc.Parameters;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;

namespace Revelium.Evm.Common
{
    public static class Erc20Helper
    {
        public static async Task<Result<BigInteger>> GetAllowanceAsync(
            this RpcClient rpc,
            string tokenContract,
            string owner,
            string spender,
            CancellationToken cancellationToken = default)
        {
            var allowance = new Allowance
            {
                Owner = owner,
                Spender = spender
            };

            var (hexResult, error) = await rpc.CallAsync<string>(
                to: tokenContract,
                input: allowance.CreateTransactionInput(tokenContract).Data,
                block: BlockNumber.Latest,
                cancellationToken: cancellationToken);

            if (error != null)
                return error;

            return new HexBigInteger(hexResult).Value;
        }

        public static async Task<Result<BigInteger>> GetErc20TokenBalanceAsync(
            this RpcClient rpc,
            string tokenContract,
            string account,
            BlockNumber? defaultBlock = null,
            BigInteger? chainId = null,
            CancellationToken cancellationToken = default)
        {
            var balanceOf = new BalanceOf()
            {
                Account = account
            };

            var (hexResult, error) = await rpc.CallAsync<string>(
                to: tokenContract,
                input: balanceOf.CreateTransactionInput(tokenContract, chainId ?? BigInteger.Zero).Data,
                block: defaultBlock,
                cancellationToken: cancellationToken);

            if (error != null)
                return error;

            return new HexBigInteger(hexResult).Value;
        }
    }
}
