using System;
using HexBouncyCastle = Org.BouncyCastle.Utilities.Encoders.Hex;

namespace Revelium.Evm.Common
{
    public static class Hex
    {
        public static byte[] FromString(string hex, bool prefixed = false) =>
            HexBouncyCastle.Decode(prefixed ? hex[2..] : hex);

        public static string ToHexString(this byte[] bytes, int offset, int count, bool lowerCase = true) => lowerCase
            ? HexBouncyCastle.ToHexString(bytes, offset, count).ToLowerInvariant()
            : HexBouncyCastle.ToHexString(bytes, offset, count);

        public static string ToHexString(this byte[] bytes, bool lowerCase = true) => lowerCase
            ? HexBouncyCastle.ToHexString(bytes).ToLowerInvariant()
            : HexBouncyCastle.ToHexString(bytes);

        public static string ToHexString(this ReadOnlySpan<byte> bytes, bool lowerCase = true) => lowerCase
            ? HexBouncyCastle.ToHexString(bytes.ToArray()).ToLowerInvariant()
            : HexBouncyCastle.ToHexString(bytes.ToArray());
    }
}
