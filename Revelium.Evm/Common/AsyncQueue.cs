using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Revelium.Evm.Common
{
    public sealed class AsyncQueue<T> : IDisposable
    {
        private readonly List<T> _data;
        private readonly SemaphoreSlim _sync;
        private bool _disposed;

        public int Count => _data.Count;
        public bool IsEmpty => _data.Count == 0;

        public AsyncQueue()
        {
            _data = [];
            _sync = new SemaphoreSlim(1);
        }

        public async Task EnqueueAsync(T item, CancellationToken cancellationToken = default)
        {
            try
            {
                await _sync.WaitAsync(cancellationToken);

                _data.Add(item);
            }
            finally
            {
                _sync.Release();
            }
        }

        public async Task<(T, bool)> TryDequeue(CancellationToken cancellationToken = default)
        {
            try
            {
                await _sync.WaitAsync(cancellationToken);

                if (_data.Count == 0)
                    return (default!, false);

                var item = _data[0];
                _data.RemoveAt(0);

                return (item, true);
            }
            finally
            {
                _sync.Release();
            }
        }

        public async Task<(T, bool)> TryPeek(CancellationToken cancellationToken = default)
        {
            try
            {
                await _sync.WaitAsync(cancellationToken);

                if (_data.Count == 0)
                    return (default!, false);

                return (_data[0], true);
            }
            finally
            {
                _sync.Release();
            }
        }

        public async Task<int> RemoveAsync(Predicate<T> predicate, CancellationToken cancellationToken = default)
        {
            try
            {
                var result = 0;

                await _sync.WaitAsync(cancellationToken);

                for (var i = 0; i < _data.Count;)
                {
                    if (predicate(_data[i]))
                    {
                        _data.RemoveAt(i);
                        result++;
                    }
                    else i++;
                }

                return result;
            }
            finally
            {
                _sync.Release();
            }
        }

        private void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _sync?.Dispose();
                }

                _disposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
