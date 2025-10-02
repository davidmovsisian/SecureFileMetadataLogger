using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Watcher
{
  public class Settings
  {
    public string LoggerUrl { get; set; }
    public string ISS { get; set; }
    public string WatchedDir { get; set; }
    public string ProcessedDir { get; set; }
    public string DefaualtJWTSecret { get; set; }
  }
}
