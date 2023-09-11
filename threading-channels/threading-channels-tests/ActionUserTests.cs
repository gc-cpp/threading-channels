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
    private readonly WebApplicationFactory<Program> _webApplicationFactory;

    public ActionUserTests()
    {
        _webApplicationFactory = GetWebApplicationFactory();
        var dbContextFactory =
            _webApplicationFactory.Services.GetRequiredService<IDbContextFactory<UserActionContext>>();
        using var context = dbContextFactory.CreateDbContext();
        context.Database.Migrate();
    }
    
    [Fact]
    public async Task AddUserActions_Ok()
    {
        const int userCount = 50;
        const int userMessageCount = 50;
        const int userMessageDelay = 100;
        const int userMessagesDelay = 25000;
        
        var fixture = new Fixture();
        var userIds = fixture.CreateMany<Guid>(userCount);

        foreach (var userId in userIds)
        {
            Task.Run(async () =>
            {
                var client = _webApplicationFactory.CreateClient();
                await client.PostAsync($"/channel/subscribe/{userId}", null);
                for (var i = 0; i < userMessageCount; i++)
                {
                    // immitate user delay
                    await Task.Delay(userMessageDelay);
                    var payload = new UserAction() { UserId = userId.ToString(), Action = i.ToString() };
                    var stringPayload = JsonConvert.SerializeObject(payload);
                    var httpContent = new StringContent(stringPayload, Encoding.UTF8, "application/json");
                    await client.PostAsync("/channel/action", httpContent);
                }
            });
        }

        await Task.Delay(userMessagesDelay);

        var dbContextFactory =
            _webApplicationFactory.Services.GetRequiredService<IDbContextFactory<UserActionContext>>();
        await using var context = await dbContextFactory.CreateDbContextAsync();
        foreach (var userId in userIds)
        {
            Task.Run(async () =>
            {
                var client = _webApplicationFactory.CreateClient();
                await client.PostAsync($"/channel/unsubscribe/{userId}", null);
            });

            var userActions = await context.UserActions
                .Where(x => string.Equals(x.UserId, userId.ToString()))
                .OrderBy(x => x.CreatedOn)
                .ToListAsync();
            
            for (var i = 1; i < userActions.Count; i++)
            {
                Assert.False(int.Parse(userActions[i].Action) < int.Parse(userActions[i - 1].Action));
            }
        }
    }
    
    private WebApplicationFactory<Program> GetWebApplicationFactory()
    {
        return new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
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
                        $"User ID=postgres;Password=;Host=localhost;Port=15432;Database=channels-{Guid.NewGuid()};Connection Lifetime=0;")
                );

            });
        });
    }
}