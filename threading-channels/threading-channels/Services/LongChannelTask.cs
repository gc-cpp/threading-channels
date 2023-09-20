using System.Threading.Channels;

namespace threading_channels.Services;

public class LongChannelTask<T>
{
    public Task Task { get; private set; }
    public IServiceProvider ServiceProvider { get; init; }
    private readonly CancellationTokenSource _cts = new();

    public void StartTask(Channel<T> channel, Func<T, IServiceProvider, CancellationToken, Task> func)
    {
        Task = HandleMessage(channel, func);
    }

    public void CancelTask()
    {
        _cts.Cancel();
    }

    private async Task HandleMessage(Channel<T> channel, Func<T, IServiceProvider, CancellationToken, Task> func)
    {
        while (!channel.Reader.Completion.IsCompleted)
        {
            if (_cts.IsCancellationRequested) throw new OperationCanceledException();
            try
            {
                var msg = await channel.Reader.ReadAsync(_cts.Token).ConfigureAwait(false);
                await func(msg, ServiceProvider, _cts.Token).ConfigureAwait(false);
            }
            catch (ChannelClosedException)
            {
            }
        }
    }
}