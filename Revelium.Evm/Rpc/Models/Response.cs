using System.Text.Json.Serialization;

namespace Revelium.Evm.Rpc.Models
{
    public class RpcError
    {
        [JsonPropertyName("code")]
        public int Code { get; init; }
        [JsonPropertyName("message")]
        public string Message { get; init; } = default!;
        [JsonPropertyName("data")]
        public string Data { get; init; } = default!;
    }

    public class Response<T>
    {
        [JsonPropertyName("id")]
        public int Id { get; init; }
        [JsonPropertyName("jsonrpc")]
        public string JsonRpc { get; init; } = default!;
        [JsonPropertyName("result")]
        public T Result { get; init; } = default!;
        [JsonPropertyName("error")]
        public RpcError? Error { get; init; }
    }
}
