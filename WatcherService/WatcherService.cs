using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Common;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography;
using System.Net.Http.Json;
using System.Net;
using Microsoft.AspNetCore.Mvc;

namespace Watcher_ConsoleApp
{
  public class WatcherService : BackgroundService
  {
    private readonly ILogger<WatcherService> _logger;
    private readonly IHttpClientFactory _httpClientFactory;
    private FileSystemWatcher _fileSystemWatcher;
    private readonly IHostEnvironment _hostEnvironment;

    private readonly string _watchedDir;
    private readonly string _processedDir;
    private readonly string _loggerUrl = "http://localhost:5001/log";
    private readonly string iss = "watcher-service";
    private readonly string _jwtSecret;
    private readonly TimeSpan _tokenExpire = TimeSpan.FromMinutes(5);

    public WatcherService(ILogger<WatcherService> logger,
      IHttpClientFactory httpClientFactory,
      IHostEnvironment hostEnvironment)
    {
      _logger = logger;
      _httpClientFactory = httpClientFactory;
      _hostEnvironment = hostEnvironment;

      _watchedDir = Path.Combine(_hostEnvironment.ContentRootPath, "./watched");
      _processedDir = Path.Combine(_hostEnvironment.ContentRootPath, "./processed");
      _jwtSecret = Environment.GetEnvironmentVariable("JWT_SECRET");

      Directory.CreateDirectory(_watchedDir);
      Directory.CreateDirectory(_processedDir);
    }

    protected override Task ExecuteAsync(CancellationToken ct)
    {
      _logger.LogInformation("Watching at directory{dir}", _watchedDir);
      _logger.LogInformation("ContentRootPath {contentRoot}", _hostEnvironment.ContentRootPath);

      _fileSystemWatcher = new FileSystemWatcher(_watchedDir)
      {
        //changes to watch for
        NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite,
        Filter = "*.*",
        EnableRaisingEvents = true
      };

      _fileSystemWatcher.Created += async (sender, args) =>
      {
        await Task.Delay(500);//delay to make sure that file is ready 
        _ = Task.Run(() => SendMetadata(args.FullPath));
      };

      return Task.CompletedTask;
    }

    private async Task SendMetadata(string filePath)
    {
      int maxretry = 10;
      bool isReady = false;

      //check if file is ready
      for (int i = 0; i < maxretry; i++)
      {
        try
        {
          using var fs = File.OpenRead(filePath);
          if (fs.Length > 0)
          {
            isReady = true;
            break;
          }
        }
        catch
        {
          await Task.Delay(100);
        }
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
      httpClient.Timeout = TimeSpan.FromMinutes(10);

      using var request = new HttpRequestMessage(HttpMethod.Post, _loggerUrl);
      request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", jwtTokem);
      request.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

      try
      {
        var response = await httpClient.SendAsync(request);

        if (response.IsSuccessStatusCode)
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
        _logger.LogWarning(ex, "Exception diring sending the metadata to logger");
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

        issuer: iss,
        audience: null,
        claims: new[] { new Claim(JwtRegisteredClaimNames.Iss, iss) },
        notBefore: now,
        expires: now.Add(_tokenExpire),
        signingCredentials: creds
      );

      return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private string GetSHA256Async(string filePath)
    {
      using var sha = SHA256.Create();
      using var fs = File.OpenRead(filePath);
      var hash = sha.ComputeHash(fs);
      return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
    }
  }
}
