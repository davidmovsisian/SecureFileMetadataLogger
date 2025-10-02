using System.Net;
using Microsoft.AspNetCore.Mvc;

namespace Common.DTO
{
  public class Response
  {
    public HttpStatusCode Status { get; set; }
    public string Message { get; set; } = "";
  }
}
