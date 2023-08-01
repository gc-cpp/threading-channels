using System.Threading.Channels;
using threading_channels.Services.Models;

namespace threading_channels.Services;

public class LongChannelTask
{
    readonly CancellationTokenSource _cts = new ();
    
    public async Task StartTask(Channel<UserAction> value, Func<Channel<UserAction>, Task> act)  
    {  
        while (!value.Reader.Completion.IsCompleted)  
        {  
            if (_cts.IsCancellationRequested) throw new OperationCanceledException();  
            await act(value);
        }  
    }  
  
    public void CancelTask()  
    {  
        _cts.Cancel();  
    }  
}