# Revelium.Evm
[![License: MIT](https://img.shields.io/github/license/matsakiv/Revelium.Evm)](https://opensource.org/licenses/MIT) ![NuGet Version](https://img.shields.io/nuget/v/Revelium.Evm) ![NuGet Downloads](https://img.shields.io/nuget/dt/Revelium.Evm)

Revelium.Evm is .NET standard 2.1 integration library for EVM-compatible networks,  aimed primarily at creating transaction-intensive applications (trading bots, etc.).

In addition to the basic capabilities of creating, signing and sending EVM transactions (including EIP-1559), the library also contains:
- `NonceManager` to effective offline `Nonce` management and send transactions without waiting for confirmations;
- `RpcCallSequencer` to manage the queue of sent transactions. If the RPC has a limit on the number of transactions from one address, the class allows you to not exceed the limits, streamline sending, and allows you to cancel queued calls that have not yet been sent to the RPC;
- `RpcClient` with built-in support for RPC calls batching, limiting the number of requests per unit of time and requests retries in case of errors with support for various strategies;
- `BlockScoutApi` for BlockScout explorer.

## Getting started

### Installation

`PM> Install-Package Revelium.Evm`

### Create, sign and send transaction (short way)

Let's create new wallet and signer:
```cs
var key = EthECKey.GenerateKey();
var signer = new EthEcdsaSigner(key);
var fromAddress = signer.GetAddress();
```

To interact with the Etherlink test network, let's create an RPC client:
```cs
var rpc = new RpcClient(url: RpcUrl.ETHERLINK_GHOSTNET);
```

Now we are ready to create and send the transaction:
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

And finally, as an example, we will receive the submitted transaction via the BlockScout API:
```cs
var api = new BlockScoutApi(BlockScoutApi.ETHERLINK_TESTNET);
var tx = await api.GetTransactionAsync(txId);
```

### Create, sign and send transaction (detailed way)

Let's look at a more detailed and low-level transaction creation:

First of all, we need a transaction counter for the nonce. To do this, we will use the `NonceManager`, which allows you to send transactions without waiting for confirmation of previous ones:
```cs
using var nonceLock = await NonceManager.LockAsync(fromAddress);

var (nonce, nonceError) = await nonceLock.GetNonceAsync(
    rpc: rpc,
    pending: true);

if (nonceError != null) { /* do something if necessary */ }
```

Let's create an Approve transaction for the ERC-20 token:
```cs
var approve = new Approve
{
    FromAddress = fromAddress,
    GasPrice = 100_000_000,
    Gas = 1_000_000,
    Nonce = nonceLock.Nonce,
    Spender = "<SPENDER_ADDRESS>",
    Value = 1_000_000_000_000
};

var approveInput = approve.CreateTransactionInput("<TOKEN_CONTRACT_ADDRESS>").Data;
```

We can also estimate gas usage:
```cs
var (estimatedGas, estimateGasError) = await rpc.EstimateGasAsync(
    block: BlockNumber.Latest,
    to: approve.To,
    from: approve.From,
    gasPrice: approve.GasPrice,
    value: 0,
    data: approveInput);

if (estimateGasError == null)
    approve.Gas = new HexBigInteger(estimatedGas);
```

Everything is ready to sign, verify and send the transaction:
```cs
var request = new TransactionLegacyRequest(approve);

signer.Sign(request);

if (!request.Verify()) { /* do something if necessary */ }

var (txId, error) = await rpc.SendRawTransactionAsync(request);
```

Finally, before disposing the nonce lock, we need to reset the nonce in case of error:
```cs
if (error != null)
    nonceLock.Reset();

// NonceLock automatically disposes when you use `using` statement
// nonceLock.Dispose();
```

### RPC call batching

Let's create a batch of requests:
```cs
var (batchResult, error) = await _rpc.SendBatchAsync<BigInteger, Block, BigInteger>(
    _rpc.CreateBalanceRequest(ADDRESS) with { Id = 1 },
    _rpc.CreateBlockByNumberRequest() with { Id = 2 },
    _rpc.CreateMaxPriorityFeePerGasRequest() with { Id = 3 });

// use destructuring to get the results
var ((balance, balanceError), (block, blockError), (fee, feeError)) = batchResult;
```

> **Note**
> Every call in the batch can return an error, so you need to check each result.

> For convenience, there are methods for sending two and three requests with the ability to get results as a tuple of specific types. For all other cases, you can currently use the `SendBatchAsync` method, which returns an array of `NullableResult<JsonDocument>[]` if the return types are different or unknown, or use the `SendBatchAsync<T>` method, which returns an array of `NullableResult<T>[]` if the return types are known and the same.
