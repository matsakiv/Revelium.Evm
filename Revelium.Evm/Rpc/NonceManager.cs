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
    /// <summary>
    /// Manages the nonce for an address.
    /// </summary>
    public class NonceManager
    {
        private class NonceEntry
        {
            public DateTimeOffset LastUpdated { get; set; }
            public BigInteger Nonce { get; set; }
        }

        /// <summary>
        /// A lock for the nonce manager.
        /// </summary>
        public class NonceLock : IDisposable
        {
            private readonly NonceManager _nonceManager;
            private bool _isDisposed;

            private NonceLock(NonceManager nonceManager)
            {
                _nonceManager = nonceManager;
            }

            /// <summary>
            /// Locks the nonce manager.
            /// </summary>
            public static async Task<NonceLock> LockAsync(
                NonceManager nonceManager,
                CancellationToken cancellationToken = default)
            {
                await nonceManager.Sync.WaitAsync(cancellationToken);
                return new NonceLock(nonceManager);
            }

            /// <summary>
            /// Gets the nonce for the address.
            /// </summary>
            /// <param name="rpc">The RPC client.</param>
            /// <param name="pending">Whether to use the pending block.</param>
            /// <param name="logger">The logger.</param>
            /// <param name="cancellationToken">The cancellation token.</param>
            /// <returns>The nonce for the address.</returns>
            public async Task<Result<BigInteger>> GetNonceAsync(
                RpcClient rpc,
                bool pending = true,
                ILogger? logger = null,
                CancellationToken cancellationToken = default)
            {
                return await _nonceManager.GetNonceAsync(rpc, pending, logger, cancellationToken);
            }

            /// <summary>
            /// Resets the nonce for the address.
            /// </summary>
            /// <param name="nonce">The nonce to reset to.</param>
            /// <param name="logger">The logger.</param>
            public void Reset(BigInteger nonce, ILogger? logger = null)
            {
                _nonceManager.Reset(nonce, logger);
            }

            private void Dispose(bool disposing)
            {
                if (_isDisposed)
                    return;

                if (disposing)
                    _nonceManager.Sync.Release();

                _isDisposed = true;
            }

            public void Dispose()
            {
                Dispose(disposing: true);
                GC.SuppressFinalize(this);
            }
        }

        private const int NONCE_FORCE_UPDATE_INTERVAL_SEC = 3 * 60;
        private const int OFFLINE_NONCE_VALID_PERIOD_SEC = 60;

        private NonceEntry? _offlineNonceEntry;
        private NonceEntry? _networkNonceEntry;
        private readonly SemaphoreSlim Sync;

        public string? NetworkId { get; }
        public string Address { get; }

        private NonceManager(string address, string? networkId = null)
        {
            Address = address;
            NetworkId = networkId;

            Sync = new SemaphoreSlim(initialCount: 1);
        }

        private static ConcurrentDictionary<string, NonceManager>? _instances;

        /// <summary>
        /// Gets or adds an instance of the nonce manager.
        /// </summary>
        /// <param name="address">The address.</param>
        /// <param name="networkId">The network ID.</param>
        /// <returns>The nonce manager.</returns>
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

        /// <summary>
        /// Locks the nonce manager.
        /// </summary>
        /// <param name="address">The address.</param>
        /// <param name="networkId">The network ID.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The nonce lock.</returns>
        public static Task<NonceLock> LockAsync(
            string address,
            string? networkId = null,
            CancellationToken cancellationToken = default)
        {
            var instance = GetOrAddInstance(address, networkId);
            return NonceLock.LockAsync(instance, cancellationToken);
        }

        /// <summary>
        /// Locks the nonce manager.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The nonce lock.</returns>
        public async Task<NonceLock> LockAsync(CancellationToken cancellationToken = default)
        {
            return await NonceLock.LockAsync(this, cancellationToken);
        }

        private async Task<Result<BigInteger>> GetNonceAsync(
            RpcClient rpc,
            bool pending = true,
            ILogger? logger = null,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var (transactionCount, error) = await rpc
                    .GetTransactionCountAsync(
                        Address,
                        pending ? BlockNumber.Pending : BlockNumber.Latest,
                        cancellationToken)
                    .ConfigureAwait(false);

                if (error != null)
                    return error;

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

                return currentNonce;
            }
            catch (Exception ex)
            {
                return new Error("GetNonceAsync error", ex);
            }
        }

        private void Reset(BigInteger nonce, ILogger? logger = null)
        {
            if (_offlineNonceEntry == null)
                return;

            _offlineNonceEntry.Nonce = nonce;
            logger?.LogDebug("Reset nonce to {@nonce}", nonce.ToString());
        }
    }
}
