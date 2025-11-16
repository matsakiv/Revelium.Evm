using System.Text.Json.Serialization;

namespace Revelium.Evm.BlockScout.Models;

public class Holder
{
    [JsonPropertyName("address")]
    public Address Address { get; set; } = default!;
    [JsonPropertyName("value")]
    public string Value { get; set; } = default!;
    [JsonPropertyName("token_id")]
    public string? TokenId { get; set; }
    [JsonPropertyName("token")]
    public Token Token { get; set; } = default!;
}
