using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Revelium.Evm.Common
{
    public class BoundedAsyncQueue<T>(int capacity) : IDisposable
    {
        private readonly int _capacity = capacity;
        private readonly List<T> _data = [];
        private readonly SemaphoreSlim _sync = new(1);
        private readonly Queue<TaskCompletionSource<bool>> _completions = new();
        private bool _disposed;

        public int Count => _data.Count;
        public bool IsEmpty => _data.Count == 0;
        public int Capacity => _capacity;

        public async Task EnqueueAsync(
            T item,
            CancellationToken cancellationToken = default)
        {
            while (true)
            {
                var waitingTask = await EnqueueOrGetWaiterAsync(item, cancellationToken);

                if (waitingTask == null)
                    return;

                await waitingTask;
            }

            async Task<Task?> EnqueueOrGetWaiterAsync(T item, CancellationToken cancellationToken = default)
            {
                try
                {
                    await _sync.WaitAsync(cancellationToken);

                    if (_data.Count < _capacity)
                    {
                        _data.Add(item);
                        return null;
                    }
                    else
                    {
                        var tcs = new TaskCompletionSource<bool>();
                        _completions.Enqueue(tcs);

                        return tcs.Task;
                    }
                }
                finally
                {
                    _sync.Release();
                }
            }
        }

        public async Task<bool> TryEnqueueAsync(
            T item,
            CancellationToken cancellationToken = default)
        {
            try
            {
                await _sync.WaitAsync(cancellationToken);

                if (_data.Count >= _capacity)
                    return false;

                _data.Add(item);
                return true;
            }
            catch (OperationCanceledException)
            {
                return false;
            }
            finally
            {
                _sync.Release();
            }
        }

        public async Task<bool> WaitToEnqueueAsync(
            CancellationToken cancellationToken = default)
        {
            try
            {
                var waitingTask = await CanEnqueueOrGetWaiterAsync(cancellationToken);

                if (waitingTask != null)
                    await waitingTask;

                return true;
            }
            catch (OperationCanceledException)
            {
                return false;
            }

            async Task<Task?> CanEnqueueOrGetWaiterAsync(CancellationToken cancellationToken = default)
            {
                try
                {
                    await _sync.WaitAsync(cancellationToken);

                    if (_data.Count < _capacity)
                    {
                        return null;
                    }
                    else
                    {
                        var tcs = new TaskCompletionSource<bool>();
                        _completions.Enqueue(tcs);

                        return tcs.Task;
                    }
                }
                finally
                {
                    _sync.Release();
                }
            }
        }

        public async Task<(T, bool)> TryDequeue(
            CancellationToken cancellationToken = default)
        {
            try
            {
                await _sync.WaitAsync(cancellationToken);

                if (_data.Count == 0)
                    return (default!, false);

                var item = _data[0];
                _data.RemoveAt(0);

                if (_completions.TryDequeue(out var tcs))
                    tcs.SetResult(true);

                return (item, true);
            }
            finally
            {
                _sync.Release();
            }
        }

        public async Task<(T, bool)> TryPeek(
            CancellationToken cancellationToken = default)
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

        public async Task<int> RemoveAsync(
            Predicate<T> predicate,
            CancellationToken cancellationToken = default)
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

                        if (_completions.TryDequeue(out var tcs))
                            tcs.SetResult(true);
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
