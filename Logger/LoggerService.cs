using System.Text;
using Common.DTO;
using Microsoft.Extensions.Options;

namespace Logger
{
  public class LoggerService
  {
    private readonly ILogger<LoggerService> _logger;
    private readonly IHostEnvironment _hostEnvironment;
    private readonly string _logsDir;
    private readonly Settings _settings;
    

    public LoggerService(ILogger<LoggerService> logger, IHostEnvironment hostEnvironment, IOptions<Settings> settings)
    {
      _logger = logger;
      _hostEnvironment = hostEnvironment;
      _settings = settings.Value;
      _logsDir = Path.Combine(_hostEnvironment.ContentRootPath, _settings.LogsDir);

      _logger.LogInformation("Logs directory {logdir}", _logsDir);

      Directory.CreateDirectory(_logsDir);
    }

    public async Task<bool> ProcessLog(LogMetaData logMetaData)
    {
      var sb = new StringBuilder();
      var filename = SanitzeFileName(Path.GetFileNameWithoutExtension(logMetaData.FileName));
      var timeStamp = logMetaData.CreatedAt?.ToString("yyyyMMddTHHmmssZ");
      var logFileName = Path.Combine(_logsDir, $"{filename}-{timeStamp}.txt");

      var fileContent =
        sb.Append("FileName: ").Append($"{logMetaData.FileName}{Environment.NewLine}")
        .Append("Size: ").Append($"{logMetaData.FileSize}{Environment.NewLine}")
        .Append("Created At: ").Append($"{logMetaData.CreatedAt.ToString()}{Environment.NewLine}").ToString();

      try
      {
        File.Create(logFileName);
        await File.WriteAllTextAsync(logFileName, fileContent);
        _logger.LogInformation("Log file {filename} created", logFileName);
      }
      catch(Exception ex)
      {
        _logger.LogError(ex, "Failed to write {fileName}", logFileName);
        return false;
      }

      return true;
    }

    private static string SanitzeFileName(string filename)
    {
      var sb = new StringBuilder();
      var invalidChars = new HashSet<char>(Path.GetInvalidFileNameChars());
      for (int i = 0; i < filename.Length; i++)
      {
        if(invalidChars.Contains(filename[i]))
        {
          sb.Append('_');
        }
        else
        {
          sb.Append(filename[i]);
        }
      }

      return sb.ToString();
    }
  }
}
