using System;
using System.Windows; // wpf window, events, and application shutdown
using UlsterTravelKioskApplication.Services;

namespace UlsterTravelKioskApplication.UI.Screens.Admin
{
    // admin login screen
    public partial class LoginScreen : Window
    {
        private readonly AdminService _admin; // used to validate username/password against settings
        private readonly LogService _log; // used to write login activity to logs.csv

        private int _failedAttempts = 0; // tracks how many incorrect attempts the user has made

        // constructor receives shared services (AdminService, LogService)
        public LoginScreen(AdminService admin, LogService log)
        {
            InitializeComponent(); // loads the XAML UI
            _admin = admin; // stores adminService for login validation
            _log = log; // stores logService for logging events
        }

        // runs when the user clicks confirm
        private void ConfirmClick(object sender, RoutedEventArgs e)
        {
            // reads and cleans user input from the textboxes
            string username = (txtUsername.Text ?? string.Empty).Trim();
            string password = txtPassword.Password; // passwordbox uses .Password instead of .Text

            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrEmpty(password))
            {
                lblError.Text = "Please enter username and password."; // prompts user for input
                _log.AddLog("Login", "Error: Missing username or password."); // records the reason in logs
                return; // exits the method
            }

            // calls the admin service to check if username/password are correct
            bool ok = _admin.LoginValidation(username, password);

            // if valid, closes window and returns true to the caller
            if (ok)
            {
                _log.AddLog("Login", "Admin login successful!"); // logs successful login
                DialogResult = true;
                Close(); // closes login screen
                return;
            }

            // if invalid, increase attempt counter and show an error message
            _failedAttempts++; // increments failed attempts
            _log.AddLog("Login", $"Error: Invalid login attempt {_failedAttempts}/3 for username: {username}"); // login attempts
            lblError.Text = $"Error: Invalid username or password. Attempt {_failedAttempts}/3"; // displays login error

            // after 3 failed attempts, return to the previous screen
            if (_failedAttempts >= 3)
            {
                _log.AddLog("Login", "Error: 3 failed attempts reached. Returning to main screen..."); // logs lockout behaviour
                DialogResult = false; // tells the caller login failed
                Close(); // closes the login screen
            }
        }

        // returns to the previous screen
        private void BackClick(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        // closes the application
        private void ExitClick(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }
    }
}