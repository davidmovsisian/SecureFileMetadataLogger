using Logger;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddLogging();
builder.Services.AddTransient<LoggerService>();

var app = builder.Build();

app.MapControllers();

app.Run();
