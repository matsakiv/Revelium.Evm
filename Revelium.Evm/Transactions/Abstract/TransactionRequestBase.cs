using Nethereum.Model;
using Nethereum.Signer;
using Nethereum.Util;
using Revelium.Evm.Common;
using System.Numerics;

namespace Revelium.Evm.Transactions.Abstract;

public abstract class TransactionRequestBase
{
    public string From { get; set; } = default!;
    public string To { get; set; } = default!;
    public BigInteger GasLimit { get; set; }
    public BigInteger Nonce { get; set; }
    public BigInteger Value { get; set; }
    public string? Data { get; set; }
    public BigInteger ChainId { get; set; }

    public byte[]? R { get; set; }
    public byte[]? S { get; set; }
    public byte[]? V { get; set; }

    public abstract SignedTransaction GetTransaction();

    public virtual byte[] GetRawHash() => new Sha3Keccack()
        .CalculateHash(GetTransaction().GetRLPEncodedRaw());

    public virtual string GetRlpEncoded() => GetTransaction()
        .GetRLPEncoded()
        .ToHexString();

    public virtual void SetSignature(ISignature signature) =>
        SetSignature(signature.R, signature.S, signature.V);

    public virtual void SetSignature(byte[] r, byte[] s, byte[] v)
    {
        R = r;
        S = s;
        V = v;
    }

    public virtual ISignature GetSignature() => GetEthEcdsaSignature();

    public virtual byte[] GetSignatureInDer() => GetEthEcdsaSignature().ToDER();

    public virtual bool Verify()
    {
        try
        {
            var rlp = GetRlpEncoded();

            return TransactionVerificationAndRecovery
                .VerifyTransaction(rlp);
        }
        catch
        {
            return false;
        }
    }

    private EthECDSASignature GetEthEcdsaSignature() =>
        EthECDSASignatureFactory.FromComponents(R, S, V);
}
