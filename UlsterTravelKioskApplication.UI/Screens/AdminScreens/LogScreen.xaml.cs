using System;
using System.Windows;
using System.Windows.Controls;
using UlsterTravelKioskApplication.Services;

namespace UlsterTravelKioskApplication.UI.Screens.AdminScreens
{
    // log screen for viewing logs.csv
    public partial class LogScreen : Window
    {
        private readonly DataManager _data; // provides access to Logs list and LoadLogs method
        private readonly LogService _log;

        // constants used so filter values are consistent
        private const string FilterAll = "ALL";
        private const string FilterApiCall = "API Call";
        private const string FilterApiResponse = "API Response";
        private const string FilterError = "Error";

        // constructor receives shared services (DataManager, LogService)
        public LogScreen(DataManager data, LogService log)
        {
            InitializeComponent(); // loads the XAML UI

            _data = data;
            _log = log;

            
            _data.LoadLogs(); // reloads logs from file

            comboboxLogOptions.SelectedIndex = 3; // sets default dropdown option to "Show All Logs"

            LoadLogs(FilterAll); // loads logs into the listbox
        }

        // runs when the user clicks confirm (applies the selected filter)
        private void ConfirmLogOptionClick(object sender, RoutedEventArgs e)
        {
            if (comboboxLogOptions.SelectedItem is not ComboBoxItem item)
            {
                LoadLogs(FilterAll); // fallback to showing all logs
                return;
            }
            
            string choice = (item.Content?.ToString() ?? "").Trim(); // reads selected text from the dropdown

            // converts the dropdown text into the value used in logs.csv
            string filter = choice switch
            {
                "API Calls" => FilterApiCall, // shows items (description == "API Call")
                "API Responses" => FilterApiResponse, // shows logs (description == "API Response")
                "Errors" => FilterError, // shows logs (description == "Error")
                "Show All Logs" => FilterAll, // no filtering applied
                _ => FilterAll // fallback if something unexpected appears
            };

            LoadLogs(filter); // loads and displays logs using selected filter
        }

        // loads logs.csv into memory and shows them in the listbox
        private void LoadLogs(string filter)
        {
            _data.LoadLogs();

            var logs = _data.Logs.AsEnumerable(); // Create an enumerable copy of the logs for querying


            // applies filtering unless "all" is selected
            if (filter != FilterAll)
            {
                // compares description text ignoring case ("error" matches "Error")
                logs = logs.Where(l => string.Equals(l.Description, filter, StringComparison.OrdinalIgnoreCase));
            }

            // binds the formatted output to the listbox, newest logs first
            listLogs.ItemsSource = logs
                .OrderByDescending(l => l.Timestamp) // most recent timestamp at the top
                .Select(l => $"{l.Timestamp:dd/MM/yyyy HH:mm} | {l.Description} | {l.Details}") // display format
                .ToList(); // forces execution and creates the list shown in the UI
        }

        // resets the dropdown and shows all logs
        private void ResetClick(object sender, RoutedEventArgs e)
        {
            comboboxLogOptions.SelectedIndex = 3; // sets dropdown back to "Show All Logs"
            LoadLogs(FilterAll); // reloads and displays all logs
        }

        // returns to previous screen
        private void BackClick(object sender, RoutedEventArgs e) => Close();

        // closes the application
        private void ExitClick(object sender, RoutedEventArgs e) => Application.Current.Shutdown();
    }
}