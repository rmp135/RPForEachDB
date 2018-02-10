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

    public interface IServerModel
    {
        string Name { get; set; }
        string Server { get; set; }
        string Username { get; set; }
        string Password { get; set; }
        AuthenticationMode AuthenticationMode { get; set; }

    }

    [Serializable]
    public class ServerModel : IServerModel
    {
        private event PropertyChangedEventHandler PropertyChanged;

        private void NotifiyPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private string _name;
        public string Name
        {
            get => _name;
            set
            {
                _name = value;
                NotifiyPropertyChanged("Name");
            }
        }

        private string _server;
        public string Server
        {
            get => _server;
            set
            {
                _server = value;
                NotifiyPropertyChanged("Server");
            }
        }

        private string _username;
        public string Username
        {
            get => _username;
            set
            {
                _username = value;
                NotifiyPropertyChanged("Username");
            }
        }

        private string _password;
        public string Password
        {
            get => _password;
            set
            {
                _password = value;
                NotifiyPropertyChanged("Password");
            }
        }

        public AuthenticationMode AuthenticationMode { get; set; }

        public ServerModel()
        {
            Name = "";
            Password = "";
            Username = "";
            Server = "";
        }
    }
}
