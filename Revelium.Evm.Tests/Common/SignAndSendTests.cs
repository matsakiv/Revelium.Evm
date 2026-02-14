using Moq;
using Moq.Protected;
using Nethereum.Signer;
using Revelium.Evm.Crypto;
using Revelium.Evm.Rpc;
using Revelium.Evm.Transactions;
using System.Net;
using System.Text.Json;

namespace Revelium.Evm.Common;

public class SignAndSendTests
{
    private const string RPC_URL = "http://localhost:8000/";
    private const string ALICE_PRIVATE_KEY = "0x514575f3fb08a4aeee8ee51c332f766689cf1aedee163e33bb82088bfbcfb343";
    private const string BOB_PRIVATE_KEY = "0xb571f079ff5ae7a0aea7439e1667db7aaa0a70e324035414137b04a1769fed85";
    private const string EXPECTED_TX_ID = "0xabc123def456789012345678901234567890123456789012345678901234abcd";

    private readonly Mock<HttpMessageHandler> _handler;
    private readonly RpcClient _rpc;
    private readonly EthEcdsaSigner _signer;
    private readonly string _aliceAddress;
    private readonly string _bobAddress;

    public SignAndSendTests()
    {
        _handler = new Mock<HttpMessageHandler>();
        _rpc = new(RPC_URL, chainId: null, new HttpClient(_handler.Object));

        var aliceKey = new EthECKey(ALICE_PRIVATE_KEY);
        _signer = new EthEcdsaSigner(aliceKey);
        _aliceAddress = aliceKey.GetPublicAddress();
        _bobAddress = new EthECKey(BOB_PRIVATE_KEY).GetPublicAddress();
    }

    private TransactionLegacyRequest CreateLegacyTx(bool estimateGas = false, long? nonce = 1) => new()
    {
        From = _aliceAddress,
        To = _bobAddress,
        GasLimit = 21_000,
        GasPrice = 1_000_000_000,
        Nonce = nonce,
        Value = 1_000_000,
        EstimateGas = estimateGas
    };

    #region SignAndSendTransactionAsync

    [Fact]
    public async Task SignAndSend_Success_ReturnsTxId()
    {
        // Arrange
        SetupJsonRpcResponseResult(EXPECTED_TX_ID);
        var tx = CreateLegacyTx();

        // Act
        var (txId, error) = await _rpc.SignAndSendTransactionAsync(tx, _signer);

        // Assert
        Assert.Null(error);
        Assert.Equal(EXPECTED_TX_ID, txId);
        VerifyRequestSent(Times.Once());
    }

    [Fact]
    public async Task SignAndSend_SendError_ReturnsError()
    {
        // Arrange
        SetupJsonRpcError(code: -32000, message: "insufficient funds");
        var tx = CreateLegacyTx();

        // Act
        var (_, error) = await _rpc.SignAndSendTransactionAsync(tx, _signer);

        // Assert
        Assert.NotNull(error);
        Assert.Equal(-32000, error.Code);
    }

    [Fact]
    public async Task SignAndSend_NullNonceWithoutNonceManager_ReturnsError()
    {
        // Arrange
        var tx = CreateLegacyTx(nonce: null);

        // Act
        var (_, error) = await _rpc.SignAndSendTransactionAsync(tx, _signer);

        // Assert
        Assert.NotNull(error);
        Assert.Equal(IRpcClientExtensions.NONCE_IS_NULL, error.Code);
    }

    [Fact]
    public async Task SignAndSend_VerifyDisabled_SendsWithoutVerification()
    {
        // Arrange
        SetupJsonRpcResponseResult(EXPECTED_TX_ID);
        var tx = CreateLegacyTx();

        // Act
        var (txId, error) = await _rpc.SignAndSendTransactionAsync(
            tx, _signer, verifyTx: false);

        // Assert
        Assert.Null(error);
        Assert.Equal(EXPECTED_TX_ID, txId);
    }

    [Fact]
    public async Task SignAndSend_WithEstimateGas_EstimatesThenSends()
    {
        // Arrange: first call returns gas estimate, second returns txId
        SetupSequentialResponses(
            CreateJsonRpcResult("0x5208"),     // 21000 gas
            CreateJsonRpcResult(EXPECTED_TX_ID));
        var tx = CreateLegacyTx(estimateGas: true);

        // Act
        var (txId, error) = await _rpc.SignAndSendTransactionAsync(tx, _signer);

        // Assert
        Assert.Null(error);
        Assert.Equal(EXPECTED_TX_ID, txId);
        Assert.Equal(0x5208, tx.GasLimit);
        VerifyRequestSent(Times.Exactly(2));
    }

    [Fact]
    public async Task SignAndSend_EstimateGasFails_ReturnsError()
    {
        // Arrange
        SetupJsonRpcError(code: 3, message: "execution reverted");
        var tx = CreateLegacyTx(estimateGas: true);

        // Act
        var (_, error) = await _rpc.SignAndSendTransactionAsync(tx, _signer);

        // Assert
        Assert.NotNull(error);
    }

    [Fact]
    public async Task SignAndSend_NetworkError_ReturnsError()
    {
        // Arrange
        SetupNetworkError();
        var tx = CreateLegacyTx();

        // Act
        var (_, error) = await _rpc.SignAndSendTransactionAsync(tx, _signer);

        // Assert
        Assert.NotNull(error);
    }

    #endregion

    #region SignAndSendTransactionWithBlockAsync

    [Fact]
    public async Task SignAndSendWithBlock_Success_ReturnsTxIdAndBlock()
    {
        // Arrange: batch response with txId and block
        SetupJsonRpcResponse(
            "[" +
                $"{{\"jsonrpc\":\"2.0\",\"result\":\"{EXPECTED_TX_ID}\",\"id\":1}}," +
                "{\"jsonrpc\":\"2.0\",\"result\":{" +
                    "\"number\":\"0x670b90\"," +
                    "\"hash\":\"0x3ab00abc\"," +
                    "\"size\":\"0x279\"," +
                    "\"gasLimit\":\"0x4000000000000\"," +
                    "\"gasUsed\":\"0x45643c\"," +
                    "\"baseFeePerGas\":\"0x3b9aca00\"," +
                    "\"timestamp\":\"0x67bc82e4\"," +
                    "\"transactions\":[]" +
                "},\"id\":2}" +
            "]");
        var tx = CreateLegacyTx();

        // Act
        var (result, error) = await _rpc.SignAndSendTransactionWithBlockAsync(tx, _signer);

        // Assert
        Assert.Null(error);
        Assert.Equal(EXPECTED_TX_ID, result.TxId);
        Assert.NotNull(result.Block);
        Assert.Equal(6753168, result.Block.GetBlockNumber());
        Assert.Equal("0x3b9aca00", result.Block.BaseFeePerGas);
        VerifyRequestSent(Times.Once());
    }

    [Fact]
    public async Task SignAndSendWithBlock_SendErrorInBatch_ReturnsError()
    {
        // Arrange: batch where sendRawTransaction fails but getBlock succeeds
        SetupJsonRpcResponse(
            "[" +
                "{\"jsonrpc\":\"2.0\",\"error\":{\"code\":-32000,\"message\":\"insufficient funds\"},\"id\":1}," +
                "{\"jsonrpc\":\"2.0\",\"result\":{" +
                    "\"number\":\"0x670b90\"," +
                    "\"hash\":\"0x3ab00abc\"," +
                    "\"size\":\"0x279\"," +
                    "\"gasLimit\":\"0x4000000000000\"," +
                    "\"gasUsed\":\"0x45643c\"," +
                    "\"baseFeePerGas\":\"0x3b9aca00\"," +
                    "\"timestamp\":\"0x67bc82e4\"," +
                    "\"transactions\":[]" +
                "},\"id\":2}" +
            "]");
        var tx = CreateLegacyTx();

        // Act
        var (_, error) = await _rpc.SignAndSendTransactionWithBlockAsync(tx, _signer);

        // Assert
        Assert.NotNull(error);
        Assert.Equal(IRpcClientExtensions.TX_SEND_ERROR, error.Code);
    }

    [Fact]
    public async Task SignAndSendWithBlock_BlockErrorInBatch_ReturnsError()
    {
        // Arrange: batch where sendRawTransaction succeeds but getBlock fails
        SetupJsonRpcResponse(
            "[" +
                $"{{\"jsonrpc\":\"2.0\",\"result\":\"{EXPECTED_TX_ID}\",\"id\":1}}," +
                "{\"jsonrpc\":\"2.0\",\"error\":{\"code\":-32602,\"message\":\"invalid block\"},\"id\":2}" +
            "]");
        var tx = CreateLegacyTx();

        // Act
        var (_, error) = await _rpc.SignAndSendTransactionWithBlockAsync(tx, _signer);

        // Assert
        Assert.NotNull(error);
        Assert.Equal(IRpcClientExtensions.TX_SEND_ERROR, error.Code);
    }

    [Fact]
    public async Task SignAndSendWithBlock_NullNonce_ReturnsError()
    {
        // Arrange
        var tx = CreateLegacyTx(nonce: null);

        // Act
        var (_, error) = await _rpc.SignAndSendTransactionWithBlockAsync(tx, _signer);

        // Assert
        Assert.NotNull(error);
        Assert.Equal(IRpcClientExtensions.NONCE_IS_NULL, error.Code);
    }

    [Fact]
    public async Task SignAndSendWithBlock_NetworkError_ReturnsError()
    {
        // Arrange
        SetupNetworkError();
        var tx = CreateLegacyTx();

        // Act
        var (_, error) = await _rpc.SignAndSendTransactionWithBlockAsync(tx, _signer);

        // Assert
        Assert.NotNull(error);
    }

    #endregion

    #region Helpers

    private static string CreateJsonRpcResult(string result) =>
        JsonSerializer.Serialize(new { jsonrpc = "2.0", id = 1, result });

    private void SetupJsonRpcResponseResult(string result) =>
        SetupJsonRpcResponse(CreateJsonRpcResult(result));

    private void SetupJsonRpcError(int code, string message) =>
        SetupJsonRpcResponse(
            $"{{\"jsonrpc\":\"2.0\",\"id\":1,\"error\":{{\"code\":{code},\"message\":\"{message}\"}}}}");

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

    private void SetupSequentialResponses(params string[] jsonResponses)
    {
        var sequence = _handler
            .Protected()
            .SetupSequence<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>());

        foreach (var json in jsonResponses)
        {
            sequence.ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(json)
            });
        }
    }

    private void SetupNetworkError()
    {
        _handler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Network error"));
    }

    private void VerifyRequestSent(Times times)
    {
        _handler
            .Protected()
            .Verify(
                "SendAsync",
                times,
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Post &&
                    req.RequestUri!.ToString() == RPC_URL),
                ItExpr.IsAny<CancellationToken>());
    }

    #endregion
}
