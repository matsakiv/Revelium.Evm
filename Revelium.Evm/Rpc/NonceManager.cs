using Incendium;
using Microsoft.Extensions.Logging;
using Revelium.Evm.Rpc.Parameters;
using System;
using System.Collections.Concurrent;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;

namespace Revelium.Evm.Rpc
{
    public class NonceLock(BigInteger nonce, SemaphoreSlim sync) : IDisposable
    {
        private readonly SemaphoreSlim _sync = sync;
        private bool _isDisposed;

        public BigInteger Nonce { get; } = nonce;

        private void Dispose(bool disposing)
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

    public class NonceManager
    {
        private class NonceEntry
        {
            public DateTimeOffset LastUpdated { get; set; }
            public BigInteger Nonce { get; set; }
        }

        private const int NONCE_FORCE_UPDATE_INTERVAL_SEC = 3 * 60;
        private const int OFFLINE_NONCE_VALID_PERIOD_SEC = 60;

        private NonceEntry? _offlineNonceEntry;
        private NonceEntry? _networkNonceEntry;
        private readonly SemaphoreSlim _sync;

        public string? NetworkId { get; }
        public string Address { get; }

        private NonceManager(string address, string? networkId = null)
        {
            Address = address;
            NetworkId = networkId;

            _sync = new SemaphoreSlim(initialCount: 1);
        }

        private static ConcurrentDictionary<string, NonceManager>? _instances;

        public static NonceManager GetOrAddInstance(string address, string? networkId = null)
        {
            var instances = _instances;

            if (instances == null)
            {
                Interlocked.CompareExchange(ref _instances, [], null);
                instances = _instances;
            }

            return instances.GetOrAdd($"{networkId ?? ""}:{address}", id => new NonceManager(address, networkId));
        }

        public async Task<Result<NonceLock>> GetNonceAsync(
            RpcClient rpc,
            bool pending = true,
            ILogger? logger = null,
            CancellationToken cancellationToken = default)
        {
            try
            {
                await _sync.WaitAsync(cancellationToken);

                var (transactionCount, error) = await rpc
                    .GetTransactionCountAsync(
                        Address,
                        pending ? BlockNumber.Pending : BlockNumber.Latest,
                        cancellationToken)
                    .ConfigureAwait(false);

                if (error != null)
                {
                    _sync.Release();
                    return error;
                }

                var currentTimeStamp = DateTimeOffset.UtcNow;
                var nonce = transactionCount;

                logger?.LogDebug("Network nonce is {@nonce}", nonce.ToString());

                if (_networkNonceEntry != null && _networkNonceEntry.Nonce > nonce)
                {
                    logger?.LogWarning(
                        "Current network nonce {@nonce} is less than previously recorded network nonce {@lastNonce}",
                        nonce.ToString(),
                        _networkNonceEntry.Nonce.ToString());
                }

                if (_networkNonceEntry == null ||
                    _networkNonceEntry.Nonce != nonce ||
                    (currentTimeStamp - _networkNonceEntry.LastUpdated >=
                    TimeSpan.FromSeconds(NONCE_FORCE_UPDATE_INTERVAL_SEC)))
                {
                    _networkNonceEntry = new NonceEntry
                    {
                        Nonce = nonce,
                        LastUpdated = currentTimeStamp
                    };
                }

                var currentNonce = _offlineNonceEntry != null &&
                    _offlineNonceEntry.Nonce > _networkNonceEntry.Nonce &&
                    (_offlineNonceEntry.LastUpdated - _networkNonceEntry.LastUpdated <
                    TimeSpan.FromSeconds(OFFLINE_NONCE_VALID_PERIOD_SEC))
                        ? _offlineNonceEntry.Nonce
                        : nonce;

                if (_offlineNonceEntry != null &&
                    (_offlineNonceEntry.LastUpdated - _networkNonceEntry.LastUpdated >=
                    TimeSpan.FromSeconds(OFFLINE_NONCE_VALID_PERIOD_SEC)))
                {
                    logger?.LogWarning(
                        "Network nonce lags behind offline nonce by more than {@seconds} seconds",
                        (_offlineNonceEntry.LastUpdated - _networkNonceEntry.LastUpdated).TotalSeconds);
                }

                _offlineNonceEntry = new NonceEntry
                {
                    Nonce = currentNonce + 1,
                    LastUpdated = DateTimeOffset.UtcNow
                };

                return new NonceLock(currentNonce, _sync);
            }
            catch (Exception ex)
            {
                _sync.Release();
                return new Error("GetNonceAsync error", ex);
            }
        }

        public void Reset(BigInteger nonce, ILogger? logger = null)
        {
            if (_offlineNonceEntry != null)
            {
                logger?.LogDebug("Reset nonce to {@nonce}", nonce.ToString());

                _offlineNonceEntry.Nonce = nonce;
            }
        }
    }
}
