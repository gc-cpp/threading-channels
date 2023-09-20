using System.Collections.Concurrent;
using System.Threading.Channels;

namespace threading_channels.Services;

public struct MessageHandler<T>
{
    public Channel<T> Channel { get; set; }
    public LongChannelTask<T> TaskHandler { get; set; }
}

public class ChannelPool<T>
{
    private readonly ConcurrentDictionary<string, MessageHandler<T>> _messageHandlers = new();

    // Now as singleton
    private readonly IServiceProvider _serviceProvider;

    public ChannelPool(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public void SubscribeChannel(string userId, Func<T, IServiceProvider, CancellationToken, Task> func)
    {
        var channel = Channel.CreateUnbounded<T>(new UnboundedChannelOptions
        { SingleReader = true });
        var longChannelTask = new LongChannelTask<T> { ServiceProvider = _serviceProvider };

        _messageHandlers.TryAdd(userId, new MessageHandler<T> { Channel = channel, TaskHandler = longChannelTask });
        longChannelTask.StartTask(channel, func);
    }

    public async Task WriteToChannelAsync(string userId, T message, CancellationToken cancellationToken)
    {
        if (_messageHandlers.TryGetValue(userId, out var value))
        {
            await value.Channel.Writer.WriteAsync(message, cancellationToken).ConfigureAwait(false);
        }
    }

    public async Task UnsubscribeChannel(string userId, CancellationToken cancellationToken)
    {
        if (_messageHandlers.TryRemove(userId, out var value))
        {
            value.Channel.Writer.Complete();
            cancellationToken.Register(() => value.TaskHandler.CancelTask());
            await value.TaskHandler.Task.ConfigureAwait(false);
        }
    }
}