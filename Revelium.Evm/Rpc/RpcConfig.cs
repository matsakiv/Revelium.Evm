namespace Revelium.Evm.Rpc;

public class RpcConfig
{
    public string Url { get; init; } = default!;
    public int RateLimit { get; init; }
    public int RateLimitTimeUnitSec { get; init; }
    public int RetryCount { get; init; }
    public int FirstRetryDelayMs { get; init; }
    public long? ChainId { get; init; }
}
