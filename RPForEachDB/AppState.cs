using RPForEachDB.Properties;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RPForEachDB
{
    public interface IAppState
    {
        IEnumerable<IServerModel> Servers { get; set; }
        int CommandTimeout { get; set; }
        void Save();
    }
    public class AppState: IAppState
    {
        public IEnumerable<IServerModel> Servers { get; set; }
        public int CommandTimeout
        {
            get => Settings.Default.CommandTimeout;
            set => Settings.Default.CommandTimeout = value;
        }

        public AppState()
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
