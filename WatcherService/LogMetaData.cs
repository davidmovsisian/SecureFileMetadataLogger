using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Watcher
{
  public class LogMetaData
  {
    public string FileName { get; set; } = "";
    public string CreatedAt { get; set; } = "";
    public long FileSize { get; set; }
    public string Hash { get; set; } = "";
  }
}
