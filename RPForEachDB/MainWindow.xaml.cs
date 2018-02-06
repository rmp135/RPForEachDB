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
using RPForEachDB.Properties;

namespace RPForEachDB
{
    public class DatabaseGridItem: INotifyPropertyChanged
    {
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
                    NotifiyPropertyChanged("Status");
                }
            }
        }
        public bool _checked;
        public bool Checked { get => _checked; set
            {
                _checked = value;
                if(_checked)
                    Settings.Default.SelectedDatabases.Add(Name);
                else
                    Settings.Default.SelectedDatabases.Remove(Name);
            }
        }
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
        public string ConnectionString
        {
            get => Settings.Default.ConnectionString;
            set
            {
                Settings.Default.ConnectionString = value;
            }
        }
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
            Settings.Default.Upgrade();
            Databases = new ObservableCollection<DatabaseGridItem>();
            PopulateDatabases();
            DataContext = this;
            InitializeComponent();
        }

        private void OnGetDatabasesBtnClick(object sender, RoutedEventArgs args)
        {
            PopulateDatabases();
        }

        private void PopulateDatabases()
        {
            Databases.Clear();
            try
            {
                using (var connection = new ConnectionFactory().Build())
                {
                    var names = connection.Query<string>("SELECT Name FROM master.dbo.sysdatabases WHERE DATABASEPROPERTYEX(Name, 'Status') = 'ONLINE'");
                    foreach(var name in names)
                    {
                        Databases.Add(new DatabaseGridItem { Name = name, Status = "Available" });
                    }
                }
                SetSelectedDatabases();
            }
            catch (Exception)
            {
                MessageBox.Show("Unable to retrieve databases.");
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void DataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var item = (DatabaseGridItem)((DataGrid)sender).SelectedItem;
            CurrentItem = item;
        }

        private void NotifiyPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void OnRunBtnClick(object buttonSender, RoutedEventArgs routedEventArgs)
        {
            var connectionFactory = new ConnectionFactory();
            var filteredDatabases = Databases.Where(d => d.Checked).ToList();
            var statements = Regex.Split(
                CurrentSQL,
                @"^[\t\r\n]*GO[\t\r\n]*\d*[\t\r\n]*(?:--.*)?$",
                RegexOptions.Multiline | RegexOptions.IgnorePatternWhitespace | RegexOptions.IgnoreCase
            ).Where(s => !String.IsNullOrWhiteSpace(s));
            foreach (var database in filteredDatabases)
            {
                RunScript(database, statements.ToArray());
            }
        }

        private Task RunScript(DatabaseGridItem database, string[] statements)
        {
            return Task.Factory.StartNew(() =>
            {
                using (var connection = new ConnectionFactory().Build())
                {
                    database.Status = "Running...";
                    database.LastMessage = "";
                    connection.ChangeDatabase(database.Name);
                    connection.InfoMessage += (object infoSender, SqlInfoMessageEventArgs args) =>
                    {
                        database.LastMessage += "\n" + args.Message;
                        PropertyChanged(this, new PropertyChangedEventArgs("CurrentMessage"));
                    };
                    try
                    {
                        for (var i = 0; i < statements.Length; i++)
                        {
                            connection.Execute(statements[i]);
                            var percent = (float)i / statements.Length * 100;
                            database.Status = $"({percent.ToString("0.00")}%) Running...";
                        }
                        if (database.LastMessage != "")
                        {
                            database.Status = "Completed with messages";
                        }
                        else
                        {
                            database.Status = "Complete";
                        }
                    }
                    catch (Exception e)
                    {
                        database.Status = "Error";
                        database.LastMessage = e.Message.ToString();
                    }
                }
            });
        }

        private void SetSelectedDatabases()
        {
            var databases = Settings.Default.SelectedDatabases.Cast<string>();
            foreach(var db in Databases)
            {
                db.Checked = databases.Contains(db.Name);
            };
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

        protected override void OnClosing(CancelEventArgs e)
        {
            Settings.Default.Save();
        }
    }
}
