using System.Windows;
using System.Windows.Input;
using UlsterTravelKioskApplication.Services;

namespace UlsterTravelKioskApplication.UI.Screens
{
    // home screen (Navigation to airlines, airports, and destinations screens)
    public partial class HomeScreen : Window
    {
        // stores admin features (login + settings updates)
        private readonly AdminService _admin;
        private readonly APIHelper _apiHelper;
        private readonly APIProcessor _apiProcessor;
        private readonly DataManager _data;
        private readonly DelayPredictionService _predictions;
        private readonly LogService _log;

        public HomeScreen()
        {
            InitializeComponent(); // loads the XAML UI

            // forces keyboard focus so shortcuts work
            Loaded += (s, e) => Keyboard.Focus(this);

            _log = new LogService();
            _log.AddLog("System", "App started");

            // creates the data manager and loads csv files into memory
            _data = new DataManager();
            _data.LoadData();


           // _log.AddLog("System", $"Data folder resolved to: {_data.DataFolderPath}");
          //  _log.AddLog("System",
               // $"Loaded Airports={_data.Airports.Count}, Airlines={_data.Airlines.Count}, Routes={_data.Routes.Count}, Destinations={_data.Destinations.Count}");

            // create services that depend on data and logs
            _admin = new AdminService(_data, _log);

            // handles raw http requests to the api
            _apiHelper = new APIHelper();

            // uses apiHelper and data manager to cache results and log requests
            _apiProcessor = new APIProcessor(_data, _apiHelper, _log);

            _predictions = new DelayPredictionService(_data);

            
            Focus(); // ensures keydown event fires
        }

        // runs when the user clicks the help button (opens help screen)
        private void HelpClick(object sender, RoutedEventArgs e)
        {
            MessageBox.Show(
                "Ulster Travel Kiosk Help\n\n" +
                "Airports: Select an airport to view direct destinations and delay prediction data.\n\n" +
                "Airlines: Select an airline to view available routes.\n\n" +
                "Destinations: Select a destination country to view destinations.\n\n" +
                "Navigation: Use on-screen buttons to move through the system.\n",
                "Help",
                MessageBoxButton.OK,
                MessageBoxImage.Information
            );
        }

        // runs when the airports button is clicked on the home screen
        private void AirportClick(object sender, RoutedEventArgs e)
        {
            // blocks the screen if airport data is missing or older than 7 days
            if (_data.IsAirportsDataStale())
            {
                MessageBox.Show(
                    "Airport codes are missing or older than 7 days\n\n" +
                    "Please go to Admin > Data Management > Upload Airport Codes",
                    "Airport data not available",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                // write a log entry
                _log.AddLog("Error", "Airport screen blocked: airports missing or stale");
                return;
            }

            // opens the airport screen and passes shared services into it
            var win = new AirportScreen(_data, _predictions, _apiProcessor, _log);
            win.Owner = this; // sets owner so the new window stays on top of home screen
            win.ShowDialog(); // modal window (user must close it to return)
        }

        // runs when the airlines button is clicked on the home screen
        private void AirlineClick(object sender, RoutedEventArgs e)
        {
            // blocks the screen if airline data is missing or older than 7 days
            if (_data.IsAirlinesDataStale())
            {
                MessageBox.Show(
                    "Airline codes are missing or older than 7 days\n\n" +
                    "Please go to Admin > Data Management > Upload Airline Codes",
                    "Airline data not available",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                _log.AddLog("Error", "Airline screen blocked: airlines missing or stale");
                return;
            }

            // opens the airline screen
            var win = new AirlineScreen(_data, _predictions, _apiProcessor, _log);
            win.Owner = this;
            win.ShowDialog();
        }

        // runs when the destinations button is clicked
        private void DestinationsClick(object sender, RoutedEventArgs e)
        {
            if (_data.IsAirportsDataStale())
            {
                MessageBox.Show(
                    "Airport codes are missing or older than 7 days\n\n" +
                    "Please go to Admin > Data Management > Upload Airport Codes",
                    "Destination data not available",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                _log.AddLog("Error", "Destinations screen blocked: airports missing or stale");
                return;
            }

            // opens the destinations screen
            var win = new DestinationsScreen(_data, _predictions, _apiProcessor, _log);
            win.Owner = this;
            win.ShowDialog();
        }

        // runs when a key is pressed while this window has focus
        private void WindowKeyDown(object sender, KeyEventArgs e)
        {
            // checks if the shift key is held down
            bool shiftHeld = Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift);

            // hidden admin shortcut: shift + a
            if (shiftHeld && e.Key == Key.A)
            {
                OpenAdministrativeLogin(); // opens login screen then admin menu
                e.Handled = true; // stops the key press from being handled elsewhere
            }
        }

        // opens the admin login screen and then the admin menu if login is successful
        private void OpenAdministrativeLogin()
        {
            // create the login window and pass admin + log services
            var login = new Admin.LoginScreen(_admin, _log);
            login.Owner = this;

            // show dialog returns true if login succeeded, false otherwise
            bool? result = login.ShowDialog();

            if (result == true)
            {
                // after successful login, open the admin menu
                var adminMenu = new Admin.AdminMenu(_admin, _log, _data);
                adminMenu.Owner = this;
                adminMenu.ShowDialog();
            }
        }

        // closes the application
        private void ExitClick(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }
    }
}
