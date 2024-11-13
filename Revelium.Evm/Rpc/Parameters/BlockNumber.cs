using Nethereum.Hex.HexTypes;
using System;

namespace Revelium.Evm.Rpc.Parameters
{
    public enum BlockNumberType
    {
        Value,
        Earliest,
        Latest,
        Safe,
        Finalized,
        Pending
    }

    public readonly struct BlockNumber(BlockNumberType type, ulong blockNumber = 0)
    {
        public string Value { get; init; } = type switch
        {
            BlockNumberType.Value => new HexBigInteger(blockNumber).ToString(),
            BlockNumberType.Earliest => "earliest",
            BlockNumberType.Latest => "latest",
            BlockNumberType.Safe => "safe",
            BlockNumberType.Finalized => "finalized",
            BlockNumberType.Pending => "pending",
            _ => throw new NotImplementedException(),
        };

        public static BlockNumber FromValue(ulong value) => new(BlockNumberType.Value, value);
        public static BlockNumber Earliest => new(BlockNumberType.Earliest);
        public static BlockNumber Latest => new(BlockNumberType.Latest);
        public static BlockNumber Safe => new(BlockNumberType.Safe);
        public static BlockNumber Finalized => new(BlockNumberType.Finalized);
        public static BlockNumber Pending => new(BlockNumberType.Pending);
    }
}
