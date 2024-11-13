using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Revelium.Evm.BlockScout.Models
{
    public class Decoded
    {
        [JsonPropertyName("method_call")]
        public string MethodCall { get; set; } = default!;
        [JsonPropertyName("method_id")]
        public string MethodId { get; set; } = default!;
        [JsonPropertyName("parameters")]
        public List<Parameter> Parameters { get; set; } = default!;
    }
}
