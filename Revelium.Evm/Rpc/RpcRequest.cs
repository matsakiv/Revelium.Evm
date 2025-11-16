using System.Text.Json.Serialization;

namespace Revelium.Evm.Rpc;

/// <summary>
/// Represents a JSON-RPC request.
/// </summary>
public record RpcRequest
{
    /// <summary>
    /// The JSON-RPC version.
    /// </summary>
    [JsonPropertyName("jsonrpc")]
    public string JsonRpc { get; set; } = "2.0";

    /// <summary>
    /// The method to call.
    /// </summary>
    [JsonPropertyName("method")]
    public string Method { get; set; } = "";

    /// <summary>
    /// The parameters to pass to the method.
    /// </summary>
    [JsonPropertyName("params")]
    public object[] Params { get; set; } = [];

    /// <summary>
    /// The request ID.
    /// </summary>
    [JsonPropertyName("id")]
    public int Id { get; set; } = 1;
}
