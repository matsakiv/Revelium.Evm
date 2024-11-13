using Incendium;

namespace Revelium.Evm.Common
{
    public class BoundedCallSequencerTests
    {
        [Fact]
        public async Task Test_BoundedCallSequencer_Complete()
        {
            // arrange
            var capacity = 3;
            var results = new List<int>();

            var sequencer = new BoundedCallSequencer<int, int>(
                handlerCallback: (p, c) =>
                {
                    results.Add(p);
                    return Task.FromResult<Result<int>>(p);
                },
                capacity: capacity);

            var callId1 = await sequencer.EnqueueAsync(1);
            var callId2 = await sequencer.EnqueueAsync(2);
            var callId3 = await sequencer.EnqueueAsync(3);
            var callId4 = await sequencer.EnqueueAsync(4);

            while (sequencer.PendingQueueSize != 1)
                await Task.Delay(1);

            while (sequencer.WaitingQueueSize != 3)
                await Task.Delay(1);

            var pendingQueueSize = sequencer.PendingQueueSize;
            var waitingQueueSize = sequencer.WaitingQueueSize;

            var completeResult1 = await sequencer.CompleteAsync(callId1);

            while (sequencer.WaitingQueueSize != 3)
                await Task.Delay(1);

            // act
            var completeResult4 = await sequencer.CompleteAsync(callId4);
            var completeResult3 = await sequencer.CompleteAsync(callId3);
            var completeResult2 = await sequencer.CompleteAsync(callId2);

            // asserts
            Assert.Equal(1, pendingQueueSize);
            Assert.Equal(3, waitingQueueSize);
            Assert.True(completeResult1);
            Assert.True(completeResult2);
            Assert.True(completeResult3);
            Assert.True(completeResult4);
            Assert.Equal(0, sequencer.PendingQueueSize);
            Assert.Equal(0, sequencer.WaitingQueueSize);
            Assert.Equal(1, results[0]);
            Assert.Equal(2, results[1]);
            Assert.Equal(3, results[2]);
            Assert.Equal(4, results[3]);
        }

        [Fact]
        public async Task Test_BoundedCallSequencer_CantCompleteTwice()
        {
            // arrange
            var capacity = 3;

            var sequencer = new BoundedCallSequencer<int, int>(
                handlerCallback: (p, c) => Task.FromResult<Result<int>>(p),
                capacity: capacity);

            var callId1 = await sequencer.EnqueueAsync(1);

            while (sequencer.PendingQueueSize > 0)
                await Task.Delay(1);

            while (sequencer.WaitingQueueSize == 0)
                await Task.Delay(1);

            // act
            var completeResult1 = await sequencer.CompleteAsync(callId1);
            var completeResult2 = await sequencer.CompleteAsync(callId1);

            // asserts
            Assert.True(completeResult1);
            Assert.False(completeResult2);
        }

        [Fact]
        public async Task Test_BoundedCallSequencer_TryCancel()
        {
            // arrange
            var capacity = 2;

            var sequencer = new BoundedCallSequencer<int, int>(
                handlerCallback: (p, c) => Task.FromResult<Result<int>>(p),
                capacity: capacity);

            var callId1 = await sequencer.EnqueueAsync(1);
            var callId2 = await sequencer.EnqueueAsync(2);
            var callId3 = await sequencer.EnqueueAsync(3);
            var callId4 = await sequencer.EnqueueAsync(4);

            while (sequencer.PendingQueueSize != 2)
                await Task.Delay(1);

            while (sequencer.WaitingQueueSize != 2)
                await Task.Delay(1);

            // act
            var cancelResult1 = await sequencer.TryCancelAsync(callId1);
            var cancelResult2 = await sequencer.TryCancelAsync(callId2);
            var cancelResult3 = await sequencer.TryCancelAsync(callId3);
            var cancelResult4 = await sequencer.TryCancelAsync(callId4);

            // asserts
            Assert.False(cancelResult1);
            Assert.False(cancelResult2);
            Assert.True(cancelResult3);
            Assert.True(cancelResult4);
            Assert.Equal(0, sequencer.PendingQueueSize);
            Assert.Equal(2, sequencer.WaitingQueueSize);
        }
    }
}
