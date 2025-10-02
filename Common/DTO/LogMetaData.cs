using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.DTO
{
  public class LogMetaData
  {
    public string FileName { get; set; } = "";
    public DateTime? CreatedAt { get; set; }
    public long FileSize { get; set; }
    public string Hash { get; set; } = "";
  }
}
