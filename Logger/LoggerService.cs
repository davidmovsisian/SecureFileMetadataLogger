using Common;
using Microsoft.AspNetCore.Mvc;

namespace Logger
{
  public class LoggerService
  {
    private readonly ILogger<LoggerService> _logger;
    private readonly IHostEnvironment _hostEnvironment;
    private readonly string _logsDir;
    

    public LoggerService(ILogger<LoggerService> logger, IHostEnvironment hostEnvironment)
    {
      _logger = logger;
      _hostEnvironment = hostEnvironment;
      _logsDir = Path.Combine(_hostEnvironment.ContentRootPath, "./logs"); ;

      Directory.CreateDirectory(_logsDir);
    }

    public bool ProcessLog(LogMetaData logMetaData)
    {
      return false;
    }
  }
}
