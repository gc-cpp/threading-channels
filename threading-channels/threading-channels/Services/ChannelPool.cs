using System.Collections.Concurrent;
using System.Threading.Channels;
using threading_channels.Services.Models;

namespace threading_channels.Services;

public class LongChannelTask
{
    readonly CancellationTokenSource cts = new CancellationTokenSource();
    
    public async Task StartTask(Channel<UserAction> value, Func<Channel<UserAction>, Task> act)  
    {  
        while (!value.Reader.Completion.IsCompleted)  
        {  
            if (cts.IsCancellationRequested) throw new OperationCanceledException();  
            await act(value);
        }  
    }  
  
    public void CancelTask()  
    {  
        cts.Cancel();  
    }  
}

public class ChannelPool
{
    private readonly ConcurrentDictionary<string, Channel<UserAction>> _channels =
        new ConcurrentDictionary<string, Channel<UserAction>>();

    private readonly ConcurrentDictionary<string, LongChannelTask> _longChannelTasks =
        new ConcurrentDictionary<string, LongChannelTask>();

    public void AddChannel(string userId, IServiceProvider serviceProvider)
    {
        var channel = Channel.CreateUnbounded<UserAction>(new UnboundedChannelOptions() { SingleReader = true });
        _channels.TryAdd(userId, channel);
        var longChannelTask = new LongChannelTask();
        _longChannelTasks.TryAdd(userId, longChannelTask);
        using var scope = serviceProvider.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<UserService>();
        Task.Run(async () =>
        {
            await longChannelTask.StartTask(channel, async channel =>
            {
                var msg = await channel.Reader.ReadAsync();
                await service.WriteUserAction(msg);
            });
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