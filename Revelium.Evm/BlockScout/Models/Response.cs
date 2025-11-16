using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Revelium.Evm.BlockScout.Models;


public class Response<T>
{
    [JsonPropertyName("items")]
    public List<T> Items { get; set; } = default!;
    [JsonPropertyName("next_page_params")]
    public NextPageParams? NextPageParams { get; set; }
}
