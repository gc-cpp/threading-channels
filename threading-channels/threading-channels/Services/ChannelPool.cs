using System.Collections.Concurrent;
using System.Threading.Channels;
using threading_channels.Services.Models;

namespace threading_channels.Services;

public class ChannelPool
{
    private readonly ConcurrentDictionary<string, Channel<UserAction>> _channels = new ();
    private readonly ConcurrentDictionary<string, LongChannelTask> _longChannelTasks = new ();

    // Now as singleton
    private readonly IServiceProvider _serviceProvider;

    public ChannelPool(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    } 

    public void AddChannel(string userId)
    {
        var channel = Channel.CreateUnbounded<UserAction>(new UnboundedChannelOptions { SingleReader = true });
        _channels.TryAdd(userId, channel);
        
        var longChannelTask = new LongChannelTask();
        _longChannelTasks.TryAdd(userId, longChannelTask);
        
        longChannelTask.StartTask(channel, async ch =>
        {
            // Scope for right concurrency. For example to DB context.
            await using var scope = _serviceProvider.CreateAsyncScope();
            var msg = await ch.Reader.ReadAsync();
            var service = scope.ServiceProvider.GetRequiredService<UserService>();
            await service.WriteUserAction(msg, longChannelTask.CancellationToken);
        });
    }

    public Channel<UserAction> GetChannel(string userId)
    {
        _channels.TryGetValue(userId, out var value);
        return value;
    }

    public void DeleteChannel(string userId)
    {
        _longChannelTasks.TryRemove(userId, out var task);
        task?.CancelTask();
        
        _channels.TryRemove(userId, out var channel);
        channel?.Writer.Complete();
    }
}