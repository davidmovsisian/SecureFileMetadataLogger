using Microsoft.Extensions.DependencyInjection;
using Watcher;
using Microsoft.Extensions.Hosting;

var host = Host.CreateDefaultBuilder(args)
  .ConfigureServices(services =>
  {
    services.AddHostedService<WatcherService>();
    services.AddHttpClient();
  });

//host.Build();

await host.RunConsoleAsync();