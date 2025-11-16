using System.Text.Json.Serialization;

namespace Revelium.Evm.BlockScout.Models;

public class Address
{
    [JsonPropertyName("hash")]
    public string Hash { get; set; } = default!;
    [JsonPropertyName("is_contract")]
    public bool IsContract { get; set; }
    [JsonPropertyName("is_verified")]
    public bool? IsVerified { get; set; }
    [JsonPropertyName("name")]
    public string? Name { get; set; }
    [JsonPropertyName("ens_domain_name")]
    public string? EnsDomainName { get; set; }
}
