using System.Text;
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

    public async Task<bool> ProcessLog(LogMetaData logMetaData)
    {
      var sb = new StringBuilder();
      var filename = SanitzeFileName(Path.GetFileNameWithoutExtension(logMetaData.FileName));
      var timeStamp = logMetaData.CreatedAt?.ToString("YYYYMMDDTHHMMSSZ");
      var fileName = Path.Combine(_logsDir, $"{filename}-{timeStamp}.txt");

      var fileContent =
        sb.Append("FileName: ").Append($"{logMetaData.FileName}\n")
        .Append("Size: ").Append($"{logMetaData.FileSize}\n")
        .Append("Created At: ").Append($"{logMetaData.CreatedAt.ToString()}\n").ToString();

      try
      {
        await File.WriteAllTextAsync(fileName, fileContent);
        _logger.LogInformation("Log file {filename} created", fileName);
      }
      catch(Exception ex)
      {
        _logger.LogError(ex, "Failed to write {fileName}", fileName);
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
