# Revelium.Evm
[![License: MIT](https://img.shields.io/github/license/matsakiv/Revelium.Evm)](https://opensource.org/licenses/MIT) ![NuGet Version](https://img.shields.io/nuget/v/Revelium.Evm) ![NuGet Downloads](https://img.shields.io/nuget/dt/Revelium.Evm)

Revelium.Evm is .NET standard 2.1 integration library for EVM-compatible networks.

## Getting started

### Installation

`PM> Install-Package Revelium.Evm`

### Create, sign and send transaction (long way)

Let's create new wallet and signer:
```cs
var key = EthECKey.GenerateKey();
var signer = new EthEcdsaSigner(key);
var fromAddress = signer.GetAddress();
```

To interact with the Etherlink test network, let's create an rpc client:
```cs
var rpc = new RpcClient(url: RpcClient.ETHERLINK_TESTNET);
```

Now we need a transaction counter for the nonce. To do this, we will use the nonce manager, which allows you to send transactions without waiting for confirmation of previous ones:
```cs
var (nonce, nonceError) = await NonceManager.Instance.GetNonceAsync(
    rpc: rpc,
    address: fromAddress,
    pending: true);

if (nonceError != null) { /* do something if necessary */ }
```

Let's create an Approve transaction for the Erc20 token:
```cs
var approve = new Approve
{
    FromAddress = fromAddress,
    GasPrice = 100_000_000,
    Gas = 1_000_000,
    Nonce = nonce,
    Spender = "<SPENDER_ADDRESS>",
    Value = 1_000_000_000_000

}.CreateTransactionInput("<TOKEN_CONTRACT_ADDRESS>");
```

We can also estimate gas usage:
```cs
var (estimatedGas, estimateGasError) = await rpc.EstimateGasAsync(
    block: BlockNumber.Latest,
    to: approve.To,
    from: approve.From,
    gasPrice: approve.GasPrice,
    value: 0,
    data: approve.Data);

if (estimateGasError == null)
    approve.Gas = new HexBigInteger(estimatedGas);
```

Everything is ready to sign the transaction, verify and send:
```cs
var request = new TransactionLegacyRequest(approve);

signer.Sign(request);

if (!request.Verify()) { /* do something if necessary */ }

var (txId, error) = await rpc.SendRawTransactionAsync(request);
```

And at the end we will receive the sent transaction via the BlockScout API:
```cs
var api = new BlockScoutApi(BlockScoutApi.ETHERLINK_TESTNET);
var tx = await api.GetTransactionAsync(txId);
```

### Create, sign and send transaction (short way)

The same thing can be done in a shorter way using a helper method:
```cs
var tx = new TransactionLegacyRequest
{
    From = fromAddress,
    To = "<TOKEN_CONTRACT_ADDRESS>",
    GasPrice = 100_000_000,
    Data = new Approve
    {
        Spender = "<SPENDER_ADDRESS>",
        Value = 1_000_000_000_000
    }.CreateTransactionInput("<TOKEN_CONTRACT_ADDRESS>").Data
};

var (txId, error) = await rpc.SignAndSendLegacyTransactionAsync(
    tx: tx,
    signer: signer,
    estimateGas: true,
    cancellationToken: cancellationToken);
```
