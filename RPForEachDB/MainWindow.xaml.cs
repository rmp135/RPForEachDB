using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Data.SqlClient;
using System.ComponentModel;
using System.IO;
using Microsoft.Win32;
using System.Text.RegularExpressions;
using RPForEachDB.Properties;

namespace RPForEachDB
{
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        private readonly IAppState _appState;
        public ObservableCollection<DatabaseGridItem> Databases { get; set; }
        public ObservableCollection<IServerModel> Servers { get; set; }
        private IServerModel _currentServer { get; set; }
        private readonly SQLTasks _sqlTasks;
        public IServerModel CurrentServer
        {
            get => _currentServer;
            set
            {
                _currentServer = value;
                NotifyPropertyChanged("CurrentServer");
            }
        }
        private DatabaseGridItem _currentItem;
        public DatabaseGridItem CurrentItem
        {
            get => _currentItem;
            set
            {
                if (_currentItem != value)
                {
                    _currentItem = value;
                    NotifyPropertyChanged("CurrentItem");
                }
            }
        }
        private string _currentSQL = "";
        public string CurrentSQL
        {
            get => _currentSQL;
            set
            {
                if (_currentSQL != value)
                {
                    _currentSQL = value;
                    NotifyPropertyChanged("CurrentSQL");
                }
            }
        }
        private bool _isGetDatabaseEnabled;
        public bool IsGetDatabaseEnabled
        {
            get => _isGetDatabaseEnabled;
            set
            {
                _isGetDatabaseEnabled = value;
                NotifyPropertyChanged("IsGetDatabaseEnabled");
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public MainWindow()
        {
            _appState = new AppState();
            _sqlTasks = new SQLTasks(_appState);
            Servers = new ObservableCollection<IServerModel>(_appState.Servers);
            Databases = new ObservableCollection<DatabaseGridItem>();
            DataContext = this;
            InitializeComponent();
            IsGetDatabaseEnabled = CurrentServer != null;
        }

        private void OnGetDatabasesBtnClick(object sender, RoutedEventArgs args)
        {
            Databases.Clear();
            try
            {
                using (var connection = new ConnectionFactory().Build(CurrentServer))
                {
                    var names = _sqlTasks.GetAllDatabases(connection);
                    foreach(var name in names)
                    {
                        Databases.Add(new DatabaseGridItem { Name = name, Status = "Available" });
                    }
                }
            }
            catch (Exception)
            {
                MessageBox.Show("Unable to retrieve databases.");
            }
        }

        private void DataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var item = (DatabaseGridItem)((DataGrid)sender).SelectedItem;
            CurrentItem = item;
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
                using (var connection = new ConnectionFactory().Build(CurrentServer))
                {
                    database.Status = "Running...";
                    database.LastMessage = "";
                    connection.InfoMessage += (object infoSender, SqlInfoMessageEventArgs args) =>
                    {
                        if (args.Message.Contains("Changed database context to ")) return; // Prevents unnecessary message when changing database.
                        database.LastMessage += (database.LastMessage == "" ? "" : "\n") + args.Message;
                        PropertyChanged(this, new PropertyChangedEventArgs("CurrentMessage"));
                    };
                    try
                    {
                        connection.ChangeDatabase(database.Name);
                        for (var i = 0; i < statements.Length; i++)
                        {
                            _sqlTasks.Execute(connection, statements[i]);
                            var percent = (float)i / statements.Length * 100;
                            database.Status = $"({percent.ToString("0.00")}%) Running...";
                        }
                        if (database.LastMessage == "")
                        {
                            database.Status = "Complete";
                        }
                        else
                        {
                            database.Status = "Completed with messages";
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
            _appState.Save();
        }

        private void OnManageServersBtnClick(object sender, RoutedEventArgs e)
        {
            new SettingsWindow(_appState)
            {
                Owner = this
            }.ShowDialog();
            var current = CurrentServer;
            Servers.Clear();
            foreach (var item in _appState.Servers) Servers.Add(item);
            if (Servers.Contains(current))
                CurrentServer = current;
        }

        private void ServerComboOnChange(object sender, SelectionChangedEventArgs e)
        {
            IsGetDatabaseEnabled = CurrentServer != null;
            Databases.Clear();
        }
    }
}
