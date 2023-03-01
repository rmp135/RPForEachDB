using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using Microsoft.Win32;
using System.Text.RegularExpressions;
using System.Threading;
using Microsoft.Extensions.DependencyInjection;

namespace RPForEachDB;

public partial class MainWindow : Window
{
    private MainWindowViewModel ViewModel => (MainWindowViewModel)DataContext;
    
    private readonly ISQLTasks _sqlTasks;
    private readonly IConnectionFactory _connectionFactory;
    private readonly IConfigurationManager _configurationManager;
    
    public MainWindow()
    {
        var serviceProvider = new ServiceCollection()
            .AddTransient<IConnectionFactory, ConnectionFactory>()
            .AddTransient<IConfigurationManager, ConfigurationManager>()
            .AddTransient<ISQLTasks, SQLTasks>()
            .BuildServiceProvider();

        _sqlTasks = serviceProvider.GetService<ISQLTasks>();
        _connectionFactory = serviceProvider.GetService<IConnectionFactory>();
        _configurationManager = serviceProvider.GetService<IConfigurationManager>();

        InitializeComponent();
        
        ViewModel.Servers = new ObservableCollection<ServerModel>(_configurationManager.Configuration.Servers);
        ViewModel.Databases = new ObservableCollection<DatabaseGridItem>();
    }

    private void OnGetDatabasesBtnClick(object sender, RoutedEventArgs args)
    {
        ViewModel.Databases.Clear();
        try
        {
            using var connection = _connectionFactory.Build(ViewModel.CurrentServer);
            var names = _sqlTasks.GetAllDatabases(connection);
            foreach (var name in names.OrderBy(n => n.ToLower()))
            {
                ViewModel.Databases.Add(new DatabaseGridItem { Name = name, Status = "Available", Checked = ViewModel.CurrentServer.SelectedDatabases.Contains(name) });
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
        var filteredDatabases = ViewModel.Databases.Where(d => d.Checked).ToList();
        var statements = Regex.Split(
                ViewModel.CurrentSQL,
                @"^[\t\r\n]*GO[\t\r\n]*\d*[\t\r\n]*(?:--.*)?$",
                RegexOptions.Multiline | RegexOptions.IgnorePatternWhitespace | RegexOptions.IgnoreCase | RegexOptions.Compiled
            )
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .ToArray();

        var commandLimit = _configurationManager.Configuration.CommandLimit <= 0 ? int.MaxValue : _configurationManager.Configuration.CommandLimit;
        var semaphore = new SemaphoreSlim(commandLimit);

        foreach (var database in filteredDatabases)
        {
            RunStatements(database, statements, semaphore);
        }
    }

    private Task RunStatements(
        DatabaseGridItem database,
        IReadOnlyList<string> statements,
        SemaphoreSlim semaphore
    )
    {
        var currentServer = ViewModel.CurrentServer;
        return Task.Run(async () =>
        {
            database.Status = "Waiting...";
            await semaphore.WaitAsync();
            await using var connection = _connectionFactory.Build(currentServer);
            database.Status = "Running...";
            database.LastMessage = "";
            connection.InfoMessage += (_, args) =>
            {
                if (args.Message.Contains("Changed database context to ")) return; // Prevents unnecessary message when changing database.
                database.LastMessage += (database.LastMessage == "" ? "" : "\n") + args.Message;
            };
            try
            {
                connection.ChangeDatabase(database.Name);
                for (var i = 0; i < statements.Count; i++)
                {
                    await _sqlTasks.ExecuteAsync(connection, statements[i]);
                    var percent = (float)i / statements.Count * 100;
                    database.Status = $"({percent:0.00}%) Running...";
                }

                database.Status = database.LastMessage == "" ? "Complete" : "Completed with messages";
            }
            catch (Exception e)
            {
                database.Status = "Error";
                database.LastMessage = e.Message;
            }
            finally
            {
                semaphore.Release();
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
        _configurationManager.Save();
    }

    private void OnManageServersBtnClick(object sender, RoutedEventArgs e)
    {
        new SettingsWindow(_configurationManager)
        {
            Owner = this
        }.ShowDialog();
        var current = ViewModel.CurrentServer;
        ViewModel.Servers.Clear();
        foreach (var item in _configurationManager.Configuration.Servers) ViewModel.Servers.Add(item);
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
        ViewModel.IsChecked = ViewModel.Databases.All(d => d.Checked);
    }
    
    private void HeaderCheckBox_Click(object sender, RoutedEventArgs e)
    {
        var isChecked = ((CheckBox)sender).IsChecked ?? false;
        ViewModel.IsChecked = isChecked;
        
        foreach (var item in ViewModel.Databases)
        {
            item.Checked = isChecked;
        }
        ViewModel.CurrentServer.SelectedDatabases = ViewModel.Databases.Where(d => d.Checked).Select(d => d.Name).ToArray();
    }
}

public class MainWindowViewModel : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler PropertyChanged;

    public bool? IsChecked { get; set; } = true;

    public ObservableCollection<DatabaseGridItem> Databases { get; set; }
    public ObservableCollection<ServerModel> Servers { get; set; }
    public ServerModel CurrentServer { get; set; }
    public DatabaseGridItem CurrentItem { get; set; }
    public string CurrentSQL { get; set; } = "";
    public bool IsGetDatabaseEnabled => CurrentServer != null;
    public bool IsRunEnabled => CurrentSQL != "";
}