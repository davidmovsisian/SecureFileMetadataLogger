using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography;
using System.Net.Http.Json;
using System.Net;
using Common.DTO;
using Microsoft.Extensions.Configuration;
using System.Diagnostics;

namespace Watcher
{
  public class WatcherService : BackgroundService
  {
    private readonly ILogger<WatcherService> _logger;
    private readonly IHttpClientFactory _httpClientFactory;
    private FileSystemWatcher _fileSystemWatcher;
    private readonly IHostEnvironment _hostEnvironment;

    private readonly string _watchedDir;
    private readonly string _processedDir;
    private readonly string _loggerUrl;
    private readonly string _iss;
    private readonly string _jwtSecret;
    private readonly TimeSpan _tokenExpire = TimeSpan.FromMinutes(5);

    private readonly HashSet<string> _watchedFiles = new();
    private readonly object _lock = new ();

    public WatcherService(ILogger<WatcherService> logger,
      IHttpClientFactory httpClientFactory,
      IHostEnvironment hostEnvironment,
      IConfiguration config)
    {
      _logger = logger;
      _httpClientFactory = httpClientFactory;
      _hostEnvironment = hostEnvironment;
      var settings = config.GetSection("Settings").Get<Settings>();

      _watchedDir = Path.Combine(_hostEnvironment.ContentRootPath, settings.WatchedDir);
      _processedDir = Path.Combine(_hostEnvironment.ContentRootPath, settings.ProcessedDir);
      _iss = settings.ISS;
      _loggerUrl = Environment.GetEnvironmentVariable("LOGGER_URL") ?? settings.LoggerUrl;
      _jwtSecret = Environment.GetEnvironmentVariable("JWT_SECRET") ?? settings.DefaualtJWTSecret;

      Directory.CreateDirectory(_watchedDir);
      Directory.CreateDirectory(_processedDir);
    }

    protected override Task ExecuteAsync(CancellationToken ct)
    {
      _logger.LogInformation("Watching at directory{dir}", _watchedDir);

      _fileSystemWatcher = new FileSystemWatcher(_watchedDir)
      {
        EnableRaisingEvents = true,
        NotifyFilter = NotifyFilters.FileName
      };

      _fileSystemWatcher.Created += (sender, args) =>
      {
        lock (_lock)
        {
          if (_watchedFiles.Contains(args.FullPath))
            return;

          _watchedFiles.Add(args.FullPath);
        }

        _ = Task.Run(async () =>
        {
          await SendMetadata(args.FullPath);
          lock (_lock)
          {
            _watchedFiles.Remove(args.FullPath);
          }
        });
      };

      return Task.CompletedTask;
    }

    private async Task SendMetadata(string filePath)
    {
      bool isReady = false;

      var stopwatch = new Stopwatch();
      stopwatch.Start();

      DateTime lastWrite = File.GetLastWriteTime(filePath);

      //wait untill file is ready, not changed
      while (stopwatch.Elapsed.TotalSeconds < 100)
      {
        await Task.Delay(500);
        var newWrite = File.GetLastWriteTime(filePath);
        if (lastWrite == newWrite)
        {
          isReady = true;
          break;
        }
        lastWrite = newWrite;
      }

      if (!isReady)
      {
        _logger.LogError("Failed open file {file} for reading", filePath);
        return;
      }

      //Process file
      var fileInfo = new FileInfo(filePath);
      var payload = new LogMetaData
      {
        FileName = fileInfo.Name,
        CreatedAt = DateTime.UtcNow,
        FileSize = fileInfo.Length,
        Hash = GetSHA256Async(filePath)
      };

      var jwtTokem = CreateJwtToken();

      var httpClient = _httpClientFactory.CreateClient();

      using var request = new HttpRequestMessage(HttpMethod.Post, _loggerUrl);
      request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", jwtTokem);
      request.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

      try
      {
        var response = await httpClient.SendAsync(request);

        if(!response.IsSuccessStatusCode)
        {
          string errorMessage = await response.Content.ReadAsStringAsync();
          _logger.LogError(errorMessage);
        }
        else
        {
          var result = await response.Content.ReadFromJsonAsync<Response>();
          if (result?.Status == HttpStatusCode.OK)
          {
            _logger.LogInformation("Logger accepted {file}", fileInfo.Name);
            MoveFile(filePath, fileInfo.Name);
          }
          else
          {
            _logger.LogWarning("Logger rejected {file}, Status: {status}", fileInfo.Name, result?.Status);
          }
        }
      }
      catch (Exception ex)
      {
        _logger.LogWarning(ex, "Exception on sending the metadata to logger");
      }
    }

    private void MoveFile(string sourcePath, string fileName)
    {
      var destPath = Path.Combine(_processedDir, fileName);
      if (File.Exists(destPath))
      {
        File.Delete(destPath);
      }
      File.Move(sourcePath, destPath);
    }

    private string CreateJwtToken()
    {
      var keyBytes = Encoding.UTF8.GetBytes(_jwtSecret);
      var creds = new SigningCredentials(new SymmetricSecurityKey(keyBytes), SecurityAlgorithms.HmacSha256);

      var now = DateTime.UtcNow;
      var token = new JwtSecurityToken(

        issuer: _iss,
        audience: null,
        claims: new[] { new Claim(JwtRegisteredClaimNames.Iss, _iss) },
        notBefore: now,
        expires: now.Add(_tokenExpire),
        signingCredentials: creds
      );

      return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static string GetSHA256Async(string filePath)
    {
      using var sha = SHA256.Create();
      using var fs = File.OpenRead(filePath);
      var hash = sha.ComputeHash(fs);
      return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
    }
  }
}
