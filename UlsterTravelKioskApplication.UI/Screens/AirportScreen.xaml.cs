using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using UlsterTravelKioskApplication.Services;
using UlsterTravelKioskApplication.Models;

namespace UlsterTravelKioskApplication.UI.Screens
{
    // airport screen (view destinations from selected airport)
    public partial class AirportScreen : Window
    {
        private readonly DataManager _data;
        private readonly DelayPredictionService _predictions;
        private readonly APIProcessor _apiProcessor;
        private readonly LogService _log;

        // constructor receives the shared services
        public AirportScreen(
            DataManager data,
            DelayPredictionService predictions,
            APIProcessor apiProcessor,
            LogService log)
        {
            InitializeComponent(); // loads XAML UI

            // stores references
            _data = data;
            _predictions = predictions;
            _apiProcessor = apiProcessor;
            _log = log;

            // block the screen if airport reference data is missing or stale
            if (_data.IsAirportsDataStale())
            {
                MessageBox.Show(
                    "Airport codes are missing or older than 7 days.\n\n" +
                    "Please go to Admin > Data Management > Upload Airport Codes.",
                    "Airport data not available",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                // log the reason
                _log.AddLog("Error", "Airport screen blocked: airports missing or stale");
                Close(); // close this window and return to previous screen
                return;
            }

            // populate the combobox with airport objects from the datamanager
            comboboxAirports.ItemsSource = _data.Airports;
        }

        // runs when the confirm button is clicked after selecting an airport
        private async void AirportConfirmClick(object sender, RoutedEventArgs e)
        {
            string? airportCodeSelected = comboboxAirports.SelectedValue as string;
            if (string.IsNullOrWhiteSpace(airportCodeSelected)) return; // stops if nothing is selected

            // get direct destinations for this airport using api and caching logic in apiProcessor
            var routes = await _apiProcessor.GetAirportDirectDestinations(airportCodeSelected);

            // builds display string for each destination
            foreach (var route in routes)
            {
                var destinationAirport = _data.Airports
                    .FirstOrDefault(a => a.AirportCode == route.DestinationAirportCode);

                route.DestinationDisplay = destinationAirport != null
                    ? $"{destinationAirport.AirportName} ({destinationAirport.AirportCode})"
                    : $"Unknown Airport ({route.DestinationAirportCode})";
            }

            // show the route list in the listbox
            AirportRoutesList.ItemsSource = routes;

            // clears old text from previous selections
            textAirportInfo.Inlines.Clear();
            textRouteInfo.Inlines.Clear();
            textPredictionInfo.Inlines.Clear();

            // consisdent color theme
            var textColour = textRouteInfo.Foreground;

            // shows prompt until the user selects a route from the list
            textPredictionInfo.Inlines.Add(new Run("Prediction: ")
            {
                FontWeight = FontWeights.Bold,
                Foreground = textColour
            });
            textPredictionInfo.Inlines.Add(new Run("Please select a route to view delay prediction data.")
            {
                Foreground = textColour
            });
        }

        // resets the form back to a blank state
        private void ResetClick(object sender, RoutedEventArgs e)
        {
            comboboxAirports.SelectedIndex = -1; // clears dropdown selection
            AirportRoutesList.ItemsSource = null; // clears routes list

            // clears all detail text areas
            textAirportInfo.Inlines.Clear();
            textRouteInfo.Inlines.Clear();
            textPredictionInfo.Inlines.Clear();
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

        // runs when the user selects a route in the listbox
        private async void RoutesListSelection(object sender, SelectionChangedEventArgs e)
        {
            // get the route object the user clicked
            var selectedRoute = AirportRoutesList.SelectedItem as Route;
            if (selectedRoute == null) return; // nothing selected yet

            var textColour = textRouteInfo.Foreground; // keeps consistent colours

            // find the origin airport details from the airports list
            var originAirport = _data.Airports
                .FirstOrDefault(a => a.AirportCode == selectedRoute.OriginAirportCode);

            // find the destination airport details from the airports list
            var destinationAirport = _data.Airports
                .FirstOrDefault(a => a.AirportCode == selectedRoute.DestinationAirportCode);

            // build display strings (falls back to default if the name is missing)
            string originText = originAirport != null
                ? $"{originAirport.AirportName} ({originAirport.AirportCode})"
                : selectedRoute.OriginAirportCode;

            string destinationText = destinationAirport != null
                ? $"{destinationAirport.AirportName} ({destinationAirport.AirportCode})"
                : selectedRoute.DestinationAirportCode;

            // show which airport was selected
            textAirportInfo.Inlines.Clear();
            textAirportInfo.Inlines.Add(new Run("Airport: ")
            {
                FontWeight = FontWeights.Bold,
                Foreground = textColour
            });
            textAirportInfo.Inlines.Add(new Run(originText)
            {
                Foreground = textColour
            });

            // show the selected route
            textRouteInfo.Inlines.Clear();
            textRouteInfo.Inlines.Add(new Run("Route: ")
            {
                FontWeight = FontWeights.Bold,
                Foreground = textColour
            });
            textRouteInfo.Inlines.Add(new Run($"{originText} → {destinationText}")
            {
                Foreground = textColour
            });

            // show a loading message while the api call runs
            textPredictionInfo.Inlines.Clear();
            textPredictionInfo.Inlines.Add(new Run("Prediction: ")
            {
                FontWeight = FontWeights.Bold,
                Foreground = textColour
            });
            textPredictionInfo.Inlines.Add(new Run("Fetching prediction from Amadeus...")
            {
                Foreground = textColour
            });

            try
            {
                // call the api to get the delay prediction for the origin airport
                var prediction = await _apiProcessor.GetAirportPrediction(selectedRoute.OriginAirportCode);

                // clears loading text and writes the final result
                textPredictionInfo.Inlines.Clear();
                textPredictionInfo.Inlines.Add(new Run("Prediction: ")
                {
                    FontWeight = FontWeights.Bold,
                    Foreground = textColour
                });

                // shows as unknown if api returns no result
                if (prediction == null || prediction.Status == "Unknown")
                {
                    textPredictionInfo.Inlines.Add(new Run("Unknown")
                    {
                        Foreground = textColour
                    });
                }
                else
                {
                    // show status, percent, and date returned by the api processor
                    textPredictionInfo.Inlines.Add(new Run(
                        $"{prediction.Status} ({prediction.Percentage}%) on {prediction.Date:dd/MM/yyyy}")
                    {
                        Foreground = textColour
                    });
                }
            }
            catch (Exception ex)
            {
                // logs the exception
                _log.AddLog("API Error", $"Prediction failed: {ex}");

                textPredictionInfo.Inlines.Clear();
                textPredictionInfo.Inlines.Add(new Run("Prediction: ")
                {
                    FontWeight = FontWeights.Bold,
                    Foreground = textColour
                });
                textPredictionInfo.Inlines.Add(new Run("Unknown")
                {
                    Foreground = textColour
                });
            }
        }
    }
}
