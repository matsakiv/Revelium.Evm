using System.Numerics;

namespace Revelium.Evm.Common
{
    public static class BigIntegerExtensions
    {
        public static decimal Divide(this BigInteger value, BigInteger divisor)
        {
            var integerPart = BigInteger.DivRem(value, divisor, out var remainder);

            var result = (decimal)integerPart;

            if (remainder.IsZero)
                return result;

            return result + (decimal)remainder / (decimal)divisor;
        }
    }
}
