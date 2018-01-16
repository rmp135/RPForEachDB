using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Dapper;
using System.Data.SqlClient;
using System.ComponentModel;
using System.IO;
using Microsoft.Win32;
using System.Text.RegularExpressions;

namespace RPForEachDB
{
    public enum DatabaseStatus {
        AVAILABLE,
        RUNNING,
        COMPLETE,
        COMPLETEWITHMESSAGES,
        ERROR
    }

    public class DatabaseGridItem: INotifyPropertyChanged
    {
        public string Name { get; set; }
        private DatabaseStatus status;
        public DatabaseStatus Status
        {
            get => status;
            set
            {
                if (status != value)
                {
                    status = value;
                    NotifiyPropertyChanged("Status");
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
                    NotifiyPropertyChanged("LastMessage");
                }
            }
        }

        public void NotifiyPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        public ObservableCollection<DatabaseGridItem> Databases { get; set; }
        private DatabaseGridItem _currentItem;
        public DatabaseGridItem CurrentItem
        {
            get => _currentItem;
            set
            {
                if (_currentItem != value)
                {
                    _currentItem = value;
                    NotifiyPropertyChanged("CurrentItem");
                }
            }
        }
        public string _currentSQL;
        public string CurrentSQL
        {
            get => _currentSQL;
            set
            {
                if (_currentSQL != value)
                {
                    _currentSQL = value;
                    NotifiyPropertyChanged("CurrentSQL");
                }
            }
        }
        public MainWindow()
        {
            var connectionFactory = new ConnectionFactory();
            DataContext = this;
            using (var connection = connectionFactory.Build())
            {
                var names = connection.Query<string>("SELECT Name FROM master.dbo.sysdatabases WHERE DATABASEPROPERTYEX(Name, 'Status') = 'ONLINE'");
                Databases = new ObservableCollection<DatabaseGridItem>(
                    names.Select(name => new DatabaseGridItem
                    {
                        Name = name,
                        Status = DatabaseStatus.AVAILABLE
                    })
                );
            }
            InitializeComponent();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void DataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var item = (DatabaseGridItem)((DataGrid)sender).SelectedItem;
            CurrentItem = item;
        }

        private void OnChecked(object sender, RoutedEventArgs e)
        {
            var item = (DatabaseGridItem)((DataGridCell)sender).DataContext;
            item.Checked = !item.Checked;
        }

        private void NotifiyPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void OnRunBtnClick(object buttonSender, RoutedEventArgs routedEventArgs)
        {
            var worker = new BackgroundWorker();
            worker.WorkerReportsProgress = true;
            var connectionFactory = new ConnectionFactory();
            var filteredDatabases = Databases.Where(d => d.Checked).ToList();
            var statements = Regex.Split(
                CurrentSQL,
                @"^[\t\r\n]*GO[\t\r\n]*\d*[\t\r\n]*(?:--.*)?$",
                RegexOptions.Multiline | RegexOptions.IgnorePatternWhitespace | RegexOptions.IgnoreCase
            ).Where(s => !String.IsNullOrWhiteSpace(s));
            foreach (var database in filteredDatabases)
            {
                worker.DoWork += new DoWorkEventHandler(
                delegate (object o, DoWorkEventArgs doWorkArgs)
                {
                    using (var connection = connectionFactory.Build())
                    {
                        database.LastMessage = "";
                        connection.ChangeDatabase(database.Name);
                        connection.InfoMessage += (object infoSender, SqlInfoMessageEventArgs args) =>
                        {
                            database.LastMessage += "\n" + args.Message;
                            PropertyChanged(this, new PropertyChangedEventArgs("CurrentMessage"));
                        };
                        try
                        {
                            database.Status = DatabaseStatus.RUNNING;
                            var b = o as BackgroundWorker;
                            foreach (var statement in statements)
                            {
                                connection.Execute(statement);
                            }
                            if (database.LastMessage != "")
                            {
                                database.Status = DatabaseStatus.COMPLETEWITHMESSAGES;
                            }
                            else
                            {
                                database.Status = DatabaseStatus.COMPLETE;
                            }
                        }
                        catch (Exception e)
                        {
                            database.Status = DatabaseStatus.ERROR;
                            database.LastMessage = e.Message.ToString();
                        }
                    }
                });
            }
            worker.RunWorkerAsync();
        }

        private void OnOpenFileBtnClick(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "SQL (*.sql)|*.sql|All files (*.*)|*.*"
            };
            if (openFileDialog.ShowDialog() == true)
            {
                CurrentSQL = File.ReadAllText(openFileDialog.FileName);
            }
        }
    }
}
