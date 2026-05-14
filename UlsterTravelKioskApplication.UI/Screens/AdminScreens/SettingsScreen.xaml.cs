using System.Windows;
using UlsterTravelKioskApplication.Services;

namespace UlsterTravelKioskApplication.UI.Screens.Admin
{
    // settings screen (updating admin username and api key/secret)
    public partial class SettingsScreen : Window
    {
        private readonly AdminService _admin; // used for updating admin settings
        private readonly DataManager _data; // used for reading and saving settings.csv

        // constructor receives shared services (AdminService, DataManager)
        public SettingsScreen(AdminService admin, DataManager data)
        {
            InitializeComponent(); // loads the XAML UI

            _admin = admin; // stores adminservice reference
            _data = data; // stores datamanager reference

            txtUsername.Text = _data.Settings.AdminUsername; // shows current admin username
            txtApiKey.Text = _data.Settings.APIKeyA; // shows current api key

        }

        // runs when the user clicks confirm
        private void ConfirmClick(object sender, RoutedEventArgs e)
        {
            // reads user inputs and trims spaces
            string username = txtUsername.Text?.Trim() ?? ""; // username textbox can be empty
            string apiKey = txtApiKey.Text?.Trim() ?? ""; // api key textbox can be empty
            string apiSecret = txtApiSecret.Password?.Trim() ?? ""; // passwordbox uses .Password

            // password change fields (all optional unless user wants to change password)
            string currentPassword = txtCurrentPassword.Password ?? "";
            string newPassword = txtNewPassword.Password ?? "";
            string confirmPassword = txtConfirmPassword.Password ?? "";

            // if user entered anything in the new password field, attempt a password change
            if (!string.IsNullOrEmpty(newPassword) || !string.IsNullOrEmpty(confirmPassword))
            {
                if (newPassword != confirmPassword)
                {
                    MessageBox.Show("New password and confirmation do not match.");
                    return;
                }

                if (!_admin.ChangePassword(currentPassword, newPassword, out string error))
                {
                    MessageBox.Show(error);
                    return;
                }
            }

            // only update username if the user typed something
            if (!string.IsNullOrWhiteSpace(username))
                _admin.UpdateSettings(username); // saves new username to settings.csv

            // if api key is left blank, keep the current saved value
            if (string.IsNullOrWhiteSpace(apiKey))
                apiKey = _data.Settings.APIKeyA ?? "";

            // if api secret is left blank, keep the current saved value
            if (string.IsNullOrWhiteSpace(apiSecret))
                apiSecret = _data.Settings.APISecretA ?? "";

            // saves api key and secret into the settings object
            _data.Settings.APIKeyA = apiKey;
            _data.Settings.APISecretA = apiSecret;

            // writes the updated settings back to settings.csv
            _data.SaveSettings();

            // confirms to the user that saving worked
            MessageBox.Show("Settings saved");

            // closes this window and returns to the admin menu
            Close();
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