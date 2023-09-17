using Microsoft.AspNetCore.Mvc;
using threading_channels.Services;
using threading_channels.Services.Models;

namespace threading_channels.Controllers;

[ApiController]
[Route("[controller]")]
public class ChannelController : ControllerBase
{
    private readonly ILogger _logger;
    private readonly ChannelPool<UserAction> _channelPool;

    public ChannelController(ILogger<ChannelController> logger, ChannelPool<UserAction> channelPool)
    {
        _logger = logger;
        _channelPool = channelPool;
    }

    [HttpPost("subscribe/{userId}")]
    public void SubscribeUser([FromRoute] string userId)
    {
        _logger.LogInformation($"subscribe {userId}");
        _channelPool.SubscribeChannel(userId, async (userAction, provider, ct) =>
        {
            await using var scope = provider.CreateAsyncScope();
            var service = scope.ServiceProvider.GetRequiredService<UserService>();
            await service.WriteUserAction(userAction, ct).ConfigureAwait(false);
        });
    }

    [HttpPost("unsubscribe/{userId}")]
    public async Task UnsubscribeUser([FromRoute] string userId, CancellationToken cancellationToken)
    {
        await _channelPool.UnsubscribeChannel(userId, cancellationToken);
    }

    [HttpPost("action")]
    public async Task AddUserAction([FromBody] UserAction userAction, CancellationToken cancellationToken)
    {
        await _channelPool.WriteToChannelAsync(userAction.UserId, userAction, cancellationToken);
        _logger.LogInformation($"write {userAction.UserId} {userAction.Action}");
    }
}