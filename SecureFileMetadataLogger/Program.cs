using Microsoft.Extensions.DependencyInjection;
//using Watcher;
using Microsoft.Extensions.Hosting;
using Watcher_ConsoleApp;

var host = Host.CreateDefaultBuilder(args)
  .ConfigureServices(services =>
  {
    services.AddHostedService<WatcherService>();
    services.AddHttpClient();
  });

//host.Build();

await host.RunConsoleAsync();