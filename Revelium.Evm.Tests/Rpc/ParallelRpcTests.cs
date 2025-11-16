using Moq;
using Moq.Protected;
using System.Net;
using System.Text.Json;

namespace Revelium.Evm.Rpc;

public class ParallelRpcTests
{
    private const string RPC_URL_1 = "http://localhost:8001/";
    private const string RPC_URL_2 = "http://localhost:8002/";
    private const string RPC_URL_3 = "http://localhost:8003/";
    private const string BROADCAST_URL_1 = "http://localhost:9001/";
    private const string BROADCAST_URL_2 = "http://localhost:9002/";
    private const string ADDRESS = "0xd8dA6BF26964aF9D7eEd9e03E53415D37aA96045";

    private static RpcClient CreateRpcClient(string url, Mock<HttpMessageHandler> handler)
    {
        return new RpcClient(url, chainId: null, new HttpClient(handler.Object));
    }

    private static void SetupJsonRpcResponseResult<T>(Mock<HttpMessageHandler> handler, string url, T result)
    {
        var response = new { jsonrpc = "2.0", id = 1, result };
        var json = JsonSerializer.Serialize(response);

        SetupJsonRpcResponse(handler, url, json);
    }

    private static void SetupJsonRpcResponse(Mock<HttpMessageHandler> handler, string url, string json)
    {
        handler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => req.RequestUri!.ToString() == url),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(json)
            });
    }

    private static void SetupNetworkError(Mock<HttpMessageHandler> handler, string url)
    {
        handler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => req.RequestUri!.ToString() == url),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Network error"));
    }

    private static void SetupRpcError(Mock<HttpMessageHandler> handler, string url, int errorCode, string errorMessage)
    {
        var response = new
        {
            jsonrpc = "2.0",
            id = 1,
            error = new { code = errorCode, message = errorMessage }
        };
        var json = JsonSerializer.Serialize(response);

        SetupJsonRpcResponse(handler, url, json);
    }

    [Fact]
    public async Task Test_ParallelRpcClient_GetBalance_FirstClientSucceeds()
    {
        // Arrange
        var handler1 = new Mock<HttpMessageHandler>();
        var handler2 = new Mock<HttpMessageHandler>();
        SetupJsonRpcResponseResult(handler1, RPC_URL_1, "0xb9052a11d665600");
        SetupJsonRpcResponseResult(handler2, RPC_URL_2, "0x0"); // This should not be called

        var client1 = CreateRpcClient(RPC_URL_1, handler1);
        var client2 = CreateRpcClient(RPC_URL_2, handler2);
        var parallelClient = new ParallelRpcClient([client1, client2], []);

        // Act
        var (balance, error) = await parallelClient.GetBalanceAsync(ADDRESS);

        // Assert
        Assert.Null(error);
        Assert.Equal(833256783000000000, balance);
    }

    [Fact]
    public async Task Test_ParallelRpcClient_GetBalance_FirstClientFails_SecondSucceeds()
    {
        // Arrange
        var handler1 = new Mock<HttpMessageHandler>();
        var handler2 = new Mock<HttpMessageHandler>();
        SetupNetworkError(handler1, RPC_URL_1);
        SetupJsonRpcResponseResult(handler2, RPC_URL_2, "0xb9052a11d665600");

        var client1 = CreateRpcClient(RPC_URL_1, handler1);
        var client2 = CreateRpcClient(RPC_URL_2, handler2);
        var parallelClient = new ParallelRpcClient([client1, client2], []);

        // Act
        var (balance, error) = await parallelClient.GetBalanceAsync(ADDRESS);

        // Assert
        Assert.Null(error);
        Assert.Equal(833256783000000000, balance);
    }

    [Fact]
    public async Task Test_ParallelRpcClient_GetBalance_AllClientsFail()
    {
        // Arrange
        var handler1 = new Mock<HttpMessageHandler>();
        var handler2 = new Mock<HttpMessageHandler>();
        SetupNetworkError(handler1, RPC_URL_1);
        SetupNetworkError(handler2, RPC_URL_2);

        var client1 = CreateRpcClient(RPC_URL_1, handler1);
        var client2 = CreateRpcClient(RPC_URL_2, handler2);
        var parallelClient = new ParallelRpcClient([client1, client2], []);

        // Act
        var (_, error) = await parallelClient.GetBalanceAsync(ADDRESS);

        // Assert
        Assert.NotNull(error);
        Assert.Equal(RpcClient.HTTP_REQUEST_ERROR, error.Code);
    }

    [Fact]
    public async Task Test_ParallelRpcClient_GetBalance_FirstRpcError_SecondSucceeds()
    {
        // Arrange
        var handler1 = new Mock<HttpMessageHandler>();
        var handler2 = new Mock<HttpMessageHandler>();
        SetupRpcError(handler1, RPC_URL_1, -32602, "Invalid params");
        SetupJsonRpcResponseResult(handler2, RPC_URL_2, "0xb9052a11d665600");

        var client1 = CreateRpcClient(RPC_URL_1, handler1);
        var client2 = CreateRpcClient(RPC_URL_2, handler2);
        var parallelClient = new ParallelRpcClient([client1, client2], []);

        // Act
        var (balance, error) = await parallelClient.GetBalanceAsync(ADDRESS);

        // Assert
        Assert.Null(error);
        Assert.Equal(833256783000000000, balance);
    }

    [Fact]
    public async Task Test_ParallelRpcClient_GetBlockNumber()
    {
        // Arrange
        var handler1 = new Mock<HttpMessageHandler>();
        var handler2 = new Mock<HttpMessageHandler>();
        SetupJsonRpcResponseResult(handler1, RPC_URL_1, "0x670b90");

        var client1 = CreateRpcClient(RPC_URL_1, handler1);
        var client2 = CreateRpcClient(RPC_URL_2, handler2);
        var parallelClient = new ParallelRpcClient([client1, client2], []);

        // Act
        var (blockNumber, error) = await parallelClient.GetBlockNumberAsync();

        // Assert
        Assert.Null(error);
        Assert.Equal(6753168, blockNumber);
    }

    [Fact]
    public async Task Test_ParallelRpcClient_GetGasPrice()
    {
        // Arrange
        var handler1 = new Mock<HttpMessageHandler>();
        SetupJsonRpcResponseResult(handler1, RPC_URL_1, "0x3b9aca00");

        var client1 = CreateRpcClient(RPC_URL_1, handler1);
        var client2 = CreateRpcClient(RPC_URL_2, new Mock<HttpMessageHandler>());
        var parallelClient = new ParallelRpcClient([client1, client2], []);

        // Act
        var (gasPrice, error) = await parallelClient.GetGasPriceAsync();

        // Assert
        Assert.Null(error);
        Assert.Equal(1000000000, gasPrice);
    }

    [Fact]
    public async Task Test_ParallelRpcClient_GetTransactionCount()
    {
        // Arrange
        var handler1 = new Mock<HttpMessageHandler>();
        SetupJsonRpcResponseResult(handler1, RPC_URL_1, "0x2");

        var client1 = CreateRpcClient(RPC_URL_1, handler1);
        var client2 = CreateRpcClient(RPC_URL_2, new Mock<HttpMessageHandler>());
        var parallelClient = new ParallelRpcClient([client1, client2], []);

        // Act
        var (count, error) = await parallelClient.GetTransactionCountAsync(ADDRESS);

        // Assert
        Assert.Null(error);
        Assert.Equal(0x2, count);
    }

    [Fact]
    public async Task Test_ParallelRpcClient_GetMaxPriorityFeePerGas()
    {
        // Arrange
        var handler1 = new Mock<HttpMessageHandler>();
        SetupJsonRpcResponseResult(handler1, RPC_URL_1, "0x0");

        var client1 = CreateRpcClient(RPC_URL_1, handler1);
        var client2 = CreateRpcClient(RPC_URL_2, new Mock<HttpMessageHandler>());
        var parallelClient = new ParallelRpcClient([client1, client2], []);

        // Act
        var (maxPriorityFeePerGas, error) = await parallelClient.GetMaxPriorityFeePerGasAsync();

        // Assert
        Assert.Null(error);
        Assert.Equal(0, maxPriorityFeePerGas);
    }

    [Fact]
    public async Task Test_ParallelRpcClient_EstimateGas()
    {
        // Arrange
        var handler1 = new Mock<HttpMessageHandler>();
        SetupJsonRpcResponseResult(handler1, RPC_URL_1, "0xdc5ef");

        var client1 = CreateRpcClient(RPC_URL_1, handler1);
        var client2 = CreateRpcClient(RPC_URL_2, new Mock<HttpMessageHandler>());
        var parallelClient = new ParallelRpcClient([client1, client2], []);

        // Act
        var (gas, error) = await parallelClient.EstimateGasAsync(
            to: "0xE67B7D039b78DE25367EF5E69596075Bbd852BA9",
            from: ADDRESS,
            data: "0xfaf29cb3" +
                   "000000000000000000000000d8dA6BF26964aF9D7eEd9e03E53415D37aA96045" +
                   "0000000000000000000000000000000000000000000000000000000000000000",
            maxFeePerGas: 1000000000,
            maxPriorityFeePerGas: 0);

        // Assert
        Assert.Null(error);
        Assert.Equal(0xdc5ef, gas);
    }

    [Fact]
    public async Task Test_ParallelRpcClient_Call()
    {
        // Arrange
        var handler1 = new Mock<HttpMessageHandler>();
        SetupJsonRpcResponseResult(handler1, RPC_URL_1, "0x0000000000000000000000000000000000000000000000000000000004a27c59fffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff8");

        var client1 = CreateRpcClient(RPC_URL_1, handler1);
        var client2 = CreateRpcClient(RPC_URL_2, new Mock<HttpMessageHandler>());
        var parallelClient = new ParallelRpcClient([client1, client2], []);

        // Act
        var (result, error) = await parallelClient.CallAsync<string>(
            to: "0xEfE04e62499100AcA732Db6A75868290F738928B",
            from: ADDRESS,
            input: "0x2ea68d850000000000000000000000000000000000000000000000000000000000000000");

        // Assert
        Assert.Null(error);
        Assert.Equal("0x0000000000000000000000000000000000000000000000000000000004a27c59fffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff8", result);
    }

    [Fact]
    public async Task Test_ParallelRpcClient_GetTransactionReceipt()
    {
        // Arrange
        var handler1 = new Mock<HttpMessageHandler>();
        SetupJsonRpcResponse(handler1, RPC_URL_1,
        "{" +
            "\"jsonrpc\":\"2.0\"," +
            "\"result\":{" +
                "\"transactionHash\":\"0x0766e94a24cf7c7e32f2dddb2d2e4efba6ee84eda7c44ea49acf60a10528dc0b\"," +
                "\"transactionIndex\":\"0x0\"," +
                "\"blockHash\":\"0x3ab00abc2edbd0ce7b1c3130a5c40b5f6b87285644a9743d8ec91f955ac62297\"," +
                "\"blockNumber\":\"0x670b90\"," +
                "\"from\":\"0x5d66ec78664f4a0b0929a41270316a6cd4d8bd4b\"," +
                "\"to\":\"0xa2cca359c43839040cf3d230deb1689ab8db2dac\"," +
                "\"cumulativeGasUsed\":\"0x45643c\"," +
                "\"effectiveGasPrice\":\"0x3b9aca00\"," +
                "\"gasUsed\":\"0x45643c\"," +
                "\"logs\":[]," +
                "\"logsBloom\":\"0x00000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000\"," +
                "\"type\":\"0x0\"," +
                "\"status\":\"0x1\"," +
                "\"contractAddress\":null" +
            "}," +
            "\"id\":1" +
        "}");

        var client1 = CreateRpcClient(RPC_URL_1, handler1);
        var client2 = CreateRpcClient(RPC_URL_2, new Mock<HttpMessageHandler>());
        var parallelClient = new ParallelRpcClient([client1, client2], []);

        // Act
        var (receipt, error) = await parallelClient.GetTransactionReceiptAsync(
            "0x0766e94a24cf7c7e32f2dddb2d2e4efba6ee84eda7c44ea49acf60a10528dc0b");

        // Assert
        Assert.Null(error);
        Assert.NotNull(receipt);
        Assert.Equal("0x1", receipt.Status);
        Assert.Equal("0x0766e94a24cf7c7e32f2dddb2d2e4efba6ee84eda7c44ea49acf60a10528dc0b", receipt.TransactionHash);
    }

    [Fact]
    public async Task Test_ParallelRpcClient_SendRawTransaction_UsesBroadcastClients()
    {
        // Arrange
        var handler1 = new Mock<HttpMessageHandler>();
        var handler2 = new Mock<HttpMessageHandler>();
        var broadcastHandler1 = new Mock<HttpMessageHandler>();
        SetupJsonRpcResponseResult(broadcastHandler1, BROADCAST_URL_1, "0x1234567890abcdef");

        var client1 = CreateRpcClient(RPC_URL_1, handler1);
        var client2 = CreateRpcClient(RPC_URL_2, handler2);
        var broadcastClient1 = CreateRpcClient(BROADCAST_URL_1, broadcastHandler1);
        var parallelClient = new ParallelRpcClient([client1, client2], [broadcastClient1]);

        // Act
        var (txHash, error) = await parallelClient.SendRawTransactionAsync("0xabcdef123456");

        // Assert
        Assert.Null(error);
        Assert.Equal("0x1234567890abcdef", txHash);
    }

    [Fact]
    public async Task Test_ParallelRpcClient_SendRawTransaction_BroadcastFails_FallbackToRegular()
    {
        // Arrange
        var handler1 = new Mock<HttpMessageHandler>();
        var broadcastHandler1 = new Mock<HttpMessageHandler>();
        SetupNetworkError(broadcastHandler1, BROADCAST_URL_1);
        SetupJsonRpcResponseResult(handler1, RPC_URL_1, "0x1234567890abcdef");

        var client1 = CreateRpcClient(RPC_URL_1, handler1);
        var broadcastClient1 = CreateRpcClient(BROADCAST_URL_1, broadcastHandler1);
        var parallelClient = new ParallelRpcClient([client1], [broadcastClient1]);

        // Act
        var (txHash, error) = await parallelClient.SendRawTransactionAsync("0xabcdef123456");

        // Assert
        Assert.Null(error);
        Assert.Equal("0x1234567890abcdef", txHash);
    }

    [Fact]
    public async Task Test_ParallelRpcClient_SendRawTransaction_NoBroadcastClients_UsesRegular()
    {
        // Arrange
        var handler1 = new Mock<HttpMessageHandler>();
        SetupJsonRpcResponseResult(handler1, RPC_URL_1, "0x1234567890abcdef");

        var client1 = CreateRpcClient(RPC_URL_1, handler1);
        var parallelClient = new ParallelRpcClient([client1], []);

        // Act
        var (txHash, error) = await parallelClient.SendRawTransactionAsync("0xabcdef123456");

        // Assert
        Assert.Null(error);
        Assert.Equal("0x1234567890abcdef", txHash);
    }

    [Fact]
    public async Task Test_ParallelRpcClient_SendRawTransaction_MultipleBroadcastClients_FirstSucceeds()
    {
        // Arrange
        var broadcastHandler1 = new Mock<HttpMessageHandler>();
        var broadcastHandler2 = new Mock<HttpMessageHandler>();
        SetupJsonRpcResponseResult(broadcastHandler1, BROADCAST_URL_1, "0x1111111111111111");
        // Second broadcast client should not be called if first succeeds

        var broadcastClient1 = CreateRpcClient(BROADCAST_URL_1, broadcastHandler1);
        var broadcastClient2 = CreateRpcClient(BROADCAST_URL_2, broadcastHandler2);
        var parallelClient = new ParallelRpcClient([], [broadcastClient1, broadcastClient2]);

        // Act
        var (txHash, error) = await parallelClient.SendRawTransactionAsync("0xabcdef123456");

        // Assert
        Assert.Null(error);
        Assert.Equal("0x1111111111111111", txHash);
    }

    [Fact]
    public void Test_ParallelRpcClient_ChainId()
    {
        // Arrange
        var handler1 = new Mock<HttpMessageHandler>();
        var client1 = new RpcClient(RPC_URL_1, chainId: 1, new HttpClient(handler1.Object));
        var client2 = new RpcClient(RPC_URL_2, chainId: 1, new HttpClient(new Mock<HttpMessageHandler>().Object));
        var parallelClient = new ParallelRpcClient([client1, client2], []);

        // Act
        var chainId = parallelClient.ChainId;

        // Assert
        Assert.Equal(1, chainId);
    }

    [Fact]
    public async Task Test_ParallelRpcClient_GetLogs()
    {
        // Arrange
        var handler1 = new Mock<HttpMessageHandler>();
        SetupJsonRpcResponse(handler1, RPC_URL_1,
        "{" +
            "\"jsonrpc\":\"2.0\"," +
            "\"result\":[" +
                "{" +
                    "\"address\":\"0x7f89bf25a530bb82cf51be8d0b14b5239df17f1c\"," +
                    "\"topics\":[" +
                        "\"0xddf252ad1be2c89b69c2b068fc378daa952ba7f163c4a11628f55a4df523b3ef\"," +
                        "\"0x000000000000000000000000ffff100b0017aced8e01b8eb0454fe09c43364b9\"," +
                        "\"0x000000000000000000000000d8da6bf26964af9d7eed9e03e53415d37aa96045\"" +
                    "]," +
                    "\"data\":\"0x00000000000000000000000000000000000000000000152d02c7e14af6800000\"," +
                    "\"blockNumber\":\"0x95e6\"," +
                    "\"transactionHash\":\"0x87b6f0f6708940659fb7beda7a198d14dd836ffec9897353dff9e4c38f28fb9b\"," +
                    "\"transactionIndex\":\"0x0\"," +
                    "\"blockHash\":\"0xd3fc687407fff32cea69ebf053d667f98d188a7c1b1c5f9b95990811c489e040\"," +
                    "\"logIndex\":\"0x0\"," +
                    "\"removed\":false" +
               "}" +
            "]," +
            "\"id\":1" +
        "}");

        var client1 = CreateRpcClient(RPC_URL_1, handler1);
        var client2 = CreateRpcClient(RPC_URL_2, new Mock<HttpMessageHandler>());
        var parallelClient = new ParallelRpcClient([client1, client2], []);

        // Act
        var (logs, error) = await parallelClient.GetLogsAsync();

        // Assert
        Assert.Null(error);
        Assert.NotEmpty(logs);
        Assert.Equal("0x7f89bf25a530bb82cf51be8d0b14b5239df17f1c", logs[0].Address);
    }

    [Fact]
    public async Task Test_ParallelRpcClient_ThreeClients_SecondSucceeds()
    {
        // Arrange
        var handler1 = new Mock<HttpMessageHandler>();
        var handler2 = new Mock<HttpMessageHandler>();
        var handler3 = new Mock<HttpMessageHandler>();
        SetupNetworkError(handler1, RPC_URL_1);
        SetupJsonRpcResponseResult(handler2, RPC_URL_2, "0xb9052a11d665600");
        SetupNetworkError(handler3, RPC_URL_3);

        var client1 = CreateRpcClient(RPC_URL_1, handler1);
        var client2 = CreateRpcClient(RPC_URL_2, handler2);
        var client3 = CreateRpcClient(RPC_URL_3, handler3);
        var parallelClient = new ParallelRpcClient([client1, client2, client3], []);

        // Act
        var (balance, error) = await parallelClient.GetBalanceAsync(ADDRESS);

        // Assert
        Assert.Null(error);
        Assert.Equal(833256783000000000, balance);
    }

    [Fact]
    public async Task Test_ParallelRpcClient_EmptyClients_ThrowsException()
    {
        // Arrange & Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            var parallelClient = new ParallelRpcClient([], []);
            await parallelClient.GetBalanceAsync(ADDRESS);
        });
    }
}
