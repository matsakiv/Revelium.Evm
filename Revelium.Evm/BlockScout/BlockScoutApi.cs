using Revelium.Evm.BlockScout.Models;
using Revelium.Evm.Common;
using Incendium;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Revelium.Evm.BlockScout
{
    [Flags]
    public enum FromToFilter
    {
        From = 0x1,
        To = 0x2,
        Both = From | To
    }

    public class BlockScoutApi(string url, HttpClient? httpClient = null)
    {
        public const string ETHERLINK = "https://explorer.etherlink.com/";
        public const string ETHERLINK_TESTNET = "https://testnet.explorer.etherlink.com/";

        private readonly string _baseUrl = url;
        private readonly HttpClient _httpClient = httpClient ?? new HttpClient();

        public async Task<Result<Stats>> GetStatsAsync(
            CancellationToken cancellationToken = default)
        {
            return await SendAsync<Stats>(
                requestUrl: $"api/v2/stats",
                body: null,
                method: HttpMethod.Get,
                cancellationToken: cancellationToken);
        }

        #region Tokens

        public async Task<Result<Token>> GetTokenAsync(
            string address,
            CancellationToken cancellationToken = default)
        {
            return await SendAsync<Token>(
                requestUrl: $"api/v2/tokens/{address}",
                body: null,
                method: HttpMethod.Get,
                cancellationToken: cancellationToken);
        }

        public async Task<Result<List<Transfer>>> GetTokenTransfersAsync(
            string tokenAddress,
            int limit = int.MaxValue,
            CancellationToken cancellationToken = default)
        {
            NextPageParams? nextPageParams = null;
            var transfers = new List<Transfer>();

            while (true)
            {
                var requestUrl = $"api/v2/tokens/{tokenAddress}/transfers";
                var requestParams = new List<string>();

                if (nextPageParams != null)
                {
                    if (nextPageParams.BlockNumber != null)
                        requestParams.Add($"block_number={nextPageParams.BlockNumber}");
                    if (nextPageParams.Index != null)
                        requestParams.Add($"index={nextPageParams.Index}");
                    if (nextPageParams.ItemsCount != null)
                        requestParams.Add($"items_count={nextPageParams.ItemsCount}");
                }

                if (requestParams.Count > 0)
                    requestUrl += $"?{string.Join('&', requestParams)}";

                var (response, error) = await SendAsync<Response<Transfer>>(
                    requestUrl: requestUrl,
                    body: null,
                    method: HttpMethod.Get,
                    cancellationToken: cancellationToken);

                if (error != null)
                    return error;

                if (response == null)
                    return new Error(Errors.INVALID_RESPONSE, "Transfers response is null");

                var requiredCount = Math.Min(response.Items.Count, limit);
                limit -= requiredCount;

                transfers.AddRange(response.Items.Take(requiredCount));

                if (transfers.Count >= limit || response.NextPageParams == null)
                    return transfers;

                nextPageParams = response.NextPageParams;
            }
        }

        public async Task<Result<List<Holder>>> GetTokenHoldersAsync(
            string tokenAddress,
            int limit = int.MaxValue,
            CancellationToken cancellationToken = default)
        {
            NextPageParams? nextPageParams = null;
            var holders = new List<Holder>();

            while (true)
            {
                var requestUrl = $"api/v2/tokens/{tokenAddress}/holders";
                var requestParams = new List<string>();

                if (nextPageParams != null)
                {
                    if (nextPageParams.AddressHash != null)
                        requestParams.Add($"address_hash={nextPageParams.AddressHash}");
                    if (nextPageParams.Value != null)
                        requestParams.Add($"value={nextPageParams.Value}");
                    if (nextPageParams.ItemsCount != null)
                        requestParams.Add($"items_count={nextPageParams.ItemsCount}");
                }

                if (requestParams.Count > 0)
                    requestUrl += $"?{string.Join('&', requestParams)}";

                var (response, error) = await SendAsync<Response<Holder>>(
                    requestUrl: requestUrl,
                    body: null,
                    method: HttpMethod.Get,
                    cancellationToken: cancellationToken);

                if (error != null)
                    return error;

                if (response == null)
                    return new Error(Errors.INVALID_RESPONSE, "Holders response is null");

                var requiredCount = Math.Min(response.Items.Count, limit);
                limit -= requiredCount;

                holders.AddRange(response.Items.Take(requiredCount));

                if (holders.Count >= limit || response.NextPageParams == null)
                    return holders;

                nextPageParams = response.NextPageParams;
            }
        }

        #endregion Tokens

        #region Addresses

        public async Task<Result<List<TokenBalance>>> GetAddressAllTokensAsync(
            string address,
            CancellationToken cancellationToken = default)
        {
            return await SendAsync<List<TokenBalance>>(
                requestUrl: $"api/v2/addresses/{address}/token-balances",
                body: null,
                method: HttpMethod.Get,
                cancellationToken: cancellationToken);
        }

        public async Task<Result<List<Transfer>>> GetAddressTokenTransfersAsync(
            string address,
            string? tokenAddress = null,
            FromToFilter filter = FromToFilter.Both,
            int limit = int.MaxValue,
            CancellationToken cancellationToken = default)
        {
            NextPageParams? nextPageParams = null;
            var transfers = new List<Transfer>();

            while (true)
            {
                var requestUrl = $"api/v2/addresses/{address}/token-transfers";
                var requestParams = new List<string>();

                if (nextPageParams != null)
                {
                    if (nextPageParams.BlockNumber != null)
                        requestParams.Add($"block_number={nextPageParams.BlockNumber}");
                    if (nextPageParams.Index != null)
                        requestParams.Add($"index={nextPageParams.Index}");
                    if (nextPageParams.ItemsCount != null)
                        requestParams.Add($"items_count={nextPageParams.ItemsCount}");
                }

                if (filter == FromToFilter.To || filter == FromToFilter.From)
                    requestParams.Add($"filter={filter.ToString().ToLowerInvariant()}");

                if (tokenAddress != null)
                    requestParams.Add($"token={tokenAddress}");

                if (requestParams.Count > 0)
                    requestUrl += $"?{string.Join('&', requestParams)}";

                var (response, error) = await SendAsync<Response<Transfer>>(
                    requestUrl: requestUrl,
                    body: null,
                    method: HttpMethod.Get,
                    cancellationToken: cancellationToken);

                if (error != null)
                    return error;

                if (response == null)
                    return new Error(Errors.INVALID_RESPONSE, "Transfers response is null");

                var requiredCount = Math.Min(response.Items.Count, limit);
                limit -= requiredCount;

                transfers.AddRange(response.Items.Take(requiredCount));

                if (transfers.Count >= limit || response.NextPageParams == null)
                    return transfers;

                nextPageParams = response.NextPageParams;
            }
        }

        public async Task<Result<List<Transaction>>> GetAddressTransactionsAsync(
            string address,
            FromToFilter filter = FromToFilter.Both,
            string? fromHash = null,
            int limit = int.MaxValue,
            CancellationToken cancellationToken = default)
        {
            NextPageParams? nextPageParams = null;
            var txs = new List<Transaction>();

            while (true)
            {
                var requestUrl = $"api/v2/addresses/{address}/transactions";
                var requestParams = new List<string>();

                if (nextPageParams != null)
                {
                    if (nextPageParams.BlockNumber != null)
                        requestParams.Add($"block_number={nextPageParams.BlockNumber}");
                    if (nextPageParams.Index != null)
                        requestParams.Add($"index={nextPageParams.Index}");
                    if (nextPageParams.ItemsCount != null)
                        requestParams.Add($"items_count={nextPageParams.ItemsCount}");
                    if (nextPageParams.Value != null)
                        requestParams.Add($"value={nextPageParams.Value}");
                    if (nextPageParams.Fee != null)
                        requestParams.Add($"fee={nextPageParams.Fee}");
                    if (nextPageParams.Hash != null)
                        requestParams.Add($"hash={nextPageParams.Hash}");
                    if (nextPageParams.InsertedAt != null)
                        requestParams.Add($"inserted_at={nextPageParams.InsertedAt}");
                }

                if (filter == FromToFilter.To || filter == FromToFilter.From)
                    requestParams.Add($"filter={filter.ToString().ToLowerInvariant()}");

                if (requestParams.Count > 0)
                    requestUrl += $"?{string.Join('&', requestParams)}";

                var (response, error) = await SendAsync<Response<Transaction>>(
                    requestUrl: requestUrl,
                    body: null,
                    method: HttpMethod.Get,
                    cancellationToken: cancellationToken);

                if (error != null)
                    return error;

                if (response == null)
                    return new Error(Errors.INVALID_RESPONSE, "Transactions response is null");

                var requiredCount = Math.Min(response.Items.Count, limit);

                var fromHashIndex = -1;

                if (fromHash != null)
                {
                    fromHashIndex = response.Items.FindIndex(t => t.Hash == fromHash);

                    if (fromHashIndex >= 0)
                        requiredCount = Math.Min(fromHashIndex, requiredCount);
                }

                txs.AddRange(response.Items.Take(requiredCount));

                limit -= requiredCount;

                if (txs.Count >= limit || response.NextPageParams == null || fromHashIndex >= 0)
                    return txs;

                nextPageParams = response.NextPageParams;
            }
        }

        public async Task<Result<List<Log>>> GetAddressLogsAsync(
            string address,
            int limit = int.MaxValue,
            CancellationToken cancellationToken = default)
        {
            NextPageParams? nextPageParams = null;
            var logs = new List<Log>();

            while (true)
            {
                var requestUrl = $"api/v2/addresses/{address}/logs";
                var requestParams = new List<string>();

                if (nextPageParams != null)
                {
                    if (nextPageParams.BlockNumber != null)
                        requestParams.Add($"block_number={nextPageParams.BlockNumber}");
                    if (nextPageParams.Index != null)
                        requestParams.Add($"index={nextPageParams.Index}");
                    if (nextPageParams.ItemsCount != null)
                        requestParams.Add($"items_count={nextPageParams.ItemsCount}");
                }

                if (requestParams.Count > 0)
                    requestUrl += $"?{string.Join('&', requestParams)}";

                var (response, error) = await SendAsync<Response<Log>>(
                    requestUrl: requestUrl,
                    body: null,
                    method: HttpMethod.Get,
                    cancellationToken: cancellationToken);

                if (error != null)
                    return error;

                if (response == null)
                    return new Error(Errors.INVALID_RESPONSE, "Logs response is null");

                var requiredCount = Math.Min(response.Items.Count, limit);
                limit -= requiredCount;

                logs.AddRange(response.Items.Take(requiredCount));

                if (logs.Count >= limit || response.NextPageParams == null)
                    return logs;

                nextPageParams = response.NextPageParams;
            }
        }

        public async Task<Result<AddressCounters>> GetAddressCountersAsync(
            string address,
            CancellationToken cancellationToken = default)
        {
            return await SendAsync<AddressCounters>(
                requestUrl: $"api/v2/addresses/{address}/counters",
                body: null,
                method: HttpMethod.Get,
                cancellationToken: cancellationToken);
        }

        #endregion Addresses

        #region Transactions

        public async Task<Result<Transaction>> GetTransactionAsync(
            string hash,
            CancellationToken cancellationToken = default)
        {
            return await SendAsync<Transaction>(
                requestUrl: $"api/v2/transactions/{hash}",
                body: null,
                method: HttpMethod.Get,
                cancellationToken: cancellationToken);
        }

        public async Task<Result<List<Transfer>>> GetTransactionTokenTransfersAsync(
            string hash,
            int limit = int.MaxValue,
            CancellationToken cancellationToken = default)
        {
            NextPageParams? nextPageParams = null;
            var transfers = new List<Transfer>();

            while (true)
            {
                var requestUrl = $"api/v2/transactions/{hash}/token-transfers";
                var requestParams = new List<string>();

                if (nextPageParams != null)
                {
                    if (nextPageParams.BlockNumber != null)
                        requestParams.Add($"block_number={nextPageParams.BlockNumber}");
                    if (nextPageParams.Index != null)
                        requestParams.Add($"index={nextPageParams.Index}");
                    if (nextPageParams.ItemsCount != null)
                        requestParams.Add($"items_count={nextPageParams.ItemsCount}");
                }

                if (requestParams.Count > 0)
                    requestUrl += $"?{string.Join('&', requestParams)}";

                var (response, error) = await SendAsync<Response<Transfer>>(
                    requestUrl: requestUrl,
                    body: null,
                    method: HttpMethod.Get,
                    cancellationToken: cancellationToken);

                if (error != null)
                    return error;

                if (response == null)
                    return new Error(Errors.INVALID_RESPONSE, "Transfers response is null");

                var requiredCount = Math.Min(response.Items.Count, limit);
                limit -= requiredCount;

                transfers.AddRange(response.Items.Take(requiredCount));

                if (transfers.Count >= limit || response.NextPageParams == null)
                    return transfers;

                nextPageParams = response.NextPageParams;
            }
        }

        public async Task<Result<List<Log>>> GetTransactionLogsAsync(
            string hash,
            int limit = int.MaxValue,
            CancellationToken cancellationToken = default)
        {
            NextPageParams? nextPageParams = null;
            var logs = new List<Log>();

            while (true)
            {
                var requestUrl = $"api/v2/transactions/{hash}/logs";
                var requestParams = new List<string>();

                if (nextPageParams != null)
                {
                    if (nextPageParams.BlockNumber != null)
                        requestParams.Add($"block_number={nextPageParams.BlockNumber}");
                    if (nextPageParams.Index != null)
                        requestParams.Add($"index={nextPageParams.Index}");
                    if (nextPageParams.ItemsCount != null)
                        requestParams.Add($"items_count={nextPageParams.ItemsCount}");
                }

                if (requestParams.Count > 0)
                    requestUrl += $"?{string.Join('&', requestParams)}";

                var (response, error) = await SendAsync<Response<Log>>(
                    requestUrl: requestUrl,
                    body: null,
                    method: HttpMethod.Get,
                    cancellationToken: cancellationToken);

                if (error != null)
                    return error;

                if (response == null)
                    return new Error(Errors.INVALID_RESPONSE, "Logs response is null");

                var requiredCount = Math.Min(response.Items.Count, limit);
                limit -= requiredCount;

                logs.AddRange(response.Items.Take(requiredCount));

                if (logs.Count >= limit || response.NextPageParams == null)
                    return logs;

                nextPageParams = response.NextPageParams;
            }
        }

        #endregion Transactions

        private async Task<Result<TResult>> SendAsync<TResult>(
            string requestUrl,
            string? body,
            HttpMethod method,
            CancellationToken cancellationToken = default)
        {
            var requestContent = body != null
                ? new StringContent(
                    content: body,
                    encoding: Encoding.UTF8,
                    mediaType: "application/json")
                : null;

            var requestMessage = new HttpRequestMessage
            {
                RequestUri = new Uri(Url.Combine(_baseUrl, requestUrl)),
                Content = requestContent,
                Method = method
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
                var result = JsonSerializer.Deserialize<TResult>(responseContent);

                if (result == null)
                    return new Error(Errors.INVALID_RESPONSE, "Result is null after deserialization");

                return result;
            }
            catch (Exception ex)
            {
                return new Error(Errors.INVALID_RESPONSE, "Invalid response", ex);
            }
        }
    }
}
