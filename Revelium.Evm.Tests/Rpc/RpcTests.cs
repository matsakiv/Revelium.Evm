using Moq;
using Moq.Protected;
using Revelium.Evm.Common;
using Revelium.Evm.Rpc.Models;
using Revelium.Evm.Rpc.Parameters;
using System.Net;
using System.Numerics;
using System.Text.Json;

namespace Revelium.Evm.Rpc
{
    public class RpcTests
    {
        private const string RPC_URL = "http://localhost:8000/";
        private const string ADDRESS = "0xd8dA6BF26964aF9D7eEd9e03E53415D37aA96045";

        private readonly Mock<HttpMessageHandler> _handler;
        private readonly RpcClient _rpc;
        private static readonly string[] expectedTopics = [
            "0xddf252ad1be2c89b69c2b068fc378daa952ba7f163c4a11628f55a4df523b3ef",
            "0x000000000000000000000000ffff100b0017aced8e01b8eb0454fe09c43364b9",
            "0x000000000000000000000000d8da6bf26964af9d7eed9e03e53415d37aa96045"
        ];

        public RpcTests()
        {
            _handler = new Mock<HttpMessageHandler>();
            _rpc = new(RPC_URL, chainId: null, new HttpClient(_handler.Object));
        }

        [Fact()]
        public async Task Test_RpcClient_GetBalance()
        {
            // Arrange
            SetupJsonRpcResponseResult("0xb9052a11d665600");

            // Act
            var (balance, error) = await _rpc.GetBalanceAsync(ADDRESS);

            // Assert
            Assert.Null(error);
            Assert.Equal(833256783000000000, balance);
            VerifyRequestSent();
        }

        [Fact()]
        public async Task Test_RpcClient_GetTransactionCount()
        {
            // Arrange
            SetupJsonRpcResponseResult("0x2");

            // Act
            var (count, error) = await _rpc.GetTransactionCountAsync(ADDRESS);

            // Assert
            Assert.Null(error);
            Assert.Equal(0x2, count);
            VerifyRequestSent();
        }

        [Fact()]
        public async Task Test_RpcClient_GetGasPrice()
        {
            // Arrange
            SetupJsonRpcResponseResult("0x3b9aca00");

            // Act
            var (gasPrice, error) = await _rpc.GetGasPriceAsync();

            // Assert
            Assert.Null(error);
            Assert.Equal(1000000000, gasPrice);
            VerifyRequestSent();
        }

        [Fact()]
        public async Task Test_RpcClient_GetMaxPriorityFeePerGas()
        {
            // Arrange
            SetupJsonRpcResponseResult("0x0");

            // Act
            var (maxPriorityFeePerGas, error) = await _rpc.GetMaxPriorityFeePerGasAsync();

            // Assert
            Assert.Null(error);
            Assert.Equal(0, maxPriorityFeePerGas);
            VerifyRequestSent();
        }

        [Fact()]
        public async Task Test_RpcClient_GetBlockNumber()
        {
            // Arrange
            SetupJsonRpcResponseResult("0x670b90");

            // Act
            var (blockNumber, error) = await _rpc.GetBlockNumberAsync();

            // Assert
            Assert.Null(error);
            Assert.Equal(6753168, blockNumber);
            VerifyRequestSent();
        }

        [Fact()]
        public async Task Test_RpcClient_GetBlockByNumber()
        {
            // Arrange
            SetupJsonRpcResponse(
            "{" +
                "\"jsonrpc\":\"2.0\"," +
                "\"result\":{" +
                    "\"number\":\"0x670b90\"," +
                    "\"hash\":\"0x3ab00abc2edbd0ce7b1c3130a5c40b5f6b87285644a9743d8ec91f955ac62297\"," +
                    "\"parentHash\":\"0x15b0ee8935d524f528e1eff517dad1630e620f3a54b6a2259448bfaeb2f92867\"," +
                    "\"nonce\":\"0x0000000000000000\"," +
                    "\"sha3Uncles\":\"0x1dcc4de8dec75d7aab85b567b6ccd41ad312451b948a7413f0a142fd40d49347\"," +
                    "\"logsBloom\":\"0x00000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000\"," +
                    "\"transactionsRoot\":\"0x7f1b2d4860ff3cd369e0d91e10089edf6a70e8d82b0bc30900176415e18f269d\"," +
                    "\"stateRoot\":\"0x7a8683146f899aaeb91313f5ed4d94a085771d423c07c16057809ab017c73497\"," +
                    "\"receiptsRoot\":\"0xf25f466f4cc0f6bf814df7f7c2fef815b5a9fc20fc3812dc8c026a3d4536699a\"," +
                    "\"miner\":\"0xcf02b9ca488f8f6f4e28e37aa1bdd16b3f1b2ad8\"," +
                    "\"difficulty\":\"0x0\"," +
                    "\"totalDifficulty\":\"0x0\"," +
                    "\"extraData\":\"0x\"," +
                    "\"size\":\"0x279\"," +
                    "\"gasLimit\":\"0x4000000000000\"," +
                    "\"gasUsed\":\"0x45643c\"," +
                    "\"timestamp\":\"0x67bc82e4\"," +
                    "\"transactions\":[" +
                        "{" +
                            "\"blockHash\":\"0x3ab00abc2edbd0ce7b1c3130a5c40b5f6b87285644a9743d8ec91f955ac62297\"," +
                            "\"blockNumber\":\"0x670b90\"," +
                            "\"from\":\"0x5d66ec78664f4a0b0929a41270316a6cd4d8bd4b\"," +
                            "\"gas\":\"0x4c4b40\"," +
                            "\"gasPrice\":\"0x3b9aca00\"," +
                            "\"hash\":\"0x0766e94a24cf7c7e32f2dddb2d2e4efba6ee84eda7c44ea49acf60a10528dc0b\"," +
                            "\"input\":\"0xc14c920400000000000000000000000000000000000000000000000000000195385f13f045544800000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000003e7383f4800195385f13f000000020000001e77f4b161a0b481f27b60eec1809e327ec3404ae05e4cd2baec4ab07b2adb4a57b2dc1475077ceea29d8caebe260ceb04f48604f91c5f41bb5ecf8b4b3a4113f1c45544800000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000003e7383f4800195385f13f000000020000001950e4204d0acb985b67116554d0f451c6d22f43c06da5ec8712d067abb4ae3cb57f5c849d6427ca3a34cf0d41498335bc84e0202ed1f5de5463b635cc1fb6afd1c4254430000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000008b3a885e0bc0195385f13f0000000200000012a09b934bb0fbb20e08bc47d402fd80452037663f25099636b96fccc57f258ba3f9b72a5d0cd568116b274b929aa048480eefc33af28311d314717f79861511a1c4254430000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000008b3a885e0bc0195385f13f00000002000000103934f8f9ecd26e5c6e8d62bc88d939ed5b0ed62d21f97e6935af99c953a9e4e49adf6a912b73b7d9a668b115fe811e171947024c25df4a4b09d8c8c26d562b41b58545a00000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000004f401600195385f13f000000020000001c5d368c08a01f889677ea6c064f7f35ad402b15ee5e1741b154a54dc0b0adedf534afb4559cca8a0f447bee00a6326af90dc026cee0ae75b231c895e26126f931c58545a00000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000004f401600195385f13f00000002000000105da11cc16a62d08b7943b79ee603fc8f0a66ce28900087e45909d389950a97e2931c7d94c09bb680e4605e86176852011bb8b8b9e10e2fc76b0555154144bde1c00063137343034303735323238313223302e352e3123646174612d7061636b616765732d77726170706572000029000002ed57011e0000\"," +
                            "\"nonce\":\"0x1c65e6\"," +
                            "\"to\":\"0xa2cca359c43839040cf3d230deb1689ab8db2dac\"," +
                            "\"transactionIndex\":\"0x0\"," +
                            "\"value\":\"0x0\"," +
                            "\"v\":\"0x14e75\"," +
                            "\"r\":\"0x15098710572e60f67797bdbcab7b1b63fbc4be5114749905cfe785f9ce60ebe6\"," +
                            "\"s\":\"0x3be2cd2c4dc53710aa12c3b4455c0e85dd77c49f1bc5cb414ff0e0974e84d5b8\"" +
                        "}" +
                    "]," +
                    "\"uncles\":[]," +
                    "\"baseFeePerGas\":\"0x3b9aca00\"," +
                    "\"prevRandao\":\"0x0000000000000000000000000000000000000000000000000000000000000000\"" +
                "}," +
                "\"id\":1" +
            "}");

            // Act
            var (block, error) = await _rpc.GetBlockByNumberAsync();

            // Assert
            Assert.Null(error);
            Assert.Equal(6753168, block.GetBlockNumber());
            Assert.Equal("0x3ab00abc2edbd0ce7b1c3130a5c40b5f6b87285644a9743d8ec91f955ac62297", block.Hash);
            Assert.Equal("0x279", block.Size);
            Assert.Equal("0x4000000000000", block.GasLimit);
            Assert.Equal("0x45643c", block.GasUsed);
            Assert.Equal("0x67bc82e4", block.TimeStamp);
            Assert.Equal("0x3b9aca00", block.BaseFeePerGas);
            Assert.NotEmpty(block.Transactions);
            Assert.Equal("0x0766e94a24cf7c7e32f2dddb2d2e4efba6ee84eda7c44ea49acf60a10528dc0b", block.Transactions[0].Hash);
            Assert.Equal("0x5d66ec78664f4a0b0929a41270316a6cd4d8bd4b", block.Transactions[0].From);
            Assert.Equal("0xa2cca359c43839040cf3d230deb1689ab8db2dac", block.Transactions[0].To);
            Assert.Equal("0x0", block.Transactions[0].Value);
            Assert.Equal("0x14e75", block.Transactions[0].V);
            Assert.Equal("0x15098710572e60f67797bdbcab7b1b63fbc4be5114749905cfe785f9ce60ebe6", block.Transactions[0].R);
            Assert.Equal("0x3be2cd2c4dc53710aa12c3b4455c0e85dd77c49f1bc5cb414ff0e0974e84d5b8", block.Transactions[0].S);
            Assert.Equal("0x1c65e6", block.Transactions[0].Nonce);
            Assert.Equal("0x0", block.Transactions[0].TransactionIndex);
            Assert.Equal("0x4c4b40", block.Transactions[0].Gas);
            Assert.Equal("0x3b9aca00", block.Transactions[0].GasPrice);
            VerifyRequestSent();
        }

        [Fact()]
        public async Task Test_RpcClient_GetTransactionReceipt()
        {
            // Arrange
            SetupJsonRpcResponse(
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

            // Act
            var (receipt, error) = await _rpc.GetTransactionReceiptAsync(
                "0x0766e94a24cf7c7e32f2dddb2d2e4efba6ee84eda7c44ea49acf60a10528dc0b");

            // Assert
            Assert.Null(error);
            Assert.NotNull(receipt);
            Assert.Equal("0x1", receipt.Status);
            Assert.Equal("0x0766e94a24cf7c7e32f2dddb2d2e4efba6ee84eda7c44ea49acf60a10528dc0b", receipt.TransactionHash);
            Assert.Equal("0x0", receipt.TransactionIndex);
            Assert.Equal("0x3ab00abc2edbd0ce7b1c3130a5c40b5f6b87285644a9743d8ec91f955ac62297", receipt.BlockHash);
            Assert.Equal("0x670b90", receipt.BlockNumber);
            Assert.Equal("0x5d66ec78664f4a0b0929a41270316a6cd4d8bd4b", receipt.From);
            Assert.Equal("0xa2cca359c43839040cf3d230deb1689ab8db2dac", receipt.To);
            Assert.Equal("0x45643c", receipt.CumulativeGasUsed);
            Assert.Equal("0x3b9aca00", receipt.EffectiveGasPrice);
            Assert.Equal("0x45643c", receipt.GasUsed);
            Assert.Empty(receipt.Logs);
            Assert.Equal("0x0", receipt.Type);
            Assert.Null(receipt.ContractAddress);
            VerifyRequestSent();
        }

        [Fact()]
        public async Task Test_RpcClient_GetLogs()
        {
            // Arrange
            SetupJsonRpcResponse(
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

            // Act
            var (logs, error) = await _rpc.GetLogsAsync();

            // Assert
            Assert.Null(error);
            Assert.NotEmpty(logs);
            Assert.Equal("0x7f89bf25a530bb82cf51be8d0b14b5239df17f1c", logs[0].Address);
            Assert.Equal("0x00000000000000000000000000000000000000000000152d02c7e14af6800000", logs[0].Data);
            Assert.Equal("0x95e6", logs[0].BlockNumber);
            Assert.Equal("0x87b6f0f6708940659fb7beda7a198d14dd836ffec9897353dff9e4c38f28fb9b", logs[0].TransactionHash);
            Assert.Equal("0x0", logs[0].TransactionIndex);
            Assert.Equal("0x0", logs[0].LogIndex);
            Assert.False(logs[0].Removed);
            Assert.Equal(expectedTopics, logs[0].Topics);
            VerifyRequestSent();
        }

        [Fact()]
        public async Task Test_RpcClient_GetLogsWithTopicsFilter()
        {
            // Arrange
            SetupJsonRpcResponse(
            "{" +
                "\"jsonrpc\":\"2.0\"," +
                "\"result\":[" +
                    "{" +
                        "\"address\":\"0xd0bc067cf877f7b76ceb331891331d9e6acda1a7\"," +
                        "\"topics\":[" +
                            "\"0x921a8ce27b6521c36c2f7901104535739cfb9dc28cd1bca46f5fe5913f0d079d\"" +
                        "]," +
                        "\"data\":\"0x000000000000000000000000000000000000000000000000001c9ffffffb84ae00000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000003b9ad81e00000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000\"," +
                        "\"blockNumber\":\"0x843b97\"," +
                        "\"transactionHash\":\"0xc1de995ede032da3c38d845dfc211860096f1281fd970ef8e840830f0fdbfa50\"," +
                        "\"transactionIndex\":\"0x1\"," +
                        "\"blockHash\":\"0xf7d63a306492efe5fb758e5b88cfcad7967ba8682f764a3efc891bace0686f2c\"," +
                        "\"logIndex\":\"0xf\"," +
                        "\"removed\":false" +
                    "}," +
                    "{" +
                        "\"address\":\"0xd0bc067cf877f7b76ceb331891331d9e6acda1a7\"," +
                        "\"topics\":[" +
                            "\"0x921a8ce27b6521c36c2f7901104535739cfb9dc28cd1bca46f5fe5913f0d079d\"" +
                        "]," +
                        "\"data\":\"0x000000000000000000000000000000000000000000000000001c99fffffb84ac000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000218712f5500000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000\"," +
                        "\"blockNumber\":\"0x843b97\"," +
                        "\"transactionHash\":\"0xc1de995ede032da3c38d845dfc211860096f1281fd970ef8e840830f0fdbfa50\"," +
                        "\"transactionIndex\":\"0x1\"," +
                        "\"blockHash\":\"0xf7d63a306492efe5fb758e5b88cfcad7967ba8682f764a3efc891bace0686f2c\"," +
                        "\"logIndex\":\"0x12\"," +
                        "\"removed\":false" +
                    "}," +
                    "{" +
                        "\"address\":\"0xd0bc067cf877f7b76ceb331891331d9e6acda1a7\"," +
                        "\"topics\":[" +
                            "\"0x7cbbf9e533e963aebd385f66fc01e69558a4fe08eb6c6bc0127fa292b0442800\"," +
                            "\"0x000000000000000000000000fc03b2f8b4fb6438ce7d4e4b5f91372157a514cd\"," +
                            "\"0x0000000000000000000000000000000000000000000000000000000000000000\"" +
                        "]," +
                        "\"data\":\"0x000000000000000000000000000000000000000000000000001ca4fffffb84aa00000000000000000000000000000000000000000000000000000000000214c50000000000000000000000000000000000000000000000000000000000001ca400000000000000000000000000000000000000000000000000000000000214c5000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000\"," +
                        "\"blockNumber\":\"0x843b97\"," +
                        "\"transactionHash\":\"0xc1de995ede032da3c38d845dfc211860096f1281fd970ef8e840830f0fdbfa50\"," +
                        "\"transactionIndex\":\"0x1\"," +
                        "\"blockHash\":\"0xf7d63a306492efe5fb758e5b88cfcad7967ba8682f764a3efc891bace0686f2c\"," +
                        "\"logIndex\":\"0x15\"," +
                        "\"removed\":false" +
                    "}," +
                    "{" +
                        "\"address\":\"0xd0bc067cf877f7b76ceb331891331d9e6acda1a7\"," +
                        "\"topics\":[" +
                            "\"0x7cbbf9e533e963aebd385f66fc01e69558a4fe08eb6c6bc0127fa292b0442800\"," +
                            "\"0x000000000000000000000000fc03b2f8b4fb6438ce7d4e4b5f91372157a514cd\"," +
                            "\"0x0000000000000000000000000000000000000000000000000000000000000000\"" +
                        "]," +
                        "\"data\":\"0x000000000000000000000000000000000000000000000000001c9efffffb84a8000000000000000000000000000000000000000000000000000000000012bed60000000000000000000000000000000000000000000000000000000000001c9e000000000000000000000000000000000000000000000000000000000012bed6000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000\"," +
                        "\"blockNumber\":\"0x843b97\"," +
                        "\"transactionHash\":\"0xc1de995ede032da3c38d845dfc211860096f1281fd970ef8e840830f0fdbfa50\"," +
                        "\"transactionIndex\":\"0x1\"," +
                        "\"blockHash\":\"0xf7d63a306492efe5fb758e5b88cfcad7967ba8682f764a3efc891bace0686f2c\"," +
                        "\"logIndex\":\"0x17\"," +
                        "\"removed\":false" +
                    "}" +
                "]," +
                "\"id\":1" +
            "}");

            // Act
            var (logs, error) = await _rpc.GetLogsWithTopicsFilterAsync(
                fromBlock: BlockNumber.FromValue(0x843b97),
                toBlock: BlockNumber.FromValue(0x843b97),
                address: "0xD0BC067CF877F7b76CeB331891331d9e6ACda1a7",
                topics: [
                    [
                        "0x7cbbf9e533e963aebd385f66fc01e69558a4fe08eb6c6bc0127fa292b0442800",
                        "0x921a8ce27b6521c36c2f7901104535739cfb9dc28cd1bca46f5fe5913f0d079d"
                    ]
                ]);

            // Assert
            Assert.Null(error);
            Assert.NotEmpty(logs);
            Assert.Equal(4, logs.Count);
            Assert.Equal("0x921a8ce27b6521c36c2f7901104535739cfb9dc28cd1bca46f5fe5913f0d079d", logs[0].Topics[0]);
            Assert.Equal("0x921a8ce27b6521c36c2f7901104535739cfb9dc28cd1bca46f5fe5913f0d079d", logs[1].Topics[0]);
            Assert.Equal("0x7cbbf9e533e963aebd385f66fc01e69558a4fe08eb6c6bc0127fa292b0442800", logs[2].Topics[0]);
            Assert.Equal("0x7cbbf9e533e963aebd385f66fc01e69558a4fe08eb6c6bc0127fa292b0442800", logs[3].Topics[0]);
            VerifyRequestSent();
        }

        [Fact()]
        public async Task Test_RpcClient_EstimateGas()
        {
            // Arrange
            SetupJsonRpcResponseResult("0xdc5ef");

            // Act
            var (gas, error) = await _rpc.EstimateGasAsync(
                to: "0xE67B7D039b78DE25367EF5E69596075Bbd852BA9",
                from: "0xd8dA6BF26964aF9D7eEd9e03E53415D37aA96045",
                data: "0xfaf29cb3" +
                       "000000000000000000000000d8dA6BF26964aF9D7eEd9e03E53415D37aA96045" +
                       "0000000000000000000000000000000000000000000000000000000000000000",
                maxFeePerGas: 1000000000,
                maxPriorityFeePerGas: 0);

            // Assert
            Assert.Null(error);
            Assert.Equal(0xdc5ef, gas);
            VerifyRequestSent();
        }

        [Fact()]
        public async Task Test_RpcClient_Call()
        {
            // Arrange
            SetupJsonRpcResponseResult("0x0000000000000000000000000000000000000000000000000000000004a27c59fffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff8");

            // Act
            var (result, error) = await _rpc.CallAsync<string>(
                to: "0xEfE04e62499100AcA732Db6A75868290F738928B",
                from: "0xd8dA6BF26964aF9D7eEd9e03E53415D37aA96045",
                input: "0x2ea68d850000000000000000000000000000000000000000000000000000000000000000");

            // Assert
            Assert.Null(error);
            Assert.Equal("0x0000000000000000000000000000000000000000000000000000000004a27c59fffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff8", result);
            VerifyRequestSent();
        }

        [Fact()]
        public async Task Test_RpcClient_Batch()
        {
            // Arrange
            SetupJsonRpcResponse(
            "[" +
                "{" +
                    "\"jsonrpc\":\"2.0\"," +
                    "\"result\":\"0x16345785d8a0000\"," +
                    "\"id\":1" +
                "}," +
                "{" +
                    "\"jsonrpc\":\"2.0\"," +
                    "\"result\":{" +
                        "\"number\":\"0x672fa9\"," +
                        "\"hash\":\"0x11b04bdaa77ceeb62cd622a1b12f423064673a1d3d9641bd1d42c238f8e64b34\"," +
                        "\"parentHash\":\"0xd2e3029f09ffe91178b53459759f98b93ccb224bd3c19d99e0ab1e86d28b1798\"," +
                        "\"nonce\":\"0x0000000000000000\"," +
                        "\"sha3Uncles\":\"0x1dcc4de8dec75d7aab85b567b6ccd41ad312451b948a7413f0a142fd40d49347\"," +
                        "\"logsBloom\":\"0x00000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000\"," +
                        "\"transactionsRoot\":\"0xddeb678fd0597a8ec8d8b4db327c398da4848736f706e29d409b6d0d8428d690\"," +
                        "\"stateRoot\":\"0xa7a8151a2ae0b664ef86afd00cda867ae0a15f3e141160c43ff88450e99defe8\"," +
                        "\"receiptsRoot\":\"0x2c6d8278b566bcc735046f0730c1fb5fcab5f3049202539d5906ecbb9d48a2a8\"," +
                        "\"miner\":\"0xcf02b9ca488f8f6f4e28e37aa1bdd16b3f1b2ad8\"," +
                        "\"difficulty\":\"0x0\"," +
                        "\"totalDifficulty\":\"0x0\"," +
                        "\"extraData\":\"0x\"," +
                        "\"size\":\"0x258\"," +
                        "\"gasLimit\":\"0x4000000000000\"," +
                        "\"gasUsed\":\"0x0\"," +
                        "\"timestamp\":\"0x67bd02db\"," +
                        "\"transactions\":[]," +
                        "\"uncles\":[]," +
                        "\"baseFeePerGas\":\"0x3b9aca00\"," +
                        "\"prevRandao\":\"0x0000000000000000000000000000000000000000000000000000000000000000\"" +
                    "}," +
                    "\"id\":2" +
                "}," +
                "{" +
                    "\"jsonrpc\":\"2.0\"," +
                    "\"result\":\"0x0\"," +
                    "\"id\":3" +
                "}" +
            "]");

            // Act
            var (result, error) = await _rpc.SendBatchAsync<BigInteger, Block, BigInteger>(
                RpcClient.CreateBalanceRequest(ADDRESS) with { Id = 1 },
                RpcClient.CreateBlockByNumberRequest() with { Id = 2 },
                RpcClient.CreateMaxPriorityFeePerGasRequest() with { Id = 3 });

            var ((balance, balanceError), (block, blockError), (fee, feeError)) = result;

            // Assert
            Assert.Null(error);
            Assert.Null(balanceError);
            Assert.Null(blockError);
            Assert.Null(feeError);
            Assert.NotNull(block);
            Assert.Equal(0x16345785d8a0000, balance);
            Assert.Equal("0x672fa9", block.Number);
            Assert.Equal("0x11b04bdaa77ceeb62cd622a1b12f423064673a1d3d9641bd1d42c238f8e64b34", block.Hash);
            Assert.Equal(0x0, fee);
            VerifyRequestSent();
        }

        [Fact()]
        public async Task Test_RpcClient_GetBalance_Fail()
        {
            // Arrange
            SetupNetworkError();

            // Act
            var (_, error) = await _rpc.GetBalanceAsync(ADDRESS);

            // Assert
            Assert.NotNull(error);
            Assert.Equal(RpcClient.HTTP_REQUEST_ERROR, error.Code);
            VerifyRequestSent();
        }

        [Fact]
        public async Task Test_RpcClient_GetFeePerGasBatch()
        {
            SetupJsonRpcResponse(
            "[" +
                "{" +
                    "\"jsonrpc\":\"2.0\"," +
                    "\"id\":1," +
                    "\"result\":\"0x3b3847a40\"" +
                "}," +
                "{" +
                    "\"jsonrpc\":\"2.0\"," +
                    "\"id\":2," +
                    "\"result\":{" +
                        "\"parentHash\":\"0xc0188b85ff8b95486e6caec6e6859b8b1e4ab44e58598b970fa4405922eb2eca\"," +
                        "\"sha3Uncles\":\"0x1dcc4de8dec75d7aab85b567b6ccd41ad312451b948a7413f0a142fd40d49347\"," +
                        "\"miner\":\"0x0000000000000000000000000000000000000000\"," +
                        "\"stateRoot\":\"0x76f1ff886fdc3b8bfa595c2e486c2f4ba66f07243635fcd48adb6b809e820b17\"," +
                        "\"transactionsRoot\":\"0x68c4ebe76e0491ecbc2e1018638b2bce3a2a086e1137b2b7309678e48249e785\"," +
                        "\"receiptsRoot\":\"0x2f6abd03224f584d5f5ee4301dd9416f59ee6b5cf515ae64087e01c4ada5be0f\"," +
                        "\"logsBloom\":\"0x01000000000000400080040404002000002000210000080000000800000000000000000000001000101002800000000060010000000028200000000000000000000000000000000900000008010008000000000000001400000004001002000040080000000040000000000020000000084000000008000200000010000810040000010000000000004401400010800000800010024200000000000008000000000000000000000000000000000000100000800040000000000000004028000000000022000000820000000000000000000000804001000000000400000000002000000000004000000000000000000040000000000000000200000000000400\"," +
                        "\"difficulty\":\"0x0\"," +
                        "\"number\":\"0xc4e236\"," +
                        "\"gasLimit\":\"0x12a05f200\"," +
                        "\"gasUsed\":\"0x3c6065\"," +
                        "\"timestamp\":\"0x67cf3762\"," +
                        "\"timestampNano\":\"0x182b86549f0a128c\"," +
                        "\"extraData\":\"0x3662be8c00000000125cd890\"," +
                        "\"mixHash\":\"0xbf6f4d32aa0680b1a397342d5c48a1b7c608df11ed9fc5736a21cbd24f126081\"," +
                        "\"nonce\":\"0x0000000000000000\"," +
                        "\"baseFeePerGas\":\"0xba43b7400\"," +
                        "\"hash\":\"0x01ea60f699b3c138bccb932e9e3a683da0e9302653510b80430d88a09422acf5\"," +
                        "\"epoch\":\"0x3a52\"," +
                        "\"totalDifficulty\":\"0x0\"," +
                        "\"withdrawalsRoot\":\"0x56e81f171bcc55a6ff8345e692c0f86e5b48e01b996cadc001622fb5e363b421\"," +
                        "\"blobGasUsed\":\"0x0\"," +
                        "\"excessBlobGas\":\"0x0\"," +
                        "\"transactions\":[" +
                            "\"0xbd5171aa2dcedca9602ef58dec643525c3b0615a12c0646765487f3de19e357c\"," +
                            "\"0x3d576d04ff6228c30f482983a5517b3f99199db6ce344c81a5d49e8ffb289732\"," +
                            "\"0xd5e98cba0e043a25c47b1e228bf40cb7f354b983efc7e39f70477cca88f1b504\"," +
                            "\"0x214364f1bf194502e055c826843e2e90087aa1f8c76cc6c13c269fe59afbb1d2\"," +
                            "\"0x251cf4bfc70f7b3fb4d5875dbd99f24b983fcf55cbe663eeb55a8b03587bf859\"," +
                            "\"0x4c4728518715dbe8fc6fe45ceaccb246194d658fd0978b42d9227f3b3ad3e8d8\"," +
                            "\"0x31a3352936f1c9671b04c4736c5b244204f9f226a5c1357c620a77f5c332697f\"," +
                            "\"0x3843920e6f5a56cb5ecb8f891f7474394effa70a02fb5aff80c6f9ce793e67f9\"," +
                            "\"0x38404535902ef970f7954d84a79c173f6c2b85a4b77128004ac37f7b48803eb1\"," +
                            "\"0x38620249685e1e769e453a7b20105b9b5ae782334f1f90efab542db001dfc340\"," +
                            "\"0x38051f8fdc523903866e696db6ca48d9c3908e2e43280c1ab1e52938f53cb0f7\"," +
                            "\"0x3823ce77a9ebd138e55967e32b89a819426e900f3fb95230f1acea8720c84d63\"," +
                            "\"0x38ef22da2342d9e0f8ec4e7c34c108cf75db2a622db8cd68975f3011028b6771\"," +
                            "\"0x389f10a3dd6d3a0b0117e0b036c75d2769e2c99985b8cdea87522b48bd0de354\"," +
                            "\"0x3883c4b25d47bfaea22f9da1dcf564208bac8c04b432b375b0e4a633a85df061\"," +
                            "\"0x1f2d409e743286d8fa7655daf3bb6baa42622f6d07ee76b1f15b048b4f766c41\"," +
                            "\"0x7e6f48f0c8d84c2411974f89379c72bcb850ad9723a45d105380aef1acb2580f\"," +
                            "\"0xc5de9a88725302ca22637c6f9b4071b57bda2ad7ab43d48d7e6dfc64f6440768\"," +
                            "\"0x409f3bdaecb02f6fb585cfd340bd23589e726850548d3faa28f17f20d0d06137\"," +
                            "\"0x87bd045094ad530da3bdb059cab62e6cd010b436275aa446b0519d16f1c97728\"" +
                        "]," +
                        "\"size\":\"0x2e78\"," +
                        "\"uncles\":[]" +
                    "}" +
                "}" +
            "]");

            var (((maxPriorityFeePerGas, feeError), (block, blockError)), error) =
                await _rpc.SendBatchAsync<BigInteger, LightBlock>(
                    RpcClient.CreateMaxPriorityFeePerGasRequest() with { Id = 1 },
                    RpcClient.CreateBlockByNumberRequest(BlockNumber.Pending, includeTransactions: false) with { Id = 2 });

            Assert.Null(error);
            Assert.Null(feeError);
            Assert.Null(blockError);
            Assert.NotNull(block);
            Assert.Equal(0x3b3847a40, maxPriorityFeePerGas);
            Assert.Equal("0xba43b7400", block.BaseFeePerGas);
            VerifyRequestSent();
        }

        [Fact]
        public async Task Test_RpcClient_EstimateGas_Fail()
        {
            SetupJsonRpcResponse(
            "{" +
                "\"jsonrpc\":\"2.0\"," +
                "\"id\":1," +
                "\"error\":{" +
                    "\"code\":3," +
                    "\"message\":\"execution reverted\"," +
                    "\"data\":\"0xee90c468\"" +
                "}" +
            "}");

            var (result, error) = await _rpc.EstimateGasAsync(
                to: "0xbAF872cE876EF3551A1623F966e097dfc91eaba0",
                from: "0xd8dA6BF26964aF9D7eEd9e03E53415D37aA96045",
                data: "0x8456cb59");

            Assert.NotNull(error);
            Assert.Equal(3, error.Code);
            Assert.Equal("Message: execution reverted, Data: 0xee90c468", error.Message);
            VerifyRequestSent();
        }

        [Fact]
        public async Task Test_RpcClient_EstimateGas_FailWithoutData()
        {
            SetupJsonRpcResponse(
            "{" +
                "\"jsonrpc\":\"2.0\"," +
                "\"id\":1," +
                "\"error\":{" +
                    "\"code\":-32602," +
                    "\"message\":\"invalid argument 0: json: cannot unmarshal hex string of odd length into Go struct field TransactionArgs.to of type common.Address\"" +
                "}" +
            "}");

            var (result, error) = await _rpc.EstimateGasAsync(
                to: "0xbAF872cE876EF3551A1623F966e097dfc91eaba",
                from: "0xd8dA6BF26964aF9D7eEd9e03E53415D37aA96045",
                data: "0x8456cb59");

            Assert.NotNull(error);
            Assert.Equal(-32602, error.Code);
            Assert.Equal("Message: invalid argument 0: json: cannot unmarshal hex string of odd length into Go struct field TransactionArgs.to of type common.Address", error.Message);
            VerifyRequestSent();
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

        private void VerifyRequestSent()
        {
            _handler
                .Protected()
                .Verify(
                    "SendAsync",
                    Times.Once(),
                    ItExpr.Is<HttpRequestMessage>(req =>
                        req.Method == HttpMethod.Post &&
                        req.RequestUri!.ToString() == RPC_URL),
                    ItExpr.IsAny<CancellationToken>());
        }
    }
}
