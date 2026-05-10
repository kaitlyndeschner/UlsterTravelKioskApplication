using System;
using System.Windows;
using UlsterTravelKioskApplication.Services;

namespace UlsterTravelKioskApplication.UI
{
    public partial class App : Application
    {
        // ensures every screen uses the same loaded csv data
        public static DataManager Data { get; private set; }

        // ensures api calls and caching use one consistent service
        public static APIProcessor APIProcessor { get; private set; }

        // ensures all screens write to the same log list/csv file
        public static LogService Log { get; private set; }

        // runs once when the application starts
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            try
            {
                // creates the data manager and load all csv files into memory
                Data = new DataManager();
                Data.LoadData();

                // creates the logging service once (reuse it across the application)
                Log = new LogService();

                // creates api services (api processor uses data and log)
                var apiHelper = new APIHelper();
                APIProcessor = new APIProcessor(Data, apiHelper, Log);
            }
            catch (Exception ex)
            {
                // if anything fails during startup, show a message and close the application
                MessageBox.Show(
                    "Startup error:\n" + ex.Message,
                    "Ulster Travel Kiosk",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);

                Shutdown(); // closes the application safely
            }
        }
    }
}