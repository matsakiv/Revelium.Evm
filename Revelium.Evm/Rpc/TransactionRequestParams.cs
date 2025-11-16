using Revelium.Evm.Transactions.Abstract;

namespace Revelium.Evm.Rpc;

/// <summary>
/// Transaction request parameters
/// </summary>
public class TransactionRequestParams
{
    /// <summary>
    /// Unique client-generated request id to track the request
    /// </summary>
    public string RequestId { get; init; } = default!;

    /// <summary>
    /// Transaction
    /// </summary>
    public TransactionRequestBase Tx { get; init; } = default!;

    /// <summary>
    /// Flag indicating if gas should be estimated
    /// </summary>
    public bool EstimateGas { get; init; }

    /// <summary>
    /// Gas reserve percentage over the estimated gas
    /// </summary>
    public uint? EstimateGasReserveInPercent { get; init; }
}
