
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Text;
using Common;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using Microsoft.IdentityModel.Tokens;

namespace Logger.Controllers
{
  [ApiController]
  public class LoggerController : ControllerBase
  {
    private readonly LoggerService _loggerService;
    private readonly string iss = "watcher-service";
    private readonly string _jwtSecret;
    private readonly ILogger<LoggerController> _logger;

    public LoggerController(LoggerService loggerService, ILogger<LoggerController> logger)
    {
      _loggerService = loggerService;
      _jwtSecret = Environment.GetEnvironmentVariable("JWT_SECRET");
      _logger = logger;
    }

    [HttpPost]
    [Route("log")]
    public async Task<Response> LogMetaData(LogMetaData logMetaData)
    {
      //Validation
      if (!Request.Headers.TryGetValue("Authorization", out var authHeader) ||
        string.IsNullOrEmpty(authHeader.ToString()) ||
        !authHeader.ToString().StartsWith("Bearer ") ||
        !ValidateJwtToken(authHeader))
      {
        return new Response
        {
          Status = HttpStatusCode.Unauthorized,
          Message = "JWT is invalid or expired"
        };
      }

      if (string.IsNullOrEmpty(logMetaData.FileName) ||
        logMetaData.CreatedAt == null ||
        logMetaData.FileSize == 0 ||
        string.IsNullOrEmpty(logMetaData.Hash))
      {
        return new Response
        {
          Status = HttpStatusCode.BadRequest,
          Message = "Payload is malformed"
        };
      }

      //Process Log
      if (!await _loggerService.ProcessLog(logMetaData))
        return new Response
        {
          Status = HttpStatusCode.InternalServerError,
          Message = "Logging metadata failed"
        };

      return new Response
      {
        Status = HttpStatusCode.OK,
        Message = "Succced"
      };
    }

    private bool ValidateJwtToken(StringValues authHeader)
    {
      var jwtToken = authHeader.ToString().Substring("Bearer ".Length);
      var handler = new JwtSecurityTokenHandler();
      try
      {
        var parameters = new TokenValidationParameters
        {
          ValidateIssuer = true,
          ValidIssuer = iss,
          ValidateAudience = false,
          ValidateLifetime = true,
          ClockSkew = TimeSpan.Zero,
          ValidateIssuerSigningKey = true,
          IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSecret)),
        };

        handler.ValidateToken(jwtToken, parameters, out _);

        return true;
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Jwt token is invalid");
        return false;
      }
    }
  }
}
