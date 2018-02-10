using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Security;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace RPForEachDB
{
    public partial class ConnectionManager : Window, INotifyPropertyChanged
    {
        private readonly StateManager appState;

        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private IServerModel _currentModel;
        public IServerModel CurrentModel
        {
            get => _currentModel;
            set
            {
                _currentModel = value;
                NotifyPropertyChanged("CurrentModel");
                NotifyPropertyChanged("CurrentModelIndex");
                NotifyPropertyChanged("IsWindowsAuthentication");
                NotifyPropertyChanged("IsSQLAuthentication");
            }
        }

        public ObservableCollection<IServerModel> Servers { get; set; }

        private bool _isDeleteDisabled;
        public bool IsDeleteEnabled
        {
            get => _isDeleteDisabled;
            set
            {
                _isDeleteDisabled = value;
                NotifyPropertyChanged("IsDeleteEnabled");
            }
        }

        public int CurrentModelIndex
        {
            get => Servers.IndexOf(CurrentModel);
        }

        public ConnectionManager(StateManager appState)
        {
            this.appState = appState;
            Servers = new ObservableCollection<IServerModel>(appState.Servers);
            IsDeleteEnabled = true;
            if (!Servers.Any())
            {
                AddBlankServer();
                IsDeleteEnabled = false;
            }
            DataContext = this;
            CurrentModel = Servers.Last();
            InitializeComponent();
            CredentialsGrid.Visibility = SQLAuthenticationRadio.IsChecked == true ? Visibility.Visible : Visibility.Hidden;
        }

        public bool IsWindowsAuthentication
        {
            get => CurrentModel?.AuthenticationMode == AuthenticationMode.Windows;
            set
            {
                if (value)
                {
                    CurrentModel.AuthenticationMode = AuthenticationMode.Windows;
                    NotifyPropertyChanged("IsWindowsAuthentication");
                    NotifyPropertyChanged("IsSQLAuthentication");
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
                    NotifyPropertyChanged("IsWindowsAuthentication");
                    NotifyPropertyChanged("IsSQLAuthentication");
                }
            }
        }


        private void AddBlankServer()
        {
            Servers.Add(new ServerModel
            {
                Name = $"New Server {Servers.Count() + 1}"
            });
        }

        private void OnAddNewBtnClick(object sender, RoutedEventArgs e)
        {
            AddBlankServer();
            CurrentModel = Servers.Last();
            IsDeleteEnabled = true;
        }

        private void OnSecurityModeRadioClick(object sender, RoutedEventArgs e)
        {
            if (CredentialsGrid != null)
                CredentialsGrid.Visibility = SQLAuthenticationRadio.IsChecked == true ? Visibility.Visible : Visibility.Hidden;
        }

        private void OnDeleteBtnClick(object sender, RoutedEventArgs e)
        {
            var current = Servers.IndexOf(CurrentModel);
            Servers.Remove(CurrentModel);
            if (!Servers.Any())
                AddBlankServer();
            IsDeleteEnabled = Servers.Count() > 1;
            CurrentModel = Servers.ElementAt(Math.Max(0, current - 1));
        }

        private void OnCloseBtnClick(object sender, RoutedEventArgs e)
        {
            appState.Servers = Servers;
            Close();
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            appState.Save();
        }
    }
}
