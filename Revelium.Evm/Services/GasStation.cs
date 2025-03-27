using Incendium;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Nethereum.Hex.HexTypes;
using Revelium.Evm.Common;
using Revelium.Evm.Rpc;
using Revelium.Evm.Rpc.Models;
using Revelium.Evm.Rpc.Parameters;
using System;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;

namespace Revelium.Evm.Services
{
    /// <summary>
    /// Event arguments for fee per gas updated.
    /// </summary>
    public class FeePerGasEventArgs
    {
        /// <summary>
        /// The base fee per gas.
        /// </summary>
        public BigInteger BaseFeePerGas { get; set; }

        /// <summary>
        /// The max priority fee per gas.
        /// </summary>
        public BigInteger MaxPriorityFeePerGas { get; set; }
    }

    /// <summary>
    /// Options for the gas station.
    /// </summary>
    public class GasStationOptions
    {
        /// <summary>
        /// The update interval in milliseconds.
        /// </summary>
        public int UpdateIntervalMs { get; init; } = 10000;
    }

    /// <summary>
    /// Gas station service.
    /// </summary>
    /// <param name="options">The options for the gas station.</param>
    /// <param name="rpc">The RPC client.</param>
    /// <param name="logger">The logger.</param>
    public class GasStation(
        GasStationOptions options,
        RpcClient rpc,
        ILogger<GasStation>? logger = null) : IHostedService
    {
        /// <summary>
        /// Event raised when the fee per gas is updated.
        /// </summary>
        public event EventHandler<FeePerGasEventArgs>? OnFeePerGasUpdated;

        private readonly RpcClient _rpc = rpc;
        private readonly TimeSpan _updateInterval = TimeSpan.FromMilliseconds(options.UpdateIntervalMs);
        private readonly ILogger<GasStation>? _logger = logger;
        private readonly object _startStopLock = new();
        private readonly object _lock = new();
        private BigInteger? _baseFeePerGas;
        private BigInteger? _maxPriorityFeePerGas;
        private CancellationTokenSource? _cts;
        private bool _isRunning;

        /// <summary>
        /// Starts the gas station service.
        /// </summary>
        /// <param name="ct">The cancellation token.</param>
        public Task StartAsync(CancellationToken ct)
        {
            lock (_startStopLock)
            {
                if (_isRunning)
                    return Task.CompletedTask;

                _cts = new CancellationTokenSource();
                _isRunning = true;

                _ = Task.Run(async () => await DoWorkAsync(_cts.Token), _cts.Token);
            }

            return Task.CompletedTask;
        }

        /// <summary>
        /// Stops the gas station service.
        /// </summary>
        /// <param name="ct">The cancellation token.</param>
        public Task StopAsync(CancellationToken ct)
        {
            lock (_startStopLock)
            {
                if (!_isRunning)
                    return Task.CompletedTask;

                _cts?.Cancel();
                _cts = null;
                _isRunning = false;
            }

            return Task.CompletedTask;
        }

        /// <summary>
        /// Gets the base fee per gas.
        /// </summary>
        /// <returns>The base fee per gas.</returns>
        public Result<BigInteger> GetBaseFeePerGas()
        {
            lock (_lock)
            {
                return _baseFeePerGas != null
                    ? Result<BigInteger>.Success(_baseFeePerGas.Value)
                    : Result<BigInteger>.Failure(new Error("BaseFeePerGas not set"));
            }
        }

        /// <summary>
        /// Gets the max priority fee per gas.
        /// </summary>
        /// <returns>The max priority fee per gas.</returns>
        public Result<BigInteger> GetMaxPriorityFeePerGas()
        {
            lock (_lock)
            {
                return _maxPriorityFeePerGas != null
                    ? Result<BigInteger>.Success(_maxPriorityFeePerGas.Value)
                    : Result<BigInteger>.Failure(new Error("MaxPriorityFeePerGas not set"));
            }
        }

        private async Task DoWorkAsync(CancellationToken cancellationToken)
        {
            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    await UpdateAsync(cancellationToken);
                    await Task.Delay(_updateInterval, cancellationToken);
                }
            }
            catch (OperationCanceledException)
            {
                // expected
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error updating gas prices");
            }
        }

        private async Task UpdateAsync(CancellationToken cancellationToken = default)
        {
            var (((maxPriorityFeePerGas, feeError), (block, blockError)), error) =
                await _rpc.SendBatchAsync<BigInteger, LightBlock>(
                    RpcClient.CreateMaxPriorityFeePerGasRequest(),
                    RpcClient.CreateBlockByNumberRequest(BlockNumber.Pending, includeTransactions: false),
                    cancellationToken);

            if (error != null)
            {
                _logger?.LogError("Error getting gas prices: {@Error}", error);
                return;
            }

            if (feeError != null)
            {
                _logger?.LogError("Error getting max priority fee per gas: {@Error}", feeError);
                return;
            }

            if (blockError != null)
            {
                _logger?.LogError("Error getting block: {@Error}", blockError);
                return;
            }

            if (block == null)
            {
                _logger?.LogError("Block is null");
                return;
            }

            var baseFeePerGas = new HexBigInteger(block.BaseFeePerGas).Value;

            lock (_lock)
            {
                _baseFeePerGas = baseFeePerGas;
                _maxPriorityFeePerGas = maxPriorityFeePerGas;
            }

            OnFeePerGasUpdated?.Invoke(this, new FeePerGasEventArgs
            {
                BaseFeePerGas = baseFeePerGas,
                MaxPriorityFeePerGas = maxPriorityFeePerGas
            });
        }
    }
}
