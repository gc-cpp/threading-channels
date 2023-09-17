using System.Text;
using AutoFixture;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Newtonsoft.Json;
using threading_channels.Services;
using threading_channels.Services.Models;
using Xunit;

namespace threading_channels_tests;

public class ActionUserTests
{
    [Theory]
    [InlineData(50, 50, 100, 35000)] // usual case.
    [InlineData(25, 25, 200, 0)] // unsubscribe before all messages processed.
    public async Task AddUserActions_Ok(int userCount, int userMessageCount, int userMessageDelayInMs,
        int userMessagesDelay)
    {
        var webApplicationFactory = GetWebApplicationFactory();
        var fixture = new Fixture();
        var userIds = fixture.CreateMany<Guid>(userCount);

        var tasks = new List<Task>();

        foreach (var userId in userIds)
        {
            tasks.Add(Task.Run(async () =>
            {
                var client = webApplicationFactory.CreateClient();
                await client.PostAsync($"/channel/subscribe/{userId}", null);
                for (var i = 0; i < userMessageCount; i++)
                {
                    // immitate user delay
                    await Task.Delay(userMessageDelayInMs);
                    var payload = new UserAction() { UserId = userId.ToString(), Action = i.ToString() };
                    var stringPayload = JsonConvert.SerializeObject(payload);
                    var httpContent = new StringContent(stringPayload, Encoding.UTF8, "application/json");
                    await client.PostAsync("/channel/action", httpContent);
                }
            }));
        }

        var postMessagesTask = Task.WhenAll(tasks);
        await Task.WhenAll(Task.Delay(userMessagesDelay), postMessagesTask);

        var dbContextFactory =
            webApplicationFactory.Services.GetRequiredService<IDbContextFactory<UserActionContext>>();
        await using var context = await dbContextFactory.CreateDbContextAsync();
        foreach (var userId in userIds)
        {
            var client = webApplicationFactory.CreateClient();
            await client.PostAsync($"/channel/unsubscribe/{userId}", null);

            var userActions = await context.UserActions
                .Where(x => string.Equals(x.UserId, userId.ToString()))
                .OrderBy(x => x.CreatedOn)
                .ToListAsync();
            
            for (var i = 1; i < userActions.Count; i++)
            {
                // assert that in right order.
                Assert.False(int.Parse(userActions[i].Action) < int.Parse(userActions[i - 1].Action));
            }
        }

        // assert that all messages are processed.
        Assert.Equal(userCount * userMessageCount, context.UserActions.Count());
    }
    
    private WebApplicationFactory<Program> GetWebApplicationFactory()
    {
        var webApplicationFactory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<UserActionContext>();
                services.RemoveAll<IDbContextFactory<UserActionContext>>();
                services.RemoveAll<DbContextOptions>();
                
                foreach (var option in services.Where(s => s.ServiceType.BaseType == typeof(DbContextOptions)).ToList())
                {
                    services.Remove(option);
                }

                services.AddPooledDbContextFactory<UserActionContext>(options =>
                    options.UseNpgsql(
                        $"User ID=postgres;Password=;Host=localhost;Port=5432;Database=channels-{Guid.NewGuid()};Connection Lifetime=0;")
                );

            });
        });
        
        var dbContextFactory =
            webApplicationFactory.Services.GetRequiredService<IDbContextFactory<UserActionContext>>();
        using var context = dbContextFactory.CreateDbContext();
        context.Database.Migrate();

        return webApplicationFactory;
    }
}