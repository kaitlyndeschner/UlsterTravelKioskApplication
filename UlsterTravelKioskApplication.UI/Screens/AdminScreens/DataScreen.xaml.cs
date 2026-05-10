using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using UlsterTravelKioskApplication.Services;

namespace UlsterTravelKioskApplication.UI.Screens.AdminScreens
{
    // data management screen
    public partial class DataScreen : Window
    {
        private readonly DataManager _data; // stores access to all csv backed data lists and settings
        private readonly APIProcessor _api; // used for api actions (if needed by this screen)
        private readonly LogService _log; // used for logging admin actions (optional)

        // constructor receives shared services (DataManager, APIProcessor, LogService)
        public DataScreen(DataManager data, APIProcessor _api, LogService _log)
        {
            InitializeComponent(); // loads the XAML UI

            _data = data; // stores datamanager for csv loading/saving
            this._api = _api; // stores apiprocessor for api work
            this._log = _log; // stores logservice for logs

            UpdateDisplay(); // shows refresh dates and record counts on load
        }

        // updates the refresh dates, record counts, and status text
        private void UpdateDisplay()
        {
            // shows the last refresh date/time values from settings.csv
            textAirlinesRefresh.Text = _data.Settings.AirlinesRefresh.ToString("dd/MM/yyyy HH:mm");
            textAirportsRefresh.Text = _data.Settings.AirportsRefresh.ToString("dd/MM/yyyy HH:mm");
            textDestinationsRefresh.Text = _data.Settings.DestinationsRefresh.ToString("dd/MM/yyyy HH:mm");

            // shows how many records are currently loaded in memory
            textTracking.Text =
                $"Airlines: {_data.Airlines.Count}\n" +
                $"Airports: {_data.Airports.Count}\n" +
                $"Routes: {_data.Routes.Count}\n" +
                $"Destinations: {_data.Destinations.Count}\n" +
                $"Delay Predictions: {_data.DelayPredictions.Count}\n" +
                $"Logs: {_data.Logs.Count}\n" +
                $"Airport Direct Destinations: {_data.AirportDirectDestinations.Count}\n" +
                $"Airline Destinations: {_data.AirlineDestinations.Count}";

            if (string.IsNullOrWhiteSpace(txtStatus.Text))
            {
                txtStatus.Text = "Ready";
            }
        }

        // runs the selected admin action from the dropdown
        private void ConfirmClick(object sender, RoutedEventArgs e)
        {
            if (comboboxActions.SelectedItem == null)
            {
                txtStatus.Text = "Please select an action";
                return;
            }

            // reads the selected combobox item text
            string selectedAction = ((ComboBoxItem)comboboxActions.SelectedItem).Content?.ToString() ?? "";

            try
            {
                // runs different code depending on the selected action (switch statement)
                switch (selectedAction)
                {
                    case "Reload Data":
                        _data.LoadData(); // reloads all csv files into memory
                        txtStatus.Text = "Data reloaded";
                        break;

                    case "Save Data":
                        _data.SaveData(); // saves current lists back to csv
                        txtStatus.Text = "Data saved";
                        break;

                    case "Upload Airline Codes":
                        {
                            // opens file picker so admin can choose a csv file
                            string filePath = PickCsvFile("Select airline codes CSV");
                            if (string.IsNullOrWhiteSpace(filePath))
                            {
                                txtStatus.Text = "Upload cancelled";
                                break;
                            }

                            // imports the csv into the airlines list and saves it
                            int count = _data.ImportAirlinesFromCSV(filePath);

                            // updates refresh time so the data is treated as current
                            _data.Settings.AirlinesRefresh = DateTime.Now;
                            _data.SaveSettings();

                            txtStatus.Text = $"Airline codes uploaded: {count} records";
                            break;
                        }

                    case "Upload Airport Codes":
                        {
                            string filePath = PickCsvFile("Select airport codes CSV");
                            if (string.IsNullOrWhiteSpace(filePath))
                            {
                                txtStatus.Text = "Upload cancelled";
                                break;
                            }

                            int count = _data.ImportAirportsFromCSV(filePath);

                            _data.Settings.AirportsRefresh = DateTime.Now;
                            _data.SaveSettings();

                            txtStatus.Text = $"Airport codes uploaded: {count} records";
                            break;
                        }

                    // clears existing data so the app will require fresh data next time
                    case "Refresh Airlines":
                        _data.Airlines.Clear(); // clears airlines list in memory
                        _data.SaveAirlines(); // saves empty airlines.csv
                        _data.Settings.AirlinesRefresh = DateTime.MinValue; // marks refresh date as unknown/old
                        _data.SaveSettings();
                        txtStatus.Text = "Airlines cleared (will require new upload)";
                        break;

                    case "Refresh Airports":
                        _data.Airports.Clear();
                        _data.SaveAirports();
                        _data.Settings.AirportsRefresh = DateTime.MinValue;
                        _data.SaveSettings();
                        txtStatus.Text = "Airports cleared (will require new upload)";
                        break;

                    case "Refresh Destinations":
                        _data.Destinations.Clear();
                        _data.SaveDestinations();
                        _data.Settings.DestinationsRefresh = DateTime.MinValue;
                        _data.SaveSettings();
                        txtStatus.Text = "Destinations cleared (will be rebuilt on next use)";
                        break;

                    case "Refresh Delay Predictions":
                        _data.DelayPredictions.Clear(); // clears saved prediction results
                        _data.SaveDelayPredictions(); // writes empty delayPredictions.csv
                        txtStatus.Text = "Delay predictions cleared";
                        break;

                    case "Refresh Airport Direct Destinations":
                        _data.AirportDirectDestinations.Clear(); // clears cached airport destination results
                        _data.SaveAirportDirectDestinations(); // saves empty cache csv
                        _data.Settings.AirportRoutesRefresh = DateTime.MinValue; // forces stale cache behaviour
                        _data.SaveSettings();
                        txtStatus.Text = "Airport direct destinations cache cleared";
                        break;

                    case "Refresh Airline Destinations":
                        _data.AirlineDestinations.Clear();
                        _data.SaveAirlineDestinations();
                        _data.Settings.AirlineDestinationsRefresh = DateTime.MinValue;
                        _data.SaveSettings();
                        txtStatus.Text = "Airline destinations cache cleared";
                        break;

                    default:
                        txtStatus.Text = "Error: Unknown action selected";
                        break;
                }
            }
            catch (Exception ex)
            {
                // shows the error message in the status box instead of crashing
                txtStatus.Text = $"Error: {ex.Message}";
            }

            UpdateDisplay(); // refreshes the overview section after action runs
        }

        // opens a file picker and returns a selected csv file path
        private string PickCsvFile(string title)
        {
            var dialog = new OpenFileDialog
            {
                Title = title, // text shown at the top of the dialog
                Filter = "CSV files (*.csv)|*.csv|All files (*.*)|*.*" // restricts to csv by default
            };

            bool? result = dialog.ShowDialog(); // shows dialog and returns user action
            if (result != true) return ""; // cancelled

            if (!File.Exists(dialog.FileName)) return ""; // extra safety check

            return dialog.FileName; // returns chosen file path
        }

        // resets the dropdown and status message
        private void ResetClick(object sender, RoutedEventArgs e)
        {
            comboboxActions.SelectedIndex = -1; // clears selection
            txtStatus.Text = "Ready";
            UpdateDisplay();
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