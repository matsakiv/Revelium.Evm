using System.Text.Json.Serialization;

namespace Revelium.Evm.BlockScout.Models;

public class Parameter
{
    [JsonPropertyName("indexed")]
    public bool Indexed { get; set; }
    [JsonPropertyName("name")]
    public string Name { get; set; } = default!;
    [JsonPropertyName("type")]
    public string Type { get; set; } = default!;
    [JsonPropertyName("value")]
    public string Value { get; set; } = default!;
}
