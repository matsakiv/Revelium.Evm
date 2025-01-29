using Incendium;
using Incendium.RetryPolicy;
using Nethereum.Hex.HexTypes;
using Revelium.Evm.Common;
using Revelium.Evm.Rpc.Models;
using Revelium.Evm.Rpc.Parameters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Numerics;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Revelium.Evm.Rpc
{
    /// <summary>
    /// Provides a client for making JSON-RPC calls to Ethereum-compatible nodes.
    /// Supports standard Ethereum JSON-RPC methods with automatic error handling and retries.
    /// </summary>
    public class RpcClient(string url, long? chainId = null, HttpClient? httpClient = null)
    {
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
            var body = new
            {
                id = 1,
                jsonrpc = "2.0",
                method = "eth_getBalance",
                @params = new string[]
                {
                    address,
                    block?.Value ?? BlockNumber.Latest.Value
                }
            };

            var (response, error) = await SendAsync<string>(
                JsonSerializer.Serialize(body),
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
            var body = new
            {
                id = 1,
                jsonrpc = "2.0",
                method = "eth_getTransactionCount",
                @params = new string[]
                {
                    address,
                    block?.Value ?? BlockNumber.Latest.Value
                }
            };

            var (response, error) = await SendAsync<string>(
                JsonSerializer.Serialize(body),
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
            var body = new
            {
                id = 1,
                jsonrpc = "2.0",
                method = "eth_gasPrice",
                @params = Array.Empty<string>()
            };

            var (response, error) = await SendAsync<string>(
                JsonSerializer.Serialize(body),
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
            var body = new
            {
                id = 1,
                jsonrpc = "2.0",
                method = "eth_maxPriorityFeePerGas",
                @params = Array.Empty<string>()
            };

            var (response, error) = await SendAsync<string>(
                JsonSerializer.Serialize(body),
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
            var body = new
            {
                id = 1,
                jsonrpc = "2.0",
                method = "eth_blockNumber",
                @params = Array.Empty<string>()
            };

            var (response, error) = await SendAsync<string>(
                JsonSerializer.Serialize(body),
                cancellationToken);

            if (error != null)
                return error;

            return new HexBigInteger(response).Value;
        }

        /// <summary>
        /// Gets information about a block by block number.
        /// </summary>
        /// <param name="block">Optional block number, or Latest/Pending/Earliest. Defaults to Latest.</param>
        /// <param name="includeTransactions">If true, returns full transaction objects. If false, only returns transaction hashes.</param>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>Information about the specified block.</returns>
        public async Task<Result<Block>> GetBlockByNumberAsync(
            BlockNumber? block = null,
            bool includeTransactions = true,
            CancellationToken cancellationToken = default)
        {
            var body = new
            {
                id = 1,
                jsonrpc = "2.0",
                method = "eth_getBlockByNumber",
                @params = new object[]
                {
                    block?.Value ?? BlockNumber.Latest.Value,
                    includeTransactions
                }
            };

            var (response, error) = await SendAsync<Block>(
                JsonSerializer.Serialize(body),
                cancellationToken);

            if (error != null)
                return error;

            return response!;
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
            var body = new
            {
                id = 1,
                jsonrpc = "2.0",
                method = "eth_sendRawTransaction",
                @params = new string[] { "0x" + signedTransactionData }
            };

            var (response, error) = await SendAsync<string>(
                JsonSerializer.Serialize(body),
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
            var body = new
            {
                id = 1,
                jsonrpc = "2.0",
                method = "eth_getTransactionReceipt",
                @params = new string[] { txId }
            };

            return await SendAsync<TransactionReceipt>(
                JsonSerializer.Serialize(body),
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
            var paramsList = new List<string>();

            if (fromBlock != null)
                paramsList.Add($"\"fromBlock\":\"{fromBlock.Value.Value}\"");

            if (toBlock != null)
                paramsList.Add($"\"toBlock\":\"{toBlock.Value.Value}\"");

            if (address != null)
                paramsList.Add($"\"address\":\"{address}\"");

            if (topics != null && topics.Length > 0)
                paramsList.Add($"\"topics\":[{string.Join(',', topics.Select(t => $"\"{t}\""))}]");

            if (blockHash != null)
                paramsList.Add($"\"blockHash\":\"{blockHash}\"");

            var @params = paramsList.Count > 0
                ? $"{{{string.Join(',', paramsList)}}}"
                : "";

            var body = "{" +
                "\"id\":1," +
                "\"jsonrpc\":\"2.0\"," +
                "\"method\":\"eth_getLogs\"," +
                $"\"params\":[{@params}]" +
            "}";

            var (response, error) = await SendAsync<List<Log>>(body, cancellationToken);

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
            var body = "{" +
                $"\"id\": 1," +
                $"\"jsonrpc\": \"2.0\"," +
                $"\"method\": \"eth_estimateGas\"," +
                $"\"params\":[" +
                    $"{{" +
                        (from != null ? $"\"from\": \"{from}\"," : "") +
                        (gas != null ? $"\"gas\": \"{new HexBigInteger(gas.Value)}\"," : "") +
                        (gasPrice != null ? $"\"gasPrice\": \"{new HexBigInteger(gasPrice.Value)}\"," : "") +
                        (maxPriorityFeePerGas != null ? $"\"maxPriorityFeePerGas\": \"{new HexBigInteger(maxPriorityFeePerGas.Value)}\"," : "") +
                        (maxFeePerGas != null ? $"\"maxFeePerGas\": \"{new HexBigInteger(maxFeePerGas.Value)}\"," : "") +
                        (value != null ? $"\"value\": \"{new HexBigInteger(value.Value)}\"," : "") +
                        (data != null ? $"\"data\": \"{data}\"," : "") +
                        $"\"to\": \"{to}\"" +
                    $"}}," +
                    $"\"{block?.Value ?? BlockNumber.Latest.Value}\"" +
                $"]" +
                "}";

            var (response, error) = await SendAsync<string>(body, cancellationToken);

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
            var body = "{" +
                $"\"id\": 1," +
                $"\"jsonrpc\": \"2.0\"," +
                $"\"method\": \"eth_call\"," +
                $"\"params\":[" +
                    $"{{" +
                        (from != null ? $"\"from\": \"{from}\"," : "") +
                        (gas != null ? $"\"gas\": \"{new HexBigInteger(gas.Value)}\"," : "") +
                        (gasPrice != null ? $"\"gasPrice\": \"{new HexBigInteger(gasPrice.Value)}\"," : "") +
                        (value != null ? $"\"value\": \"{new HexBigInteger(value.Value)}\"," : "") +
                        (input != null ? $"\"input\": \"{input}\"," : "") +
                        $"\"to\": \"{to}\"" +
                    $"}}," +
                    $"\"{block?.Value ?? BlockNumber.Latest.Value}\"" +
                $"]" +
                "}";

            return await SendAsync<TResult>(body, cancellationToken);
        }

        private async Task<NullableResult<TResult>> SendAsync<TResult>(
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
                    return new Error(Errors.INVALID_RESPONSE, "Response content is null");

                var rpcResponse = JsonSerializer.Deserialize<Response<TResult>>(responseContent);

                if (rpcResponse == null)
                    return new Error(Errors.INVALID_RESPONSE, "RPC response is null");

                if (rpcResponse.Error != null)
                    return new Error(rpcResponse.Error.Code, rpcResponse.Error.Message);

                return rpcResponse.Result;
            }
            catch (HttpRequestException ex)
            {
                return new Error(Errors.HTTP_REQUEST_ERROR, "Http request error", ex);
            }
            catch (JsonException ex)
            {
                return new Error(Errors.INVALID_RESPONSE, "Invalid JSON response", ex);
            }
            catch (Exception ex)
            {
                return new Error(Errors.INVALID_RESPONSE, "Invalid response", ex);
            }
        }

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
    }
}
