using Microsoft.EntityFrameworkCore;
using threading_channels.Services.Models;

namespace threading_channels.Services;

public class UserService
{
    private readonly ILogger _logger;
    private readonly IDbContextFactory<UserActionContext> _dbContextFactory;

    public UserService(ILogger<UserService> logger, IDbContextFactory<UserActionContext> dbContextFactory)
    {
        _logger = logger;
        _dbContextFactory = dbContextFactory;
    }
    
    public async Task WriteUserAction(UserAction userAction, CancellationToken cancellationToken)
    {
        await MakeDelay(cancellationToken);
        try
        {
            await using var dbContext =
                await _dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
            userAction.CreatedOn = DateTimeOffset.UtcNow;
            await dbContext.UserActions.AddAsync(userAction, cancellationToken).ConfigureAwait(false);
            await dbContext.SaveChangesAsync(cancellationToken);
            _logger.LogInformation($"executed {userAction.UserId} {userAction.Action}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unavailable dbContext.");
        }
    }

    private Task MakeDelay(CancellationToken cancellationToken)
    {
        var rand = new Random();
        var del = rand.Next(200, 700);
        return Task.Delay(del, cancellationToken);
    }
}