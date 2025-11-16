using Nethereum.Model;

namespace Revelium.Evm.Crypto.Abstract;

public interface ISigner
{
    ISignature Sign(byte[] hash);
    bool Verify(byte[] hash, byte[] signature);
    string GetAddress();
    byte[] GetPublicKey();
}
