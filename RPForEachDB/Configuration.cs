using System.Collections.Generic;
using System.Linq;

namespace RPForEachDB;

public class Configuration
{
    public int CommandTimeout { get; set; } = 30;
    public int CommandLimit { get; set; } = -1;
    public IEnumerable<ServerModel> Servers { get; set; } = Enumerable.Empty<ServerModel>();
}