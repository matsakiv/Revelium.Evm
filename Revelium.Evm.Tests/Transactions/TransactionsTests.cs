using Nethereum.Signer;
using Revelium.Evm.Crypto;
using Revelium.Evm.Common;
using Revelium.Evm.Rpc;

namespace Revelium.Evm.Transactions
{
    public class TransactionsTests
    {
        private const string ALICE_PRIVATE_KEY = "0x514575f3fb08a4aeee8ee51c332f766689cf1aedee163e33bb82088bfbcfb343";
        private const string BOB_PRIVATE_KEY = "0xb571f079ff5ae7a0aea7439e1667db7aaa0a70e324035414137b04a1769fed85";

        [Fact]
        public void Test_Transactions_CreateSignAndVerifyLegacyTransaction()
        {
            // arrange
            var aliceKey = new EthECKey(ALICE_PRIVATE_KEY);
            var aliceAddress = aliceKey.GetPublicAddress();

            var bobKey = new EthECKey(BOB_PRIVATE_KEY);
            var bobAddress = bobKey.GetPublicAddress();

            var tx = new TransactionLegacyRequest()
            {
                From = aliceAddress,
                To = bobAddress,
                GasLimit = 1_000_000,
                GasPrice = 1_000_000_000,
                Nonce = 1,
                Value = 1_000_000
            };

            var signer = new EthEcdsaSigner(aliceKey);
            var signature = signer.Sign(tx);

            // act
            var verifyResult1 = tx.Verify();
            var verifyResult2 = signer.Verify(tx);

            // asserts
            Assert.True(verifyResult1);
            Assert.True(verifyResult2);
            Assert.NotNull(signature);
        }

        [Fact]
        public void Test_Transactions_CreateSignAndVerifyEip1559Transaction()
        {
            // arrange
            var aliceKey = new EthECKey(ALICE_PRIVATE_KEY);
            var aliceAddress = aliceKey.GetPublicAddress();

            var bobKey = new EthECKey(BOB_PRIVATE_KEY);
            var bobAddress = bobKey.GetPublicAddress();

            var tx = new Transaction1559Request()
            {
                From = aliceAddress,
                To = bobAddress,
                GasLimit = 1_000_000,
                MaxFeePerGas = 12,
                MaxPriorityFeePerGas = 1,
                Nonce = 1,
                Value = 1_000_000,
                ChainId = RpcClient.ETHERLINK_TESTNET_CHAIN_ID
            };

            var signer = new Eip1559EcdsaSigner(aliceKey);
            var signature = signer.Sign(tx);

            // act
            var verifyResult1 = tx.Verify();
            var verifyResult2 = signer.Verify(tx);

            // asserts
            Assert.True(verifyResult1);
            Assert.True(verifyResult2);
            Assert.NotNull(signature);
        }
    }
}
