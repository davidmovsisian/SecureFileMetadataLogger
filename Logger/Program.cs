using Logger;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddLogging();
builder.Services.Configure<Settings>(
  builder.Configuration.GetSection("Settings"));
builder.Services.AddSingleton<Settings>();
builder.Services.AddTransient<LoggerService>();


var app = builder.Build();

app.MapControllers();

app.Run();
