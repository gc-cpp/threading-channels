using Microsoft.EntityFrameworkCore;
using threading_channels.Services;
using threading_channels.Services.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddPooledDbContextFactory<UserActionContext>(o =>
    o.UseNpgsql(
        builder.Configuration.GetValue<string>("DbContextConnectionString")));
builder.Services.AddScoped<UserService>();
builder.Services.AddSingleton<ChannelPool<UserAction>>();

var app = builder.Build();

app.MapControllers();

app.Run();

public partial class Program
{
}