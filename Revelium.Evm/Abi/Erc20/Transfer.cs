using Nethereum.ABI.FunctionEncoding.Attributes;
using Nethereum.Contracts;
using System.Numerics;

namespace Revelium.Evm.Abi.Erc20
{
    [Function("transfer")]
    public class Transfer : FunctionMessage
    {
        [Parameter("address", "_to", 1)]
        public string To { get; init; } = default!;

        [Parameter("uint256", "_value", 2)]
        public BigInteger Value { get; init; }
    }
}
