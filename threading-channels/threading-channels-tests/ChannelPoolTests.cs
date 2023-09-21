using System.Diagnostics;
using AutoFixture;
using Microsoft.Extensions.DependencyInjection;
using threading_channels.Services;
using Xunit;
using static threading_channels_tests.LongChannelTaskTests;

namespace threading_channels_tests
{
    public class ChannelPoolTests
    {
        [Theory]
        [InlineData(0, 0)]
        [InlineData(1, 0)]
        [InlineData(100, 0)]
        [InlineData(50, 10)]
        [InlineData(100, 10)]
        public async Task WriteToChannelAsync_Ok(int testMessageCount, int delayMs)
        {
            // assert
            var actualCount = 0;
            var fixture = new Fixture();
            var stopWatch = new Stopwatch();
            var serviceCollection = new ServiceCollection();
            using var sp = serviceCollection.BuildServiceProvider();
            var channelPool = new ChannelPool<TestMessage>(sp);
            var userId = fixture.Create<string>();
            var testMessage = fixture.Create<TestMessage>();

            // act
            stopWatch.Start();
            channelPool.SubscribeChannel(userId, (_, _, _) =>
            {
                Interlocked.Increment(ref actualCount);
                return Task.Delay(delayMs);
            });

            for (var i = 0; i < testMessageCount; i++)
            {
                await channelPool.WriteToChannelAsync(userId, testMessage, CancellationToken.None).ConfigureAwait(false);
            }

            await channelPool.UnsubscribeChannel(userId, CancellationToken.None).ConfigureAwait(false);
            stopWatch.Stop();

            // assert
            Assert.Equal(testMessageCount, actualCount);
            Assert.True(stopWatch.ElapsedMilliseconds >= delayMs * testMessageCount);
        }

        [Theory]
        [InlineData(50, 10)]
        [InlineData(100, 10)]
        public async Task WriteToChannelAsync_Cancel(int testMessageCount, int delayMs)
        {
            // assert
            var actualCount = 0;
            var fixture = new Fixture();
            var stopWatch = new Stopwatch();
            var serviceCollection = new ServiceCollection();
            using var sp = serviceCollection.BuildServiceProvider();
            var channelPool = new ChannelPool<TestMessage>(sp);
            var userId = fixture.Create<string>();
            var testMessage = fixture.Create<TestMessage>();

            // act
            channelPool.SubscribeChannel(userId, (_, _, _) =>
            {
                Interlocked.Increment(ref actualCount);
                return Task.Delay(delayMs);
            });

            for (var i = 0; i < testMessageCount; i++)
            {
                await channelPool.WriteToChannelAsync(userId, testMessage, CancellationToken.None).ConfigureAwait(false);
            }

            var cts = new CancellationTokenSource();
            cts.Cancel();
            _ = await Assert.ThrowsAsync<OperationCanceledException>(() => channelPool.UnsubscribeChannel(userId, cts.Token));

            // assert
            Assert.True(actualCount <= testMessageCount);
        }
    }
}
