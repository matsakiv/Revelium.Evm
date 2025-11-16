using Nethereum.Model;
using Nethereum.Signer;
using Revelium.Evm.Crypto.Abstract;
using System;

namespace Revelium.Evm.Crypto;

public class EthEcdsaSigner(EthECKey key) : ISigner
{
    protected readonly EthECKey _key = key ?? throw new ArgumentNullException(nameof(key));

    public string GetAddress() => _key.GetPublicAddress();

    public byte[] GetPublicKey() => _key.GetPubKey();

    public virtual ISignature Sign(byte[] hash) =>
        _key.SignAndCalculateV(hash);

    public virtual bool Verify(
        byte[] hash,
        byte[] signature) => _key.VerifyAllowingOnlyLowS(
            hash,
            EthECDSASignature.FromDER(signature));
}
