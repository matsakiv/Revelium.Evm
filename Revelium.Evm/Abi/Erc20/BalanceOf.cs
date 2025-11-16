using Nethereum.ABI.FunctionEncoding.Attributes;
using Nethereum.Contracts;

namespace Revelium.Evm.Abi.Erc20;

[Function("balanceOf")]
public class BalanceOf : FunctionMessage
{
    [Parameter("address", "account", 1)]
    public string Account { get; set; } = default!;
}
