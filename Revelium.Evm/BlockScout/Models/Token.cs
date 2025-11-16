using System.Text.Json.Serialization;

namespace Revelium.Evm.BlockScout.Models;

public class Token
{
    [JsonPropertyName("address")]
    public string Address { get; set; } = default!;
    [JsonPropertyName("decimals")]
    public string? Decimals { get; set; }
    [JsonPropertyName("holders")]
    public string Holders { get; set; } = default!;
    [JsonPropertyName("name")]
    public string? Name { get; set; }
    [JsonPropertyName("symbol")]
    public string? Symbol { get; set; }
    [JsonPropertyName("total_supply")]
    public string? TotalSupply { get; set; }
    [JsonPropertyName("type")]
    public string Type { get; set; } = default!;
}

public class TokenBalance
{
    [JsonPropertyName("token")]
    public Token Token { get; set; } = default!;
    [JsonPropertyName("value")]
    public string Value { get; set; } = default!;
}
