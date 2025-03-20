using Incendium;
using Moq;
using Moq.Protected;
using Revelium.Evm.Rpc;
using Revelium.Evm.Services;
using System.Net;
using System.Numerics;
using System.Text.Json;

namespace Revelium.Evm.Nonce
{
    public class NonceManagerTests
    {
        private const string RPC_URL = "http://localhost:8000/";
        private const string ADDRESS = "0xd8dA6BF26964aF9D7eEd9e03E53415D37aA96045";

        private readonly Mock<HttpMessageHandler> _handler;
        private readonly RpcClient _rpc;
        private readonly NonceManagerOptions _options;
        private readonly NonceManager _nonceManager;

        public NonceManagerTests()
        {
            _handler = new Mock<HttpMessageHandler>();
            _rpc = new(RPC_URL, chainId: null, new HttpClient(_handler.Object));
            _options = new NonceManagerOptions
            {
                Addresses = [ADDRESS],
                NonceForceUpdateIntervalMs = 100,
                OfflineNonceForceResetIntervalMs = 200
            };
            _nonceManager = new NonceManager(_options, _rpc);
        }

        [Fact]
        public async Task Test_NonceManager_GetNonce()
        {
            BigInteger nonce;
            Error? error;

            SetupJsonRpcResponseResult("0x5");

            using (var nonceLock = await _nonceManager.LockAsync(ADDRESS))
                (nonce, error) = await nonceLock.GetNonceAsync();

            Assert.Null(error);
            Assert.Equal(5, nonce);
        }

        [Fact]
        public async Task Test_NonceManager_OfflineNonces()
        {
            BigInteger nonce;
            Error? error;

            SetupJsonRpcResponseResult("0x5");

            using (var nonceLock = await _nonceManager.LockAsync(ADDRESS))
                (nonce, error) = await nonceLock.GetNonceAsync();

            Assert.Null(error);
            Assert.Equal(5, nonce);

            using (var nonceLock = await _nonceManager.LockAsync(ADDRESS))
                (nonce, error) = await nonceLock.GetNonceAsync();

            Assert.Null(error);
            Assert.Equal(6, nonce);

            using (var nonceLock = await _nonceManager.LockAsync(ADDRESS))
                (nonce, error) = await nonceLock.GetNonceAsync();

            Assert.Null(error);
            Assert.Equal(7, nonce);
        }

        [Fact]
        public async Task Test_NonceManager_NonceForceUpdate()
        {
            BigInteger? nonce;
            bool isUpdated;

            SetupJsonRpcResponseResult("0x7");
            isUpdated = await _nonceManager.UpdateNonceAsync(ADDRESS);

            (nonce, _) = _nonceManager.GetNetworkNonce(ADDRESS);
            Assert.Equal(7, nonce);
            Assert.True(isUpdated);

            SetupJsonRpcResponseResult("0x6");
            isUpdated = await _nonceManager.UpdateNonceAsync(ADDRESS);

            (nonce, _) = _nonceManager.GetNetworkNonce(ADDRESS);
            Assert.Equal(7, nonce);
            Assert.True(isUpdated);

            await Task.Delay(_options.NonceForceUpdateIntervalMs);

            SetupJsonRpcResponseResult("0x6");
            isUpdated = await _nonceManager.UpdateNonceAsync(ADDRESS);

            (nonce, _) = _nonceManager.GetNetworkNonce(ADDRESS);
            Assert.Equal(6, nonce);
            Assert.True(isUpdated);
        }

        [Fact]
        public async Task Test_NonceManager_OfflineNonceReset()
        {
            BigInteger nonce;
            Error? error;
            bool isUpdated;

            SetupJsonRpcResponseResult("0x7");
            isUpdated = await _nonceManager.UpdateNonceAsync(ADDRESS);

            using (var nonceLock = await _nonceManager.LockAsync(ADDRESS))
            {
                (nonce, error) = await nonceLock.GetNonceAsync();
            
                nonceLock.Reset(nonce);
            }

            Assert.Null(error);
            Assert.Equal(7, nonce);

            using (var nonceLock = await _nonceManager.LockAsync(ADDRESS))
                (nonce, error) = await nonceLock.GetNonceAsync();

            Assert.Null(error);
            Assert.Equal(7, nonce);
        }

        [Fact]
        public async Task Test_NonceManager_OfflineNonceForceReset()
        {
            BigInteger nonce;
            Error? error;
            bool isUpdated;

            SetupJsonRpcResponseResult("0x7");
            isUpdated = await _nonceManager.UpdateNonceAsync(ADDRESS);

            using (var nonceLock = await _nonceManager.LockAsync(ADDRESS))
                (nonce, error) = await nonceLock.GetNonceAsync();

            Assert.Null(error);
            Assert.Equal(7, nonce);
            Assert.True(isUpdated);

            SetupJsonRpcResponseResult("0x6");
            isUpdated = await _nonceManager.UpdateNonceAsync(ADDRESS);

            using (var nonceLock = await _nonceManager.LockAsync(ADDRESS))
                (nonce, error) = await nonceLock.GetNonceAsync();

            Assert.Null(error);
            Assert.Equal(8, nonce);
            Assert.True(isUpdated);

            await Task.Delay(_options.NonceForceUpdateIntervalMs);

            SetupJsonRpcResponseResult("0x6");
            isUpdated = await _nonceManager.UpdateNonceAsync(ADDRESS);

            using (var nonceLock = await _nonceManager.LockAsync(ADDRESS))
                (nonce, error) = await nonceLock.GetNonceAsync();

            Assert.Null(error);
            Assert.Equal(9, nonce);
            Assert.True(isUpdated);

            await Task.Delay(_options.OfflineNonceForceResetIntervalMs);

            SetupJsonRpcResponseResult("0x6");
            using (var nonceLock = await _nonceManager.LockAsync(ADDRESS))
                (nonce, error) = await nonceLock.GetNonceAsync();

            Assert.Null(error);
            Assert.Equal(6, nonce);
        }

        [Fact]
        public async Task Test_NonceManager_ParallelNonceUpdates()
        {
            SetupJsonRpcResponseResult("0x1");
            await _nonceManager.UpdateNonceAsync(ADDRESS);

            var nonces = new List<BigInteger>();
            var tasks = new List<Task>();

            for (int i = 0; i < 5; i++)
            {
                tasks.Add(Task.Run(async () =>
                {
                    using var nonceLock = await _nonceManager.LockAsync(ADDRESS);
                    var (nonce, error) = await nonceLock.GetNonceAsync();
                    nonces.Add(nonce);
                }));
            }

            await Task.WhenAll(tasks);

            Assert.Equal(5, nonces.Count);
            Assert.Equal([1, 2, 3, 4, 5], nonces);
        }

        private void SetupJsonRpcResponseResult<T>(T result)
        {
            var response = new { jsonrpc = "2.0", id = 1, result };
            var json = JsonSerializer.Serialize(response);

            SetupJsonRpcResponse(json);
        }

        private void SetupJsonRpcResponse(string json)
        {
            _handler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(json)
                });
        }
    }
}
