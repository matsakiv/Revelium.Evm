using System;

namespace Revelium.Evm.Common.Events
{
    public class SuccessCallEventArgs<TParameters, TResult> : EventArgs
    {
        public string CallId { get; init; } = default!;
        public TParameters Parameters { get; init; } = default!;
        public TResult Result { get; init; } = default!;
    }
}
