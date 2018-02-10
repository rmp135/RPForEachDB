using RPForEachDB.Properties;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RPForEachDB
{
    public class DatabaseGridItem : INotifyPropertyChanged
    {
        public void NotifyPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public string Name { get; set; }
        private string status;
        public string Status
        {
            get => status;
            set
            {
                if (status != value)
                {
                    status = value;
                    NotifyPropertyChanged("Status");
                }
            }
        }
        public bool Checked { get; set; }
        private string lastMessage;
        public string LastMessage
        {
            get => lastMessage;
            set
            {
                if (lastMessage != value)
                {
                    lastMessage = value;
                    NotifyPropertyChanged("LastMessage");
                }
            }
        }

    }

}
