using Incendium;
using Incendium.RetryPolicy;
using Nethereum.Hex.HexTypes;
using Revelium.Evm.Rpc.Models;
using Revelium.Evm.Rpc.Parameters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Numerics;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace Revelium.Evm.Rpc
{
    /// <summary>
    /// Represents a JSON-RPC request.
    /// </summary>
    public record RpcRequest
    {
        /// <summary>
        /// The JSON-RPC version.
        /// </summary>
        [JsonPropertyName("jsonrpc")]
        public string JsonRpc { get; set; } = "2.0";

        /// <summary>
        /// The method to call.
        /// </summary>
        [JsonPropertyName("method")]
        public string Method { get; set; } = "";

        /// <summary>
        /// The parameters to pass to the method.
        /// </summary>
        [JsonPropertyName("params")]
        public object[] Params { get; set; } = [];

        /// <summary>
        /// The request ID.
        /// </summary>
        [JsonPropertyName("id")]
        public int Id { get; set; } = 1;
    }

    /// <summary>
    /// Provides a client for making JSON-RPC calls to Ethereum-compatible nodes.
    /// Supports standard Ethereum JSON-RPC methods with automatic error handling and retries.
    /// </summary>
    public class RpcClient(string url, long? chainId = null, HttpClient? httpClient = null)
    {
        public const int HTTP_REQUEST_ERROR = 1;
        public const int INVALID_RESPONSE = 2;
        public const int RPC_REQUEST_ERROR = 3;

        private readonly HttpClient _httpClient = httpClient ?? new HttpClient();

        public string Url { get; } = url;
        public long? ChainId { get; } = chainId;

        /// <summary>
        /// Initializes a new instance of the RpcClient class with the specified configuration.
        /// </summary>
        /// <param name="config">Configuration containing URL, chain ID, and rate limiting settings.</param>
        public RpcClient(RpcConfig config) : this(config.Url, config.ChainId, CreateHttpClient(
            config.RateLimit,
            config.RateLimitTimeUnitSec,
            config.RetryCount,
            config.FirstRetryDelayMs))
        {
        }

        /// <summary>
        /// Gets the account balance at the specified address.
        /// </summary>
        /// <param name="address">The address to check for balance.</param>
        /// <param name="block">Optional block number, or Latest/Pending/Earliest. Defaults to Latest.</param>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>The balance in wei as a BigInteger.</returns>
        public async Task<Result<BigInteger>> GetBalanceAsync(
            string address,
            BlockNumber? block = null,
            CancellationToken cancellationToken = default)
        {
            var request = CreateBalanceRequest(address, block);

            var (response, error) = await SendAsync<string>(
                JsonSerializer.Serialize(request),
                cancellationToken);

            if (error != null)
                return error;

            return new HexBigInteger(response).Value;
        }

        /// <summary>
        /// Gets the number of transactions sent from the specified address.
        /// </summary>
        /// <param name="address">The address to check.</param>
        /// <param name="block">Optional block number, or Latest/Pending/Earliest. Defaults to Latest.</param>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>The number of transactions sent from the address.</returns>
        public async Task<Result<BigInteger>> GetTransactionCountAsync(
            string address,
            BlockNumber? block = null,
            CancellationToken cancellationToken = default)
        {
            var request = CreateTransactionCountRequest(address, block);

            var (response, error) = await SendAsync<string>(
                JsonSerializer.Serialize(request),
                cancellationToken);

            if (error != null)
                return error;

            return new HexBigInteger(response).Value;
        }

        /// <summary>
        /// Gets the current gas price in wei.
        /// </summary>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>The current gas price in wei.</returns>
        public async Task<Result<BigInteger>> GetGasPriceAsync(
            CancellationToken cancellationToken = default)
        {
            var request = CreateGasPriceRequest();

            var (response, error) = await SendAsync<string>(
                JsonSerializer.Serialize(request),
                cancellationToken);

            if (error != null)
                return error;

            return new HexBigInteger(response).Value;
        }

        /// <summary>
        /// Gets the current maxPriorityFeePerGas in wei (EIP-1559).
        /// </summary>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>The current maxPriorityFeePerGas in wei.</returns>
        public async Task<Result<BigInteger>> GetMaxPriorityFeePerGasAsync(
            CancellationToken cancellationToken = default)
        {
            var request = CreateMaxPriorityFeePerGasRequest();

            var (response, error) = await SendAsync<string>(
                JsonSerializer.Serialize(request),
                cancellationToken);

            if (error != null)
                return error;

            return new HexBigInteger(response).Value;
        }

        /// <summary>
        /// Gets the latest block number.
        /// </summary>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>The number of the latest block.</returns>
        public async Task<Result<BigInteger>> GetBlockNumberAsync(
            CancellationToken cancellationToken = default)
        {
            var request = CreateBlockNumberRequest();

            var (response, error) = await SendAsync<string>(
                JsonSerializer.Serialize(request),
                cancellationToken);

            if (error != null)
                return error;

            return new HexBigInteger(response).Value;
        }

        /// <summary>
        /// Gets block by block number.
        /// </summary>
        /// <param name="block">Optional block number, or Latest/Pending/Earliest. Defaults to Latest.</param>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>Block.</returns>
        public async Task<Result<Block>> GetBlockByNumberAsync(
            BlockNumber? block = null,
            CancellationToken cancellationToken = default)
        {
            var request = CreateBlockByNumberRequest(block, includeTransactions: true);

            var (response, error) = await SendAsync<Block>(
                JsonSerializer.Serialize(request),
                cancellationToken);

            if (error != null)
                return error;

            return response!;
        }

        /// <summary>
        /// Gets block without transactions by block number.
        /// </summary>
        /// <param name="block">Optional block number, or Latest/Pending/Earliest. Defaults to Latest.</param>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>Block.</returns>
        public async Task<Result<LightBlock>> GetLightBlockByNumberAsync(
            BlockNumber? block = null,
            CancellationToken cancellationToken = default)
        {
            var request = CreateBlockByNumberRequest(block, includeTransactions: false);

            var (response, error) = await SendAsync<LightBlock>(
                JsonSerializer.Serialize(request),
                cancellationToken);

            if (error != null)
                return error;

            return response!;
        }

        /// <summary>
        /// Gets the transaction receipt for a transaction.
        /// </summary>
        /// <param name="txId">The transaction hash.</param>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>The transaction receipt if the transaction has been mined, null if pending.</returns>
        public async Task<NullableResult<TransactionReceipt>> GetTransactionReceiptAsync(
            string txId,
            CancellationToken cancellationToken = default)
        {
            var request = CreateTransactionReceiptRequest(txId);

            return await SendAsync<TransactionReceipt>(
                JsonSerializer.Serialize(request),
                cancellationToken);
        }

        /// <summary>
        /// Gets logs matching the specified filter criteria.
        /// </summary>
        /// <param name="fromBlock">Optional start block number.</param>
        /// <param name="toBlock">Optional end block number.</param>
        /// <param name="address">Optional contract address to filter by.</param>
        /// <param name="topics">Optional array of topics to filter by.</param>
        /// <param name="blockHash">Optional block hash to get logs from a single block.</param>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>A list of matching log entries.</returns>
        public async Task<Result<List<Log>>> GetLogsAsync(
            BlockNumber? fromBlock = null,
            BlockNumber? toBlock = null,
            string? address = null,
            string[]? topics = null,
            string? blockHash = null,
            CancellationToken cancellationToken = default)
        {
            var request = CreateLogsRequest(fromBlock, toBlock, address, topics, blockHash);

            var (response, error) = await SendAsync<List<Log>>(
                JsonSerializer.Serialize(request),
                cancellationToken);

            if (error != null)
                return error;

            return response!;
        }

        /// <summary>
        /// Gets logs matching the specified filter criteria.
        /// </summary>
        /// <param name="fromBlock">Optional start block number.</param>
        /// <param name="toBlock">Optional end block number.</param>
        /// <param name="address">Optional contract address to filter by.</param>
        /// <param name="topics">Optional array of topics to filter by. Each topic can also be an array of DATA with "or" options</param>
        /// <param name="blockHash">Optional block hash to get logs from a single block.</param>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>A list of matching log entries.</returns>
        public async Task<Result<List<Log>>> GetLogsWithTopicsFilterAsync(
            BlockNumber? fromBlock = null,
            BlockNumber? toBlock = null,
            string? address = null,
            string[][]? topics = null,
            string? blockHash = null,
            CancellationToken cancellationToken = default)
        {
            var request = CreateLogsWithTopicsFilterRequest(fromBlock, toBlock, address, topics, blockHash);

            var (response, error) = await SendAsync<List<Log>>(
                JsonSerializer.Serialize(request),
                cancellationToken);

            if (error != null)
                return error;

            return response!;
        }

        /// <summary>
        /// Estimates the gas needed to execute a transaction.
        /// </summary>
        /// <param name="to">The recipient address.</param>
        /// <param name="from">Optional sender address.</param>
        /// <param name="gas">Optional gas limit.</param>
        /// <param name="gasPrice">Optional gas price (pre EIP-1559).</param>
        /// <param name="maxPriorityFeePerGas">Optional maxPriorityFeePerGas (EIP-1559).</param>
        /// <param name="maxFeePerGas">Optional maxFeePerGas (EIP-1559).</param>
        /// <param name="value">Optional value in wei to send.</param>
        /// <param name="data">Optional contract data.</param>
        /// <param name="block">Optional block number for estimation. Defaults to Latest.</param>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>The estimated gas amount.</returns>
        public async Task<Result<BigInteger>> EstimateGasAsync(
            string to,
            string? from = null,
            BigInteger? gas = null,
            BigInteger? gasPrice = null,
            BigInteger? maxPriorityFeePerGas = null,
            BigInteger? maxFeePerGas = null,
            BigInteger? value = null,
            string? data = null,
            BlockNumber? block = null,
            CancellationToken cancellationToken = default)
        {
            var request = CreateEstimateGasRequest(
                to,
                from,
                gas,
                gasPrice,
                maxPriorityFeePerGas,
                maxFeePerGas,
                value,
                data,
                block);

            var (response, error) = await SendAsync<string>(
                JsonSerializer.Serialize(request),
                cancellationToken);

            if (error != null)
                return error;

            return new HexBigInteger(response).Value;
        }

        /// <summary>
        /// Executes a contract call without creating a transaction.
        /// </summary>
        /// <typeparam name="TResult">The expected return type.</typeparam>
        /// <param name="to">The contract address.</param>
        /// <param name="from">Optional sender address.</param>
        /// <param name="gas">Optional gas limit.</param>
        /// <param name="gasPrice">Optional gas price.</param>
        /// <param name="value">Optional value in wei to send.</param>
        /// <param name="input">Optional contract input data.</param>
        /// <param name="block">Optional block number. Defaults to Latest.</param>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>The result of the contract call.</returns>
        public async Task<NullableResult<TResult>> CallAsync<TResult>(
            string to,
            string? from = null,
            BigInteger? gas = null,
            BigInteger? gasPrice = null,
            BigInteger? value = null,
            string? input = null,
            BlockNumber? block = null,
            CancellationToken cancellationToken = default)
        {
            var request = CreateCallRequest(to, from, gas, gasPrice, value, input, block);

            return await SendAsync<TResult>(
                JsonSerializer.Serialize(request),
                cancellationToken);
        }

        /// <summary>
        /// Sends a signed raw transaction to the network.
        /// </summary>
        /// <param name="signedTransactionData">The signed transaction data as a hexadecimal string (without 0x prefix).</param>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>The transaction hash if successful.</returns>
        public async Task<Result<string>> SendRawTransactionAsync(
            string signedTransactionData,
            CancellationToken cancellationToken = default)
        {
            var request = CreateSendRawTransactionRequest(signedTransactionData);

            var (response, error) = await SendAsync<string>(
                JsonSerializer.Serialize(request),
                cancellationToken);

            if (error != null)
                return error;

            return response!;
        }

        /// <summary>
        /// Sends a request to the network and returns the response content.
        /// </summary>
        /// <param name="content">The request content.</param>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>The response content.</returns>
        public async Task<Result<string>> SendAsync(
            string content,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var requestContent = new StringContent(content, Encoding.UTF8, "application/json");

                using var requestMessage = new HttpRequestMessage
                {
                    RequestUri = new Uri(Url),
                    Content = requestContent,
                    Method = HttpMethod.Post
                };

                using var response = await _httpClient
                    .SendAsync(requestMessage, cancellationToken);

                var responseContent = await response.Content
                    .ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                    return new Error((int)response.StatusCode, responseContent);

                if (responseContent == null)
                    return new Error(INVALID_RESPONSE, "Rpc response content is null");

                return responseContent;
            }
            catch (HttpRequestException ex)
            {
                return new Error(HTTP_REQUEST_ERROR, "Http request error", ex);
            }
            catch (Exception ex)
            {
                return new Error(RPC_REQUEST_ERROR, "Rpc request error", ex);
            }
        }

        /// <summary>
        /// Sends a request to the network and deserializes the response to the specified type.
        /// </summary>
        /// <typeparam name="TResult">The expected return type.</typeparam>
        /// <param name="content">The request content.</param>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>The deserialized response.</returns>
        public async Task<NullableResult<TResult>> SendAsync<TResult>(
            string content,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var (responseContent, error) = await SendAsync(content, cancellationToken);

                if (error != null)
                    return error;

                if (responseContent == null)
                    return new Error(INVALID_RESPONSE, "Rpc response content is null");

                var rpcResponse = JsonSerializer.Deserialize<Response<TResult>>(responseContent);

                if (rpcResponse == null)
                    return new Error(
                        INVALID_RESPONSE,
                        "Rpc response is null after deserialization to Response<TResult>");

                if (rpcResponse.Error != null)
                {
                    if (rpcResponse.Error.Data != null)
                    {
                        return new Error(
                            rpcResponse.Error.Code,
                            $"Message: {rpcResponse.Error.Message}, Data: {rpcResponse.Error.Data ?? ""}");
                    }
                    else
                    {
                        return new Error(
                            rpcResponse.Error.Code,
                            $"Message: {rpcResponse.Error.Message}");
                    }
                }

                return rpcResponse.Result;
            }
            catch (JsonException ex)
            {
                return new Error(INVALID_RESPONSE, "Invalid json response", ex);
            }
            catch (Exception ex)
            {
                return new Error(RPC_REQUEST_ERROR, "Rpc request error", ex);
            }
        }

        /// <summary>
        /// Sends a batch of requests to the network and returns the raw JSON responses.
        /// </summary>
        /// <param name="requests">The collection of requests to send.</param>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>Array of JSON responses, maintaining the order of requests.</returns>
        public async Task<Result<NullableResult<JsonDocument>[]>> SendBatchAsync(
            IReadOnlyList<RpcRequest> requests,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var requestContent = JsonSerializer.Serialize(requests);

                var (responseContent, error) = await SendAsync(requestContent, cancellationToken);

                if (error != null)
                    return error;

                var responses = JsonSerializer.Deserialize<Response<JsonDocument>[]>(responseContent);

                if (responses == null || responses.Length != requests.Count)
                    return new Error(INVALID_RESPONSE, "Invalid batch response count");

                return responses
                    .Select(MapResponse)
                    .ToArray();
            }
            catch (JsonException ex)
            {
                return new Error(INVALID_RESPONSE, "Invalid json response", ex);
            }
            catch (Exception ex)
            {
                return new Error(RPC_REQUEST_ERROR, "Rpc request error", ex);
            }
        }

        /// <summary>
        /// Creates a new HttpClient with rate limiting and retry capabilities.
        /// </summary>
        /// <param name="rateLimit">The maximum number of requests per time unit.</param>
        /// <param name="rateLimitTimeUnitSec">The time unit for the rate limit.</param>
        /// <param name="retryCount">The number of retry attempts.</param>
        /// <param name="firstRetryDelayMs">The delay before the first retry.</param>
        /// <returns>A new HttpClient with rate limiting and retry capabilities.</returns>
        public static HttpClient CreateHttpClient(
            int rateLimit,
            int rateLimitTimeUnitSec,
            int retryCount,
            int firstRetryDelayMs)
        {
            var innerHttpClientHandler = new HttpClientHandler();
            var retryHttpClientHandler = new RetryHttpClientHandler(innerHttpClientHandler)
            {
                RetryCount = retryCount,
                RetryOnHttpRequestException = true,
                FirstRetryDelay = TimeSpan.FromMilliseconds(firstRetryDelayMs),
                RateGate = new RateGate(
                    occurrences: rateLimit,
                    timeUnit: TimeSpan.FromSeconds(rateLimitTimeUnitSec))
            };

            return new HttpClient(retryHttpClientHandler);
        }

        /// <summary>
        /// Creates a request to get the balance of an address at a specific block.
        /// </summary>
        /// <param name="address">The address to get the balance of.</param>
        /// <param name="block">The block number to get the balance at. Defaults to Latest.</param>
        /// <returns>A RpcRequest object containing the request details.</returns>
        public static RpcRequest CreateBalanceRequest(
            string address,
            BlockNumber? block = null)
        {
            return new RpcRequest
            {
                Id = 1,
                JsonRpc = "2.0",
                Method = "eth_getBalance",
                Params =
                [
                    address,
                    block?.Value ?? BlockNumber.Latest.Value
                ]
            };
        }

        /// <summary>
        /// Creates a request to get the number of transactions sent from an address at a specific block.
        /// </summary>
        /// <param name="address">The address to get the transaction count from.</param>
        /// <param name="block">The block number to get the transaction count at. Defaults to Latest.</param>
        /// <returns>A RpcRequest object containing the request details.</returns>
        public static RpcRequest CreateTransactionCountRequest(
            string address,
            BlockNumber? block = null)
        {
            return new RpcRequest
            {
                Id = 1,
                JsonRpc = "2.0",
                Method = "eth_getTransactionCount",
                Params =
                [
                    address,
                    block?.Value ?? BlockNumber.Latest.Value
                ]
            };
        }

        /// <summary>
        /// Creates a request to get the current gas price.
        /// </summary>
        /// <returns>A RpcRequest object containing the request details.</returns>
        public static RpcRequest CreateGasPriceRequest()
        {
            return new RpcRequest
            {
                Id = 1,
                JsonRpc = "2.0",
                Method = "eth_gasPrice",
            };
        }

        /// <summary>
        /// Creates a request to get the current maxPriorityFeePerGas.
        /// </summary>
        /// <returns>A RpcRequest object containing the request details.</returns>
        public static RpcRequest CreateMaxPriorityFeePerGasRequest()
        {
            return new RpcRequest
            {
                Id = 1,
                JsonRpc = "2.0",
                Method = "eth_maxPriorityFeePerGas",
            };
        }

        /// <summary>
        /// Creates a request to get the latest block number.
        /// </summary>
        /// <returns>A RpcRequest object containing the request details.</returns>
        public static RpcRequest CreateBlockNumberRequest()
        {
            return new RpcRequest
            {
                Id = 1,
                JsonRpc = "2.0",
                Method = "eth_blockNumber",
            };
        }

        /// <summary>
        /// Creates a request to get information about a block by block number.
        /// </summary>
        /// <param name="block">Optional block number, or Latest/Pending/Earliest. Defaults to Latest.</param>
        /// <param name="includeTransactions">If true, returns full transaction objects. If false, only returns transaction hashes.</param>
        /// <returns>A RpcRequest object containing the request details.</returns>
        public static RpcRequest CreateBlockByNumberRequest(
            BlockNumber? block = null,
            bool includeTransactions = true)
        {
            return new RpcRequest
            {
                Id = 1,
                JsonRpc = "2.0",
                Method = "eth_getBlockByNumber",
                Params =
                [
                    block?.Value ?? BlockNumber.Latest.Value,
                    includeTransactions
                ]
            };
        }

        /// <summary>
        /// Creates a request to get the transaction receipt for a transaction.
        /// </summary>
        /// <param name="txId">The transaction hash.</param>
        /// <returns>A RpcRequest object containing the request details.</returns>
        public static RpcRequest CreateTransactionReceiptRequest(string txId)
        {
            return new RpcRequest
            {
                Id = 1,
                JsonRpc = "2.0",
                Method = "eth_getTransactionReceipt",
                Params = [txId]
            };
        }

        /// <summary>
        /// Creates a request to get logs matching the specified filter criteria.
        /// </summary>
        /// <param name="fromBlock">Optional start block number.</param>
        /// <param name="toBlock">Optional end block number.</param>
        /// <param name="address">Optional contract address to filter by.</param>
        /// <param name="topics">Optional array of topics to filter by.</param>
        /// <param name="blockHash">Optional block hash to get logs from a single block.</param>
        /// <returns>A RpcRequest object containing the request details.</returns>
        public static RpcRequest CreateLogsRequest(
            BlockNumber? fromBlock = null,
            BlockNumber? toBlock = null,
            string? address = null,
            string[]? topics = null,
            string? blockHash = null)
        {
            var @params = new Dictionary<string, object>();

            if (fromBlock != null)
                @params.Add("fromBlock", fromBlock.Value.Value);

            if (toBlock != null)
                @params.Add("toBlock", toBlock.Value.Value);

            if (address != null)
                @params.Add("address", address);

            if (topics != null && topics.Length > 0)
                @params.Add("topics", topics);

            if (blockHash != null)
                @params.Add("blockHash", blockHash);

            return new RpcRequest
            {
                Id = 1,
                JsonRpc = "2.0",
                Method = "eth_getLogs",
                Params = @params.Count > 0
                    ? [@params]
                    : []
            };
        }

        /// <summary>
        /// Creates a request to get logs matching the specified filter criteria.
        /// </summary>
        /// <param name="fromBlock">Optional start block number.</param>
        /// <param name="toBlock">Optional end block number.</param>
        /// <param name="address">Optional contract address to filter by.</param>
        /// <param name="topics">Optional array of topics to filter by.</param>
        /// <param name="blockHash">Optional block hash to get logs from a single block.</param>
        /// <returns>A RpcRequest object containing the request details.</returns>
        public static RpcRequest CreateLogsWithTopicsFilterRequest(
            BlockNumber? fromBlock = null,
            BlockNumber? toBlock = null,
            string? address = null,
            string[][]? topics = null,
            string? blockHash = null)
        {
            var @params = new Dictionary<string, object>();

            if (fromBlock != null)
                @params.Add("fromBlock", fromBlock.Value.Value);

            if (toBlock != null)
                @params.Add("toBlock", toBlock.Value.Value);

            if (address != null)
                @params.Add("address", address);

            if (topics != null && topics.Length > 0)
            {
                var topicsFilter = new List<object?>();

                foreach (var topic in topics)
                {
                    if (topic == null || topic.Length == 0)
                    {
                        topicsFilter.Add(null);
                    }
                    else if (topic.Length == 1)
                    {
                        topicsFilter.Add(topic[0]);
                    }
                    else
                    {
                        topicsFilter.Add(topic);
                    }
                }

                @params.Add("topics", topicsFilter);
            }

            if (blockHash != null)
                @params.Add("blockHash", blockHash);

            return new RpcRequest
            {
                Id = 1,
                JsonRpc = "2.0",
                Method = "eth_getLogs",
                Params = @params.Count > 0
                    ? [@params]
                    : []
            };
        }

        public static RpcRequest CreateEstimateGasRequest(
            string to,
            string? from = null,
            BigInteger? gas = null,
            BigInteger? gasPrice = null,
            BigInteger? maxPriorityFeePerGas = null,
            BigInteger? maxFeePerGas = null,
            BigInteger? value = null,
            string? data = null,
            BlockNumber? block = null)
        {
            var @params = new Dictionary<string, object>();

            if (from != null)
                @params.Add("from", from);

            if (gas != null)
                @params.Add("gas", new HexBigInteger(gas.Value).HexValue);

            if (gasPrice != null)
                @params.Add("gasPrice", new HexBigInteger(gasPrice.Value).HexValue);

            if (maxPriorityFeePerGas != null)
                @params.Add("maxPriorityFeePerGas", new HexBigInteger(maxPriorityFeePerGas.Value).HexValue);

            if (maxFeePerGas != null)
                @params.Add("maxFeePerGas", new HexBigInteger(maxFeePerGas.Value).HexValue);

            if (value != null)
                @params.Add("value", new HexBigInteger(value.Value).HexValue);

            if (data != null)
                @params.Add("data", data);

            @params.Add("to", to);

            return new RpcRequest
            {
                Id = 1,
                JsonRpc = "2.0",
                Method = "eth_estimateGas",
                Params =
                [
                    @params,
                    block?.Value ?? BlockNumber.Latest.Value
                ]
            };
        }

        public static RpcRequest CreateCallRequest(
            string to,
            string? from = null,
            BigInteger? gas = null,
            BigInteger? gasPrice = null,
            BigInteger? value = null,
            string? input = null,
            BlockNumber? block = null)
        {
            var @params = new Dictionary<string, object>();

            if (from != null)
                @params.Add("from", from);

            if (gas != null)
                @params.Add("gas", new HexBigInteger(gas.Value).HexValue);

            if (gasPrice != null)
                @params.Add("gasPrice", new HexBigInteger(gasPrice.Value).HexValue);

            if (value != null)
                @params.Add("value", new HexBigInteger(value.Value).HexValue);

            if (input != null)
                @params.Add("input", input);

            @params.Add("to", to);

            return new RpcRequest
            {
                Id = 1,
                JsonRpc = "2.0",
                Method = "eth_call",
                Params =
                [
                    @params,
                    block?.Value ?? BlockNumber.Latest.Value
                ]
            };
        }

        /// <summary>
        /// Creates a request to send a raw transaction to the network.
        /// </summary>
        /// <param name="signedTransactionData">The signed transaction data as a hexadecimal string (without 0x prefix).</param>
        /// <returns>A RpcRequest object containing the request details.</returns>
        public static RpcRequest CreateSendRawTransactionRequest(string signedTransactionData)
        {
            return new RpcRequest
            {
                Id = 1,
                JsonRpc = "2.0",
                Method = "eth_sendRawTransaction",
                Params = ["0x" + signedTransactionData]
            };
        }

        private static NullableResult<JsonDocument> MapResponse(Response<JsonDocument>? response)
        {
            if (response == null)
                return new Error(INVALID_RESPONSE, "Null response in batch");

            if (response.Error != null)
                return new Error(response.Error.Code, response.Error.Message);

            return NullableResult<JsonDocument>.Success(response.Result);
        }
    }
}
