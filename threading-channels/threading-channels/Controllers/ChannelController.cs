using Microsoft.AspNetCore.Mvc;
using threading_channels.Services;
using threading_channels.Services.Models;

namespace threading_channels.Controllers;

[ApiController]
[Route("[controller]")]
public class ChannelController : ControllerBase
{
    private readonly ILogger _logger;
    private readonly ChannelPool _channelPool;

    public ChannelController(ILogger<ChannelController> logger, ChannelPool channelPool)
    {
        _logger = logger;
        _channelPool = channelPool;
    }

    [HttpPost("subscribe/{userId}")]
    public void SubscribeUser([FromRoute] string userId)
    {
        _logger.LogInformation($"subscribe {userId}");
        _channelPool.AddChannel(userId);
    }

    [HttpPost("unsubscribe/{userId}")]
    public void UnsubscribeUser([FromRoute] string userId)
    {
        _channelPool.DeleteChannel(userId);
    }

    [HttpPost("action")]
    public async Task AddUserAction([FromBody] UserAction userAction)
    {
        var channel = _channelPool.GetChannel(userAction.UserId);
        _logger.LogInformation($"write {userAction.UserId} {userAction.Action}");
        await channel.Writer.WriteAsync(userAction);
    }
}