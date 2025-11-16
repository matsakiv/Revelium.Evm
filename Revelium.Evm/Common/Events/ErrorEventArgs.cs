using Incendium;
using System;

namespace Revelium.Evm.Common.Events;

public class ErrorEventArgs : EventArgs
{
    public Error Error { get; init; } = default!;
}
