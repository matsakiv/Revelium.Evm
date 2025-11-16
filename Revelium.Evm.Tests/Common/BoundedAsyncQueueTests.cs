namespace Revelium.Evm.Common;

public class BoundedAsyncQueueTests
{
    [Fact]
    public async Task Test_BoundedAsyncQueue_TryEnqueue()
    {
        // arrange
        var queue = new BoundedAsyncQueue<int>(capacity: 3);

        // act
        var enqueueResult1 = await queue.TryEnqueueAsync(1);
        var enqueueResult2 = await queue.TryEnqueueAsync(2);
        var enqueueResult3 = await queue.TryEnqueueAsync(3);
        var enqueueResult4 = await queue.TryEnqueueAsync(4);
        var (value, dequeueResult) = await queue.TryDequeue();
        var enqueueResult5 = await queue.TryEnqueueAsync(4);

        // asserts
        Assert.True(enqueueResult1);
        Assert.True(enqueueResult2);
        Assert.True(enqueueResult3);
        Assert.False(enqueueResult4);
        Assert.True(dequeueResult);
        Assert.True(enqueueResult5);
        Assert.Equal(1, value);
    }

    [Fact]
    public async Task Test_BoundedAsyncQueue_TryDequeue()
    {
        // arrange
        var queue = new BoundedAsyncQueue<int>(capacity: 3);

        await queue.TryEnqueueAsync(1);
        await queue.TryEnqueueAsync(2);
        await queue.TryEnqueueAsync(3);

        // act
        var (value1, dequeueResult1) = await queue.TryDequeue();
        var (value2, dequeueResult2) = await queue.TryDequeue();
        var (value3, dequeueResult3) = await queue.TryDequeue();

        // asserts
        Assert.True(dequeueResult1);
        Assert.True(dequeueResult2);
        Assert.True(dequeueResult3);
        Assert.Equal(1, value1);
        Assert.Equal(2, value2);
        Assert.Equal(3, value3);
        Assert.Equal(0, queue.Count);
    }

    [Fact]
    public async Task Test_BoundedAsyncQueue_Enqueue()
    {
        // arrange
        var queue = new BoundedAsyncQueue<int>(capacity: 3);

        await queue.EnqueueAsync(1);
        await queue.EnqueueAsync(2);
        await queue.EnqueueAsync(3);

        var enqueueTask = Task.Run(async () => await queue.EnqueueAsync(4));

        var queueSizeBeforeDequeue = queue.Count;
        var (value, dequeueResult) = await queue.TryDequeue();

        // act
        await enqueueTask;

        // asserts
        Assert.Equal(1, value);
        Assert.True(dequeueResult);
        Assert.Equal(3, queueSizeBeforeDequeue);
        Assert.Equal(2, (await queue.TryDequeue()).Item1);
        Assert.Equal(3, (await queue.TryDequeue()).Item1);
        Assert.Equal(4, (await queue.TryDequeue()).Item1);
    }

    [Fact]
    public async Task Test_BoundedAsyncQueue_WaitToEnqueue()
    {
        // arrange
        var queue = new BoundedAsyncQueue<int>(capacity: 3);

        var waitResult1 = await queue.WaitToEnqueueAsync();
        await queue.EnqueueAsync(1);
        var waitResult2 = await queue.WaitToEnqueueAsync();
        await queue.EnqueueAsync(2);
        var waitResult3 = await queue.WaitToEnqueueAsync();
        await queue.EnqueueAsync(3);

        var waitTask = Task.Run(async () => await queue.WaitToEnqueueAsync());

        await Task.Delay(100);
        var isCompletedBeforeDequeue = waitTask.IsCompletedSuccessfully;

        await queue.TryDequeue();

        // act
        var waitResult4 = await waitTask;

        // asserts
        Assert.True(waitResult1);
        Assert.True(waitResult2);
        Assert.True(waitResult3);
        Assert.True(waitResult4);
        Assert.False(isCompletedBeforeDequeue);
        Assert.True(waitTask.IsCompletedSuccessfully);
    }

    [Fact]
    public async Task Test_BoundedAsyncQueue_Remove()
    {
        // arrange
        var queue = new BoundedAsyncQueue<int>(capacity: 5);

        await queue.EnqueueAsync(1);
        await queue.EnqueueAsync(2);
        await queue.EnqueueAsync(3);
        await queue.EnqueueAsync(2);
        await queue.EnqueueAsync(2);

        // act
        var removed = await queue.RemoveAsync(i => i == 2);

        // asserts
        Assert.Equal(3, removed);
        Assert.Equal(2, queue.Count);
        Assert.Equal(1, (await queue.TryDequeue()).Item1);
        Assert.Equal(3, (await queue.TryDequeue()).Item1);
        Assert.True(queue.IsEmpty);
    }
}
