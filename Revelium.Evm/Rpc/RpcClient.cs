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
    public class RpcClient(string url, HttpClient? httpClient = null)
    {
        private readonly HttpClient _httpClient = httpClient ?? new HttpClient();

        public string Url { get; } = url;

        public RpcClient(RpcConfig config) : this(config.Url, CreateHttpClient(
            rateLimit: config.RateLimit,
            rateLimitTimeUnitSec: config.RateLimitTimeUnitSec,
            retryCount: config.RetryCount,
            firstRetryDelayMs: config.FirstRetryDelayMs))
        {
        }

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
                jsonBody: JsonSerializer.Serialize(body),
                cancellationToken: cancellationToken);

            if (error != null)
                return error;

            return new HexBigInteger(response).Value;
        }

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
                jsonBody: JsonSerializer.Serialize(body),
                cancellationToken: cancellationToken);

            if (error != null)
                return error;

            return new HexBigInteger(response).Value;
        }

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
                jsonBody: JsonSerializer.Serialize(body),
                cancellationToken: cancellationToken);

            if (error != null)
                return error;

            return new HexBigInteger(response).Value;
        }

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
                jsonBody: JsonSerializer.Serialize(body),
                cancellationToken: cancellationToken);

            if (error != null)
                return error;

            return new HexBigInteger(response).Value;
        }

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
                jsonBody: JsonSerializer.Serialize(body),
                cancellationToken: cancellationToken);

            if (error != null)
                return error;

            return new HexBigInteger(response).Value;
        }

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
                jsonBody: JsonSerializer.Serialize(body),
                cancellationToken: cancellationToken);

            if (error != null)
                return error;

            return response!;
        }

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
                jsonBody: JsonSerializer.Serialize(body),
                cancellationToken: cancellationToken);

            if (error != null)
                return error;

            return response!;
        }

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
                jsonBody: JsonSerializer.Serialize(body),
                cancellationToken: cancellationToken);
        }

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

            var (response, error) = await SendAsync<List<Log>>(
                jsonBody: body,
                cancellationToken: cancellationToken);

            if (error != null)
                return error;

            return response!;
        }

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
            var jsonBody = "{" +
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

            var (response, error) = await SendAsync<string>(
                jsonBody: jsonBody,
                cancellationToken: cancellationToken);

            if (error != null)
                return error;

            return new HexBigInteger(response).Value;
        }

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
            var jsonBody = "{" +
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

            return await SendAsync<TResult>(
                jsonBody: jsonBody,
                cancellationToken: cancellationToken);
        }

        private async Task<NullableResult<TResult>> SendAsync<TResult>(
            string jsonBody,
            CancellationToken cancellationToken = default)
        {
            var requestContent = new StringContent(
                content: jsonBody,
                encoding: Encoding.UTF8,
                mediaType: "application/json");

            var requestMessage = new HttpRequestMessage
            {
                RequestUri = new Uri(Url),
                Content = requestContent,
                Method = HttpMethod.Post
            };

            HttpResponseMessage response;

            try
            {
                response = await _httpClient
                    .SendAsync(requestMessage, cancellationToken);
            }
            catch (Exception ex)
            {
                return new Error(Errors.HTTP_REQUEST_ERROR, "Http request error", ex);
            }

            var responseContent = await response.Content
                .ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                return new Error((int)response.StatusCode, responseContent);

            if (responseContent == null)
                return new Error(Errors.INVALID_RESPONSE, "Response content is null");

            try
            {
                var rpcResponse = JsonSerializer.Deserialize<Response<TResult>>(responseContent);

                if (rpcResponse == null)
                    return new Error(Errors.INVALID_RESPONSE, "RPC response is null");

                if (rpcResponse.Error != null)
                    return new Error(rpcResponse.Error.Code, rpcResponse.Error.Message);

                return rpcResponse.Result;
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
