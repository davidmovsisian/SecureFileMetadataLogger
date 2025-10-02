using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Watcher;

var host = Host.CreateDefaultBuilder(args)
  .ConfigureServices(services =>
  {
    services.AddHostedService<WatcherService>();
    services.AddHttpClient();
  });

await host.RunConsoleAsync();