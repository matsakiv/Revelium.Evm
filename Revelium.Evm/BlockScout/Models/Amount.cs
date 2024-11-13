using Revelium.Evm.Common;
using System.Numerics;
using System.Text.Json.Serialization;

namespace Revelium.Evm.BlockScout.Models
{
    public class Amount
    {
        [JsonPropertyName("decimals")]
        public string Decimals { get; set; } = default!;
        [JsonPropertyName("value")]
        public string Value { get; set; } = default!;

        public decimal ToDecimal()
        {
            var decimals = int.Parse(Decimals);
            var exponent = BigInteger.Pow(10, decimals);
            var value = BigInteger.Parse(Value);

            return value.Divide(exponent);
        }
    }
}
