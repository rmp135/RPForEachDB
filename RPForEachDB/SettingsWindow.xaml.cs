using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace RPForEachDB
{
    public partial class SettingsWindow : Window
    {
        private readonly IAppState _appState;

        public SettingsWindowViewModel ViewModel => (SettingsWindowViewModel)DataContext;

        public SettingsWindow(IAppState appState)
        {
            DataContext = new SettingsWindowViewModel();
            _appState = appState;
            ViewModel.Servers = new ObservableCollection<ServerModel>(appState.Servers);
            ViewModel.IsDeleteEnabled = true;
            if (!ViewModel.Servers.Any())
            {
                AddBlankServer();
                ViewModel.IsDeleteEnabled = false;
            }
            ViewModel.CurrentModel = ViewModel.Servers.First();
            InitializeComponent();
            CredentialsGrid.Visibility = SQLAuthenticationRadio.IsChecked == true ? Visibility.Visible : Visibility.Hidden;
        }

        private void AddBlankServer()
        {
            ViewModel.Servers.Add(new ServerModel
            {
                Name = $"New Server {ViewModel.Servers.Count() + 1}"
            });
        }

        private void OnAddNewBtnClick(object sender, RoutedEventArgs e)
        {
            AddBlankServer();
            ViewModel.CurrentModel = ViewModel.Servers.Last();
            ViewModel.IsDeleteEnabled = true;
        }

        private void OnSecurityModeRadioClick(object sender, RoutedEventArgs e)
        {
            if (CredentialsGrid != null)
                CredentialsGrid.Visibility = SQLAuthenticationRadio.IsChecked == true ? Visibility.Visible : Visibility.Hidden;
        }

        private void OnDeleteBtnClick(object sender, RoutedEventArgs e)
        {
            var current = ViewModel.Servers.IndexOf(ViewModel.CurrentModel);
            ViewModel.Servers.Remove(ViewModel.CurrentModel);
            if (!ViewModel.Servers.Any())
                AddBlankServer();
            ViewModel.IsDeleteEnabled = ViewModel.Servers.Count() > 1;
            ViewModel.CurrentModel = ViewModel.Servers.ElementAt(Math.Max(0, current - 1));
        }

        private void OnCloseBtnClick(object sender, RoutedEventArgs e)
        {
            _appState.Servers = ViewModel.Servers;
            Close();
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            _appState.Save();
        }

        private void TextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !int.TryParse(e.Text, out _);
        }

        private void TextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            e.Handled = e.Key == Key.Space;
        }

        private void TextBox_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            var source = (TextBox)sender;
            var newValue = int.Parse(source.Text) + (e.Delta < 0 ? -1 : 1);
            source.Text = Math.Max(newValue, 0).ToString();
        }
    }

    public class SettingsWindowViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public ServerModel CurrentModel { get; set; }

        public ObservableCollection<ServerModel> Servers { get; set; }

        public bool IsDeleteEnabled { get; set; }

        public int CommandTimeout { get; set; }

        public int CurrentModelIndex
        {
            get => Servers.IndexOf(CurrentModel);
        }

        public bool IsWindowsAuthentication
        {
            get => CurrentModel?.AuthenticationMode == AuthenticationMode.Windows;
            set
            {
                if (value)
                {
                    CurrentModel.AuthenticationMode = AuthenticationMode.Windows;
                }
            }
        }

        public bool IsSQLAuthentication
        {
            get => CurrentModel?.AuthenticationMode == AuthenticationMode.SQL;
            set
            {
                if (value)
                {
                    CurrentModel.AuthenticationMode = AuthenticationMode.SQL;
                }
            }
        }
    }
}
