using Incendium;
using System;

namespace Revelium.Evm.Common.Events
{
    public class ErrorCallEventArgs<TParameters> : EventArgs
    {
        public string CallId { get; init; } = default!;
        public TParameters Parameters { get; init; } = default!;
        public Error Error { get; init; } = default!;
    }
}
