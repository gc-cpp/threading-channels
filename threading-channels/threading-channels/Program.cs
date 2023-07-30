using threading_channels.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSingleton<UserService>();
builder.Services.AddSingleton<ChannelPool>();

var app = builder.Build();

// Configure the HTTP request pipeline.

app.MapControllers();

app.Run();