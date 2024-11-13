using Nethereum.Model;
using Nethereum.Signer;

namespace Revelium.Evm.Crypto
{
    public class Eip1559EcdsaSigner(EthECKey key) : EthEcdsaSigner(key)
    {
        public override ISignature Sign(byte[] hash) =>
            _key.SignAndCalculateYParityV(hash);
    }
}
