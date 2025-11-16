using Incendium;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Revelium.Evm.Rpc.Abstract;
using Revelium.Evm.Rpc.Parameters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;

namespace Revelium.Evm.Services;

/// <summary>
/// Options for the nonce manager.
/// </summary>
public class NonceManagerOptions
{
    /// <summary>
    /// The update interval in milliseconds.
    /// </summary>
    public int UpdateIntervalMs { get; init; } = 10 * 1000; // 10 seconds

    /// <summary>
    /// The nonce force update interval in milliseconds.
    /// </summary>
    public int NonceForceUpdateIntervalMs { get; init; } = 60 * 1000; // 1 minutes

    /// <summary>
    /// The offline nonce force reset interval in milliseconds.
    /// </summary>
    public int OfflineNonceForceResetIntervalMs { get; init; } = 2 * 60 * 1000; // 2 minutes

    /// <summary>
    /// The addresses to manage.
    /// </summary>
    public string[] Addresses { get; init; } = default!;

    /// <summary>
    /// Whether to use pending transactions count.
    /// </summary>
    public bool UsePending { get; init; } = true;
}

/// <summary>
/// Manages nonces for multiple addresses.
/// </summary>
public class NonceManager : IHostedService
{
    private class NonceEntry
    {
        public DateTimeOffset TimeStamp { get; set; }
        public BigInteger Nonce { get; set; }
    }

    /// <summary>
    /// Locks a nonce for an address.
    /// </summary>
    public class NonceLock : IDisposable
    {
        private readonly NonceManager _nonceManager;
        private readonly string _address;
        private readonly SemaphoreSlim _sync;
        private bool _isDisposed;

        private NonceLock(NonceManager nonceManager, string address, SemaphoreSlim sync)
        {
            _nonceManager = nonceManager;
            _address = address.ToLowerInvariant();
            _sync = sync;
        }

        /// <summary>
        /// Locks a nonce for an address.
        /// </summary>
        public static async Task<NonceLock> LockAsync(
            NonceManager nonceManager,
            string address,
            SemaphoreSlim sync,
            CancellationToken ct = default)
        {
            await sync.WaitAsync(ct);
            return new NonceLock(nonceManager, address, sync);
        }

        /// <summary>
        /// Gets the nonce for an address.
        /// </summary>
        public Task<Result<BigInteger>> GetNonceAsync(CancellationToken ct = default)
        {
            return _nonceManager.GetNonceAsync(_address, ct);
        }

        /// <summary>
        /// Resets the nonce for an address.
        /// </summary>
        public void Reset(BigInteger nonce)
        {
            _nonceManager.Reset(_address, nonce);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_isDisposed)
                return;

            if (disposing)
                _sync.Release();

            _isDisposed = true;
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }

    private readonly NonceManagerOptions _options;
    private readonly IRpcClient _rpc;
    private readonly ILogger<NonceManager>? _logger;
    private readonly object _startStopLock = new();
    private CancellationTokenSource? _cts;
    private bool _isRunning;

    private readonly Dictionary<string, object> _networkNonceSyncs;
    private readonly Dictionary<string, NonceEntry?> _networkNonces;

    private readonly Dictionary<string, SemaphoreSlim> _offlineNonceSyncs;
    private readonly Dictionary<string, BigInteger?> _offlineNonces;

    /// <summary>
    /// Initializes a new instance of the <see cref="NonceManager"/> class.
    /// </summary>
    /// <param name="options">The options for the nonce manager.</param>
    /// <param name="rpc">The RPC client.</param>
    /// <param name="logger">The logger.</param>
    public NonceManager(
        NonceManagerOptions options,
        IRpcClient rpc,
        ILogger<NonceManager>? logger = null)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _rpc = rpc ?? throw new ArgumentNullException(nameof(rpc));
        _logger = logger;

        _networkNonceSyncs = _options.Addresses.ToDictionary(
            a => a.ToLowerInvariant(),
            a => new object());

        _networkNonces = _options.Addresses.ToDictionary(
            a => a.ToLowerInvariant(),
            a => default(NonceEntry));

        _offlineNonceSyncs = _options.Addresses.ToDictionary(
            a => a.ToLowerInvariant(),
            a => new SemaphoreSlim(1));

        _offlineNonces = _options.Addresses.ToDictionary(
            a => a.ToLowerInvariant(),
            a => (BigInteger?)null);
    }

    /// <summary>
    /// Starts the nonce manager.
    /// </summary>
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
    /// Stops the nonce manager.
    /// </summary>
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
    /// Locks a nonce for an address.
    /// </summary>
    public Task<NonceLock> LockAsync(
        string address,
        CancellationToken ct = default)
    {
        return NonceLock.LockAsync(this, address, _offlineNonceSyncs[address.ToLowerInvariant()], ct);
    }

    /// <summary>
    /// Gets the last updated network nonce for an address.
    /// </summary>
    public (BigInteger? nonce, DateTimeOffset? timeStamp) GetNetworkNonce(string address)
    {
        var entry = GetNetworkNonceEntry(address.ToLowerInvariant());

        return (entry?.Nonce, entry?.TimeStamp);
    }

    private NonceEntry? GetNetworkNonceEntry(string address)
    {
        lock (_networkNonceSyncs[address])
        {
            var entry = _networkNonces[address];

            if (entry == null)
                return null;

            return new NonceEntry
            {
                Nonce = entry.Nonce,
                TimeStamp = entry.TimeStamp
            };
        }
    }

    private async Task<Result<BigInteger>> GetNonceAsync(string address, CancellationToken ct = default)
    {
        var timeStamp = DateTimeOffset.UtcNow;

        var networkNonceEntry = GetNetworkNonceEntry(address);

        if (networkNonceEntry == null ||
            timeStamp - networkNonceEntry.TimeStamp >= TimeSpan.FromMilliseconds(_options.NonceForceUpdateIntervalMs))
        {
            if (!await UpdateNonceAsync(address, ct))
                return new Error("Failed to get network nonce");

            networkNonceEntry = GetNetworkNonceEntry(address);

            if (networkNonceEntry == null)
                return new Error("Failed to get network nonce");
        }

        var currentNonce = networkNonceEntry.Nonce;

        if (_offlineNonces.TryGetValue(address, out var offlineNonce) &&
            offlineNonce != null &&
            offlineNonce > networkNonceEntry.Nonce)
        {
            currentNonce = offlineNonce.Value;
        }

        _offlineNonces[address] = currentNonce + 1;

        return currentNonce;
    }

    private void Reset(string address, BigInteger nonce)
    {
        _offlineNonces[address] = nonce;

        _logger?.LogDebug("Reset {address} nonce to {nonce}", address, nonce.ToString());
    }

    public async Task<bool> UpdateNonceAsync(string address, CancellationToken ct = default)
    {
        try
        {
            var (transactionCount, error) = await _rpc
                .GetTransactionCountAsync(
                    address,
                    _options.UsePending ? BlockNumber.Pending : BlockNumber.Latest,
                    ct)
                .ConfigureAwait(false);

            if (error != null)
            {
                _logger?.LogError(
                    "Error updating nonce for {Address}. Error: {@Error}",
                    address,
                    error);

                return false;
            }

            TrySetNetworkNonce(address.ToLowerInvariant(), transactionCount, DateTimeOffset.UtcNow);

            _logger?.LogInformation(
                "Network nonce for {Address} is {Nonce}",
                address,
                transactionCount.ToString());

            return true;
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception e)
        {
            _logger?.LogError(e, "Error updating nonce");
        }

        return false;
    }

    private bool TrySetNetworkNonce(string address, BigInteger nonce, DateTimeOffset timeStamp)
    {
        lock (_networkNonceSyncs[address])
        {
            var previousEntry = _networkNonces[address];

            if (previousEntry == null || previousEntry.Nonce <= nonce)
            {
                _networkNonces[address] = new NonceEntry
                {
                    Nonce = nonce,
                    TimeStamp = timeStamp
                };

                return true;
            }

            _logger?.LogWarning("Nonce for {Address} is lower than the previous network nonce. " +
                "Previous: {prev}. " +
                "Current: {curr}",
                address,
                previousEntry.Nonce.ToString(),
                nonce.ToString());

            // previousEntry.Nonce > nonce
            if (timeStamp - previousEntry.TimeStamp >= TimeSpan.FromMilliseconds(_options.NonceForceUpdateIntervalMs))
            {
                _logger?.LogWarning("Force updating nonce for {Address}", address);

                _networkNonces[address] = new NonceEntry
                {
                    Nonce = nonce,
                    TimeStamp = timeStamp
                };

                return true;
            }

            return false;
        }
    }

    private async Task DoWorkAsync(CancellationToken ct)
    {
        try
        {
            while (!ct.IsCancellationRequested)
            {
                if (_options.Addresses != null)
                {
                    foreach (var address in _options.Addresses)
                        await UpdateNonceAsync(address, ct);
                }

                await Task.Delay(_options.UpdateIntervalMs, ct);
            }
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            // expected
            _logger?.LogInformation("NonceManager stopped");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error updating nonce");
        }
    }
}
