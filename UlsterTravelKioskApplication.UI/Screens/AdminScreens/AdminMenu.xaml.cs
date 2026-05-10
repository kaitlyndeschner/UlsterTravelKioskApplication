using System.Windows;
using UlsterTravelKioskApplication.Services;
using UlsterTravelKioskApplication.UI.Screens.AdminScreens;

namespace UlsterTravelKioskApplication.UI.Screens.Admin
{
    // admin menu screen
    public partial class AdminMenu : Window
    {
        private readonly AdminService _admin; // handles admin authentication and settings
        private readonly LogService _log; // provides access to logs
        private readonly DataManager _data; // provides access to data

        // constructor receives shared services (ADminService, LogService, DataManager)
        public AdminMenu(AdminService admin, LogService log, DataManager data)
        {
            InitializeComponent(); // loads the XAML UI
            _admin = admin;
            _log = log;
            _data = data;
        }

        // opens the logging system screen
        private void LoggingSystemClick(object sender, RoutedEventArgs e)
        {
            var win = new LogScreen(App.Data, _log); // create the log viewer window
            win.Owner = this; // sets this widow as the owner
            win.ShowDialog(); // displays as a modal dialog
        }

        // opens the data management screen
        private void DataManagementClick(object sender, RoutedEventArgs e)
        {
            var screen = new DataScreen(
                App.Data,
                App.APIProcessor,
                App.Log
            );

            screen.Owner = this;
            screen.ShowDialog();
        }

        // opens the admin setting screen
        private void AdminSettingsClick(object sender, RoutedEventArgs e)
        {
            var win = new SettingsScreen(_admin, _data);
            win.Owner = this;
            win.ShowDialog();
        }

        // opens the admin help screen
        private void HelpClick(object sender, RoutedEventArgs e)
        {
            MessageBox.Show(
                "Admin Help\n\n" +
                "Logging System: View API calls, API responses, and error logs.\n\n" +
                "Data Management: Upload/refresh reference data and view refresh times.\n\n" +
                "Admin Settings: Configure API keys/secrets and other system settings.\n\n" +
                "Pleaese Note: Reference data will automatically refresh if it is older than 7 days.\n",
                "Help",
                MessageBoxButton.OK,
                MessageBoxImage.Information
            );
        }

        // closes the admin menu (returns to previous screen)
        private void BackClick(object sender, RoutedEventArgs e)
        {
            Close();
        }

        // closes the application
        private void ExitClick(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }
    }
}