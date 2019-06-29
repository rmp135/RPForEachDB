using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RPForEachDB
{
    public enum AuthenticationMode
    {
        Windows,
        SQL
    }

    [Serializable]
    public class ServerModel: INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public string Name { get; set; } = "";

        public string Server { get; set; } = "";

        public string Username { get; set; } = "";

        public string Password { get; set; } = "";

        public AuthenticationMode AuthenticationMode { get; set; } = AuthenticationMode.Windows;

        public string[] SelectedDatabases { get; set; } = new string[0];
    }
}
