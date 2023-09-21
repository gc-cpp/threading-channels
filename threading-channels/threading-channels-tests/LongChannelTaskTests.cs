using System.Diagnostics;
using System.Threading.Channels;
using AutoFixture;
using threading_channels.Services;
using Xunit;

namespace threading_channels_tests
{
    public class LongChannelTaskTests
    {
        internal class TestMessage
        {
            public string Test { get; set; }
        }

        [Theory]
        [InlineData(0, 0)]
        [InlineData(1, 0)]
        [InlineData(100, 0)]
        [InlineData(50, 10)]
        [InlineData(100, 10)]
        public async Task LongChannelTask_StartAsync_Ok(int testMessageCount, int delayMs)
        {
            // arrange
            var actualCount = 0;
            var fixture = new Fixture();
            var stopWatch = new Stopwatch();
            var channel = Channel.CreateUnbounded<TestMessage>();
            var longChannelTask = new LongChannelTask<TestMessage>();
            longChannelTask.StartTask(channel, (_, _, _) =>
            {
                Interlocked.Increment(ref actualCount);
                return Task.Delay(delayMs);
            });
            var testMessage = fixture.Create<TestMessage>();

            // act
            stopWatch.Start();
            for (var i = 0; i < testMessageCount; i++)
            {
                await channel.Writer.WriteAsync(testMessage);
            }

            channel.Writer.Complete();
            await longChannelTask.Task.ConfigureAwait(false);
            stopWatch.Stop();

            // assert
            Assert.Equal(testMessageCount, actualCount);
            Assert.True(stopWatch.ElapsedMilliseconds >= delayMs * testMessageCount);
        }

        [Theory]
        [InlineData(50, 100)]
        [InlineData(100, 100)]
        public async Task LongChannelTask_StartAsync_CancelTask(int testMessageCount, int delayMs)
        {
            // arrange
            var actualCount = 0;
            var fixture = new Fixture();
            var channel = Channel.CreateUnbounded<TestMessage>();
            var longChannelTask = new LongChannelTask<TestMessage>();
            longChannelTask.StartTask(channel, (_, _, _) =>
            {
                Interlocked.Increment(ref actualCount);
                return Task.Delay(delayMs);
            });
            var testMessage = fixture.Create<TestMessage>();

            // act
            for (var i = 0; i < testMessageCount; i++)
            {
                await channel.Writer.WriteAsync(testMessage);
            }

            channel.Writer.Complete();
            longChannelTask.CancelTask();

            // assert
            Assert.True(testMessageCount > actualCount);
        }
    }
}
