using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Revelium.Evm.Common
{
    /// <summary>
    /// Represents a thread-safe bounded queue with asynchronous operations.
    /// The queue has a fixed capacity and provides methods for asynchronous enqueuing and dequeuing of items.
    /// </summary>
    /// <typeparam name="T">The type of elements in the queue.</typeparam>
    public class BoundedAsyncQueue<T>(int capacity) : IDisposable
    {
        private readonly int _capacity = capacity;
        private readonly List<T> _data = [];
        private readonly SemaphoreSlim _sync = new(1);
        private readonly Queue<TaskCompletionSource<bool>> _completions = new();
        private bool _disposed;

        /// <summary>
        /// Gets the current number of items in the queue.
        /// </summary>
        public int Count => _data.Count;

        /// <summary>
        /// Gets a value indicating whether the queue is empty.
        /// </summary>
        public bool IsEmpty => _data.Count == 0;

        /// <summary>
        /// Gets the maximum capacity of the queue.
        /// </summary>
        public int Capacity => _capacity;

        /// <summary>
        /// Asynchronously adds an item to the queue. If the queue is full, waits until space becomes available.
        /// </summary>
        /// <param name="item">The item to add to the queue.</param>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>A task that completes when the item is added to the queue.</returns>
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

        /// <summary>
        /// Attempts to add an item to the queue without waiting. Returns false if the queue is full.
        /// </summary>
        /// <param name="item">The item to add to the queue.</param>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>True if the item was added; false if the queue was full or the operation was canceled.</returns>
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

        /// <summary>
        /// Waits until space becomes available in the queue.
        /// </summary>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>True if space became available; false if the operation was canceled.</returns>
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

        /// <summary>
        /// Attempts to remove and return an item from the beginning of the queue.
        /// </summary>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>A tuple containing the dequeued item and a boolean indicating success.</returns>
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

        /// <summary>
        /// Attempts to return the first item in the queue without removing it.
        /// </summary>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>A tuple containing the first item and a boolean indicating success.</returns>
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

        /// <summary>
        /// Removes all items that match the specified predicate.
        /// </summary>
        /// <param name="predicate">A function to test each element for a condition.</param>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>The number of elements removed from the queue.</returns>
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

        /// <summary>
        /// Releases all resources used by the queue.
        /// </summary>
        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
