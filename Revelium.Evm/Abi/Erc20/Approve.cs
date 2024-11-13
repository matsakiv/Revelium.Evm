using Nethereum.ABI.FunctionEncoding.Attributes;
using Nethereum.Contracts;
using System.Numerics;

namespace Revelium.Evm.Abi.Erc20
{
    [Function("approve")]
    public class Approve : FunctionMessage
    {
        [Parameter("address", "_spender", 1)]
        public string Spender { get; init; } = default!;

        [Parameter("uint256", "_value", 2)]
        public BigInteger Value { get; init; }
    }
}
