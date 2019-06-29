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
    public partial class MainWindow : Window
    {
        private readonly IAppState _appState;
        private readonly SQLTasks _sqlTasks;

        private MainWindowViewModel ViewModel => (MainWindowViewModel)DataContext;

        public MainWindow()
        {
            _appState = new AppState();
            _sqlTasks = new SQLTasks(_appState);
            DataContext = new MainWindowViewModel();
            ViewModel.Servers = new ObservableCollection<ServerModel>(_appState.Servers);
            ViewModel.Databases = new ObservableCollection<DatabaseGridItem>();
            InitializeComponent();
        }

        private void OnGetDatabasesBtnClick(object sender, RoutedEventArgs args)
        {
            ViewModel.Databases.Clear();
            try
            {
                using (var connection = new ConnectionFactory().Build(ViewModel.CurrentServer))
                {
                    var names = _sqlTasks.GetAllDatabases(connection);
                    foreach(var name in names.OrderBy(n => n.ToLower()))
                    {
                        ViewModel.Databases.Add(new DatabaseGridItem { Name = name, Status = "Available", Checked = ViewModel.CurrentServer.SelectedDatabases.Contains(name) });
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
            ViewModel.CurrentItem = item;
        }

        private void OnRunBtnClick(object buttonSender, RoutedEventArgs routedEventArgs)
        {
            var connectionFactory = new ConnectionFactory();
            var filteredDatabases = ViewModel.Databases.Where(d => d.Checked).ToList();
            var statements = Regex.Split(
                ViewModel.CurrentSQL,
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
            var viewModel = ViewModel;
            return Task.Run(() =>
            {
                using (var connection = new ConnectionFactory().Build(viewModel.CurrentServer))
                {
                    database.Status = "Running...";
                    database.LastMessage = "";
                    connection.InfoMessage += (object infoSender, SqlInfoMessageEventArgs args) =>
                    {
                        if (args.Message.Contains("Changed database context to ")) return; // Prevents unnecessary message when changing database.
                        database.LastMessage += (database.LastMessage == "" ? "" : "\n") + args.Message;
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
                ViewModel.CurrentSQL = File.ReadAllText(openFileDialog.FileName);
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
            var current = ViewModel.CurrentServer;
            ViewModel.Servers.Clear();
            foreach (var item in _appState.Servers) ViewModel.Servers.Add(item);
            if (ViewModel.Servers.Contains(current))
                ViewModel.CurrentServer = current;
        }

        private void ServerComboOnChange(object sender, SelectionChangedEventArgs e)
        {
            ViewModel.Databases.Clear();
        }

        private void CheckBox_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.CurrentServer.SelectedDatabases = ViewModel.Databases.Where(d => d.Checked).Select(d => d.Name).ToArray();
        }
    }

    public class MainWindowViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public ObservableCollection<DatabaseGridItem> Databases { get; set; }
        public ObservableCollection<ServerModel> Servers { get; set; }
        public ServerModel CurrentServer { get; set; }
        public DatabaseGridItem CurrentItem { get; set; }
        public string CurrentSQL { get; set; } = "";
        public bool IsGetDatabaseEnabled { get => CurrentServer != null; }
    }
}
