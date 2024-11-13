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
    public class NonceManager
    {
        private class NonceEntry
        {
            public DateTimeOffset LastUpdated { get; set; }
            public BigInteger Nonce { get; set; }
        }

        private const int NONCE_FORCE_UPDATE_INTERVAL_SEC = 3 * 60;
        private const int NONCE_VALID_PERIOD_SEC = 60;

        private NonceEntry? _offlineNonceEntry;
        private NonceEntry? _networkNonceEntry;
        private readonly SemaphoreSlim _sync;

        public string NetworkId { get; }
        public string Address { get; }

        private NonceManager(string networkId, string address)
        {
            NetworkId = networkId;
            Address = address;

            _sync = new SemaphoreSlim(initialCount: 1);
        }

        private static ConcurrentDictionary<string, NonceManager>? _instances;

        public static NonceManager GetOrAddInstance(string networkId, string address)
        {
            var instances = _instances;

            if (instances == null)
            {
                Interlocked.CompareExchange(ref _instances, [], null);
                instances = _instances;
            }

            return instances.GetOrAdd($"{networkId}:{address}", id => new NonceManager(networkId, address));
        }

        public async Task<Result<BigInteger>> GetNonceAsync(
            RpcClient rpc,
            bool pending = true,
            ILogger? logger = null,
            CancellationToken cancellationToken = default)
        {
            var (transactionCount, error) = await rpc
                .GetTransactionCountAsync(
                    address: Address,
                    block: pending
                        ? BlockNumber.Pending
                        : BlockNumber.Latest,
                    cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            if (error != null)
                return error;

            var nonceTimeStamp = DateTimeOffset.UtcNow;
            var nonce = transactionCount;

            logger?.LogDebug("Nonce from network: {@nonce}", nonce.ToString());

            try
            {
                await _sync.WaitAsync(cancellationToken);

                if (_networkNonceEntry != null && _networkNonceEntry.Nonce > nonce)
                {
                    logger?.LogWarning(
                        "Network nonce: {@nonce} less than last network nonce: {@lastNonce}",
                        nonce.ToString(),
                        _networkNonceEntry.Nonce.ToString());
                }

                if (_networkNonceEntry == null ||
                    _networkNonceEntry.Nonce != nonce ||
                    nonceTimeStamp - _networkNonceEntry.LastUpdated >= TimeSpan.FromSeconds(NONCE_FORCE_UPDATE_INTERVAL_SEC))
                {
                    _networkNonceEntry = new NonceEntry
                    {
                        Nonce = nonce,
                        LastUpdated = nonceTimeStamp
                    };
                }

                var currentNonce = _offlineNonceEntry != null &&
                    _offlineNonceEntry.Nonce > _networkNonceEntry.Nonce &&
                    _offlineNonceEntry.LastUpdated - _networkNonceEntry.LastUpdated < TimeSpan.FromSeconds(NONCE_VALID_PERIOD_SEC)
                        ? _offlineNonceEntry.Nonce
                        : nonce;

                if (_offlineNonceEntry != null &&
                    _offlineNonceEntry.LastUpdated - _networkNonceEntry.LastUpdated >= TimeSpan.FromSeconds(NONCE_VALID_PERIOD_SEC))
                {
                    logger?.LogWarning(
                        "Network nonce lags behind offline nonce by more than {@sec} seconds",
                        (_offlineNonceEntry.LastUpdated - _networkNonceEntry.LastUpdated).TotalSeconds);
                }

                _offlineNonceEntry = new NonceEntry
                {
                    Nonce = currentNonce + 1,
                    LastUpdated = DateTimeOffset.UtcNow
                };

                return currentNonce;
            }
            finally
            {
                _sync.Release();
            }
        }

        public async Task<Result<bool>> ForceResetAsync(
            RpcClient rpc,
            bool pending = true,
            ILogger? logger = null,
            CancellationToken cancellationToken = default)
        {
            var (transactionCount, error) = await rpc
                .GetTransactionCountAsync(
                    address: Address,
                    block: pending
                        ? BlockNumber.Pending
                        : BlockNumber.Latest,
                    cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            if (error != null)
                return error;

            var nonceTimeStamp = DateTimeOffset.UtcNow;
            var nonce = transactionCount;

            logger?.LogDebug("Nonce from network: {@nonce}", nonce.ToString());

            try
            {
                await _sync.WaitAsync(cancellationToken);

                _networkNonceEntry = new NonceEntry
                {
                    Nonce = nonce,
                    LastUpdated = nonceTimeStamp
                };

                _offlineNonceEntry = new NonceEntry
                {
                    Nonce = nonce,
                    LastUpdated = nonceTimeStamp
                };

                return true;
            }
            finally
            {
                _sync.Release();
            }
        }
    }
}
