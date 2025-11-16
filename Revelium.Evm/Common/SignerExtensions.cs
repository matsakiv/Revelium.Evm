using Nethereum.Model;
using Revelium.Evm.Crypto.Abstract;
using Revelium.Evm.Transactions.Abstract;

namespace Revelium.Evm.Common;

public static class SignerExtensions
{
    public static ISignature Sign(
        this ISigner signer,
        TransactionRequestBase request)
    {
        var rawHash = request.GetRawHash();
        var signature = signer.Sign(rawHash);

        request.SetSignature(signature);

        return signature;
    }

    public static bool Verify(
        this ISigner signer,
        TransactionRequestBase request)
    {
        return signer.Verify(request.GetRawHash(), request.GetSignatureInDer());
    }
}
