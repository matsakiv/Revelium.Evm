using Incendium;
using Revelium.Evm.Common.Events;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Revelium.Evm.Common
{
    public delegate Task<Result<TResult>> HandlerCallback<TParameters, TResult>(
        TParameters parameters,
        CancellationToken cancellationToken);

    public class BoundedCallSequencer<TParameters, TResult>(
        HandlerCallback<TParameters, TResult> handlerCallback,
        int capacity)
    {
        public const int PROCESS_QUEUE_ERROR = 1;

        public event EventHandler<SuccessCallEventArgs<TParameters, TResult>>? OnSuccess;
        public event EventHandler<ErrorEventArgs>? OnError;

        private class CallData
        {
            public string Id { get; set; } = default!;
            public TResult? Result { get; set; }
            public TParameters Parameters { get; set; } = default!;
            public Func<SuccessCallEventArgs<TParameters, TResult>, CancellationToken, Task>? OnSuccess { get; set; }
            public Func<ErrorCallEventArgs<TParameters>, CancellationToken, Task>? OnError { get; set; }
        }

        private readonly HandlerCallback<TParameters, TResult> _handlerCallback = handlerCallback
            ?? throw new ArgumentNullException(nameof(handlerCallback));
        private readonly AsyncQueue<CallData> _pendingQueue = new();
        private readonly BoundedAsyncQueue<CallData> _waitingQueue = new(capacity);
        private readonly SemaphoreSlim _sync = new(1);

        public int PendingQueueSize => _pendingQueue.Count;
        public int WaitingQueueSize => _waitingQueue.Count;
        public int Capacity => _waitingQueue.Capacity;

        public async Task<string> EnqueueAsync(
            TParameters parameters,
            Func<SuccessCallEventArgs<TParameters, TResult>, CancellationToken, Task>? onSuccess = null,
            Func<ErrorCallEventArgs<TParameters>, CancellationToken, Task>? onError = null,
            CancellationToken cancellationToken = default)
        {
            var callId = Guid.NewGuid().ToString();
            var callData = new CallData
            {
                Id = callId,
                Parameters = parameters,
                OnSuccess = onSuccess,
                OnError = onError
            };

            await _pendingQueue.EnqueueAsync(callData, cancellationToken);

            _ = RunQueueProcessingInBackground(cancellationToken);

            return callId;
        }

        public async Task<bool> TryCancelAsync(
            string callId,
            CancellationToken cancellationToken = default)
        {
            var result = await _pendingQueue.RemoveAsync(
                predicate: cd => cd.Id == callId,
                cancellationToken: cancellationToken);

            _ = RunQueueProcessingInBackground(cancellationToken);

            return result > 0;
        }

        public async Task<bool> CompleteAsync(
            string callId,
            CancellationToken cancellationToken = default)
        {
            var removed = await _waitingQueue.RemoveAsync(
                cd => cd.Id == callId,
                cancellationToken);

            return removed > 0;
        }

        private Task RunQueueProcessingInBackground(
            CancellationToken cancellationToken = default)
        {
            return Task.Run(() => ProcessQueueAsync(cancellationToken), cancellationToken);
        }

        private async Task ProcessQueueAsync(
            CancellationToken cancellationToken = default)
        {
            try
            {
                await _sync.WaitAsync(cancellationToken);

                while (true)
                {
                    if (!await _waitingQueue.WaitToEnqueueAsync(cancellationToken))
                    {
                        OnError?.Invoke(this, new ErrorEventArgs
                        {
                            Error = new Error(
                                code: PROCESS_QUEUE_ERROR,
                                message: "Wait to enqueue error")
                        });
                        return;
                    }

                    var (callData, isNotEmpty) = await _pendingQueue.TryDequeue(cancellationToken);

                    if (!isNotEmpty)
                        return; // queue is empty

                    // call handler
                    var (result, error) = await _handlerCallback(
                        parameters: callData.Parameters,
                        cancellationToken: cancellationToken);

                    if (error != null)
                    {
                        var handlerError = new Error(
                            code: PROCESS_QUEUE_ERROR,
                            message: "Handler callback error",
                            error: error);

                        if (callData.OnError != null)
                        {
                            await callData.OnError(
                                new ErrorCallEventArgs<TParameters>
                                {
                                    CallId = callData.Id,
                                    Parameters = callData.Parameters,
                                    Error = handlerError
                                },
                                cancellationToken);
                        }

                        OnError?.Invoke(this, new ErrorEventArgs
                        {
                            Error = handlerError
                        });

                        continue;
                    }

                    callData.Result = result;

                    await _waitingQueue.EnqueueAsync(callData, cancellationToken);

                    var successEventArgs = new SuccessCallEventArgs<TParameters, TResult>
                    {
                        CallId = callData.Id,
                        Parameters = callData.Parameters,
                        Result = callData.Result
                    };

                    if (callData.OnSuccess != null)
                    {
                        await callData.OnSuccess(successEventArgs, cancellationToken);
                    }

                    OnSuccess?.Invoke(this, successEventArgs);
                }
            }
            catch (OperationCanceledException)
            {
                // task canceled
            }
            catch (Exception e)
            {
                OnError?.Invoke(this, new ErrorEventArgs
                {
                    Error = new Error(
                        code: PROCESS_QUEUE_ERROR,
                        message: "Wait to enqueue error",
                        exception: e)
                });
            }
            finally
            {
                _sync.Release();
            }
        }
    }
}
