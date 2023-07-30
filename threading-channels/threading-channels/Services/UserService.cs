using threading_channels.Services.Models;

namespace threading_channels.Services;

public class UserService
{
    private readonly ILogger _logger;

    public UserService(ILogger<UserService> logger)
    {
        _logger = logger;
    }
    
    public async Task WriteUserAction(UserAction userAction)
    {
        var rand = new Random();
        var del = rand.Next(2500, 6500);
        await Task.Delay(del);
        _logger.LogInformation($"executed {userAction.UserId} {userAction.Action}");
    }
}