using Nethereum.ABI.FunctionEncoding.Attributes;
using Nethereum.Contracts;

namespace Revelium.Evm.Abi.Erc20
{
    [Function("allowance", "uint")]
    public class Allowance : FunctionMessage
    {
        [Parameter("address", "_owner", 1)]
        public string Owner { get; init; } = default!;

        [Parameter("address", "_spender", 2)]
        public string Spender { get; init; } = default!;
    }
}
