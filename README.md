# Revelium.Evm
[![License: MIT](https://img.shields.io/github/license/matsakiv/Revelium.Evm)](https://opensource.org/licenses/MIT) ![NuGet Version](https://img.shields.io/nuget/v/Revelium.Evm) ![NuGet Downloads](https://img.shields.io/nuget/dt/Revelium.Evm)

.NET Standard 2.1 integration library for EVM-compatible networks, aimed primarily at creating transaction-intensive applications (trading bots, etc.).

## Features

- Creating, signing and sending EVM transactions (Legacy and EIP-1559)
- `NonceManager` for offline nonce management — send transactions without waiting for confirmations
- `RpcClient` with built-in RPC call batching, rate limiting and retry strategies
- `ParallelRpcClient` for failover and load distribution across multiple RPC endpoints
- `GasStation` for automatic `BaseFeePerGas` and `MaxPriorityFeePerGas` updates
- `BlockScoutApi` for BlockScout explorer integration

## Getting started

### Installation

`PM> Install-Package Revelium.Evm`

### Create, sign and send transaction

Create a wallet and signer:
```cs
var key = EthECKey.GenerateKey();
var signer = new EthEcdsaSigner(key);
var fromAddress = signer.GetAddress();
```

Create an RPC client:
```cs
var rpc = new RpcClient(url: RpcUrl.ETHERLINK_GHOSTNET);
```

Create and send the transaction:
```cs
var tx = new TransactionLegacyRequest
{
    From = fromAddress,
    To = "0xdac17f958d2ee523a2206206994597c13d831ec7",
    GasPrice = 100000000,
    EstimateGas = true,
    Nonce = 0,
    Data = Approve.GetData(
        contractAddress: "0xdac17f958d2ee523a2206206994597c13d831ec7",
        spender: "0xd8dA6BF26964aF9D7eEd9e03E53415D37aA96045",
        value: 1000000000000)
};

var (txId, error) = await rpc.SignAndSendTransactionAsync(
    tx: tx,
    signer: signer);
```

### Using NonceManager

`NonceManager` tracks nonces per address, allowing multiple transactions to be sent without waiting for confirmations:
```cs
var nonceManager = new NonceManager(
    new NonceManagerOptions { Addresses = [fromAddress] },
    rpc);

await nonceManager.StartAsync(cancellationToken);
```

With `NonceManager`, you don't need to set `tx.Nonce` manually — it will be acquired automatically under a lock:
```cs
var tx = new TransactionLegacyRequest
{
    From = fromAddress,
    To = "0xdac17f958d2ee523a2206206994597c13d831ec7",
    GasPrice = 100000000,
    EstimateGas = true,
    Data = Approve.GetData(
        contractAddress: "0xdac17f958d2ee523a2206206994597c13d831ec7",
        spender: "0xd8dA6BF26964aF9D7eEd9e03E53415D37aA96045",
        value: 1000000000000)
};

var (txId, error) = await rpc.SignAndSendTransactionAsync(
    tx: tx,
    signer: signer,
    nonceManager: nonceManager);
```

### Send transaction with current block (batch)

To get the current block at the exact moment of sending (useful for timing measurements), use the batch method that sends `eth_sendRawTransaction` and `eth_getBlockByNumber` in a single RPC request:
```cs
var (result, error) = await rpc.SignAndSendTransactionWithBlockAsync(
    tx: tx,
    signer: signer,
    nonceManager: nonceManager);

if (error == null)
{
    var txId = result.TxId;
    var blockNumber = result.Block.GetBlockNumber();
}
```

### Manual transaction flow

For more control over individual steps:
```cs
// 1. Acquire nonce lock
using var nonceLock = await nonceManager.LockAsync(fromAddress);
var (nonce, nonceError) = await nonceLock.GetNonceAsync(cancellationToken);

// 2. Create transaction
var tx = new TransactionLegacyRequest
{
    From = fromAddress,
    To = "0xdac17f958d2ee523a2206206994597c13d831ec7",
    GasPrice = 100000000,
    GasLimit = 100000,
    Nonce = nonce,
    Data = Approve.GetData(
        contractAddress: "0xdac17f958d2ee523a2206206994597c13d831ec7",
        spender: "0xd8dA6BF26964aF9D7eEd9e03E53415D37aA96045",
        value: 1000000000000)
};

// 3. Sign and verify
signer.Sign(tx);

if (!tx.Verify())
    nonceLock.Reset(nonce);

// 4. Send
var (txId, error) = await rpc.SendTransactionAsync(tx);

if (error != null)
    nonceLock.Reset(nonce);

// nonceLock is released automatically via `using`
```

### RPC call batching

Send multiple RPC requests in a single HTTP call:
```cs
var (batchResult, error) = await rpc.SendBatchAsync<BigInteger, Block, BigInteger>(
    RpcClient.CreateBalanceRequest(address) with { Id = 1 },
    RpcClient.CreateBlockByNumberRequest() with { Id = 2 },
    RpcClient.CreateMaxPriorityFeePerGasRequest() with { Id = 3 });

var ((balance, balanceError), (block, blockError), (fee, feeError)) = batchResult;
```

> **Note**
> Each call in the batch can return an error independently — check each result.

Typed overloads are available for 2 and 3 requests. For larger batches or mixed return types, use `SendBatchAsync` with `IReadOnlyList<RpcRequest>` which returns `NullableResult<JsonDocument>[]`.

### BlockScout API

```cs
var api = new BlockScoutApi(BlockScoutApi.ETHERLINK_TESTNET);
var (tx, error) = await api.GetTransactionAsync(txId);
```
