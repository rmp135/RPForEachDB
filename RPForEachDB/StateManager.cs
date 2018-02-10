using RPForEachDB.Properties;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RPForEachDB
{
    public class StateManager
    {
        public IEnumerable<IServerModel> Servers;

        public StateManager()
        {
            Servers = Settings.Default.Servers ?? new IServerModel[0];
        }

        public void Save()
        {
            Settings.Default.Servers = Servers.Cast<ServerModel>().ToArray();
            Settings.Default.Save();
        }
    }
}
