using Microsoft.EntityFrameworkCore;
using threading_channels.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddPooledDbContextFactory<UserActionContext>(o =>
    o.UseNpgsql(
        "User ID=postgres;Password=;Host=localhost;Port=15432;Database=channels;Connection Lifetime=0;"));
builder.Services.AddScoped<UserService>();
builder.Services.AddSingleton<ChannelPool>();

var app = builder.Build();

app.MapControllers();

app.Run();

public partial class Program
{
}