using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using UlsterTravelKioskApplication.Models;
using UlsterTravelKioskApplication.Services;

namespace UlsterTravelKioskApplication.UI.Screens
{
    // airline screen (viewing destinations and delay prediction info)
    public partial class AirlineScreen : Window
    {
        private readonly DataManager _data; // provides access to airlines, airports, routes, settings, etc
        private readonly DelayPredictionService _predictions; // provides cached/local prediction fallback
        private readonly APIProcessor _apiProcessor; // handles calls to the amadeus api (with caching)
        private readonly LogService _log; // writes actions and errors to logs.csv

        // constructor receives shared services
        public AirlineScreen(
            DataManager data,
            DelayPredictionService predictions,
            APIProcessor apiProcessor,
            LogService log)
        {
            InitializeComponent(); // loads the XAML UI

            _data = data;
            _predictions = predictions;
            _apiProcessor = apiProcessor;
            _log = log;

            // sets the combobox list to the airline objects loaded in memory
            comboboxAirlines.ItemsSource = _data.Airlines;

            // makes combobox selectedvalue return the airline code property
            comboboxAirlines.SelectedValuePath = "AirlineCode";
        }

        // helper method to display airports in a user friendly way
        private string FormatAirport(string airportCode)
        {
            airportCode = (airportCode ?? "").Trim().ToUpper(); // enforces standard IATA format

            var airport = _data.Airports.FirstOrDefault(a => a.AirportCode == airportCode); // locates matching airport object

            if (airport != null && !string.IsNullOrWhiteSpace(airport.AirportName)) // ensures airport name is valid
                return $"{airport.AirportName} ({airport.AirportCode})"; // returns formatted display string

            return $"Unknown Airport ({airportCode})"; // fallback if the airport is not in airports.csv
        }

        // runs when confirm button is clicked (loads destinations for selected airline)
        private async void AirlineConfirmClick(object sender, RoutedEventArgs e)
        {
            string? airlineCodeSelected = comboboxAirlines.SelectedValue as string; // gets the selected airline code
            if (string.IsNullOrWhiteSpace(airlineCodeSelected)) return;

            // calls api processor to get destinations for selected airline
            var routes = await _apiProcessor.GetAirlineDestinations(airlineCodeSelected);

            foreach (var route in routes)
            {
                route.RouteDisplay = FormatAirport(route.DestinationAirportCode); // uses helper to show name (airportcode)
            }

            AirlineRoutesList.ItemsSource = routes; // binds routes list into the listbox

            // clears the previous info text
            textAirlineInfo.Inlines.Clear();
            textAirlineRouteInfo.Inlines.Clear();
            textAirlinePredictionInfo.Inlines.Clear();

            var textColour = textAirlineRouteInfo.Foreground; // stores current ui text colour

            // writes a prompt
            textAirlinePredictionInfo.Inlines.Add(new Run("Prediction: ")
            {
                FontWeight = FontWeights.Bold, // makes label bold
                Foreground = textColour // consistent color scheme
            });
            textAirlinePredictionInfo.Inlines.Add(new Run("Please select a destination to view delay prediction data.")
            {
                Foreground = textColour // consistent color scheme
            });
        }

        // resets the screen back to its default state
        private void ResetClick(object sender, RoutedEventArgs e)
        {
            comboboxAirlines.SelectedIndex = -1; // clears the airline selection
            AirlineRoutesList.ItemsSource = null; // clears the routes list

            // clears all detail text areas
            textAirlineInfo.Inlines.Clear();
            textAirlineRouteInfo.Inlines.Clear();
            textAirlinePredictionInfo.Inlines.Clear();
        }

        // returns to previous screen
        private void BackClick(object sender, RoutedEventArgs e) => Close();

        // closes the application
        private void ExitClick(object sender, RoutedEventArgs e) => Application.Current.Shutdown();

        // runs when user selects a route from the listbox (async because it calls the api for prediction data)
        private async void RoutesListSelection(object sender, SelectionChangedEventArgs e)
        {
            var selectedRoute = AirlineRoutesList.SelectedItem as Route; // gets selected route object from listbox
            if (selectedRoute == null) return;

            var textColour = textAirlineRouteInfo.Foreground; // consistent UI design

            // shows a loading message while waiting for the api call
            textAirlinePredictionInfo.Inlines.Clear();
            textAirlinePredictionInfo.Inlines.Add(new Run("Prediction: ")
            {
                FontWeight = FontWeights.Bold,
                Foreground = textColour
            });
            textAirlinePredictionInfo.Inlines.Add(new Run("Fetching prediction from Amadeus...")
            {
                Foreground = textColour
            });

            try
            {
                // logs what route the user clicked
                _log.AddLog("UI",
                    $"Airline route selected. Origin={selectedRoute.OriginAirportCode}, Dest={selectedRoute.DestinationAirportCode}, Airline={selectedRoute.AirlineCode}");

                // finds airline name from the selected route airline code
                var airline = _data.Airlines
                    .FirstOrDefault(a => a.AirlineCode == selectedRoute.AirlineCode);

                // uses fallback label if airline code is missing from airlines list
                string airlineText = airline != null
                    ? $"{airline.AirlineName} ({airline.AirlineCode})"
                    : $"Unknown Airline ({selectedRoute.AirlineCode})";

                // formats the route airports for display
                string originText = FormatAirport(selectedRoute.OriginAirportCode);
                string destinationText = FormatAirport(selectedRoute.DestinationAirportCode);

                // displays airline info section
                textAirlineInfo.Inlines.Clear();
                textAirlineInfo.Inlines.Add(new Run("Airline: ")
                {
                    FontWeight = FontWeights.Bold,
                    Foreground = textColour
                });
                textAirlineInfo.Inlines.Add(new Run(airlineText)
                {
                    Foreground = textColour
                });

                // displays destination info section
                textAirlineRouteInfo.Inlines.Clear();
                textAirlineRouteInfo.Inlines.Add(new Run("Destination: ")
                {
                    FontWeight = FontWeights.Bold,
                    Foreground = textColour
                });
                textAirlineRouteInfo.Inlines.Add(new Run(FormatAirport(selectedRoute.DestinationAirportCode))
                {
                    Foreground = textColour
                });

                // prepares airport code for the api call
                string airportCodeToSend = (selectedRoute.DestinationAirportCode ?? "").Trim().ToUpper();

                // stops early if the airport code is not valid IATA format
                if (airportCodeToSend.Length != 3)
                {
                    _log.AddLog("API", $"Invalid airport code for Amadeus: '{airportCodeToSend}'");

                    // displays unknown if api cannot be called
                    textAirlinePredictionInfo.Inlines.Clear();
                    textAirlinePredictionInfo.Inlines.Add(new Run("Prediction: ")
                    {
                        FontWeight = FontWeights.Bold,
                        Foreground = textColour
                    });
                    textAirlinePredictionInfo.Inlines.Add(new Run("Unknown")
                    {
                        Foreground = textColour
                    });
                    return;
                }

                _log.AddLog("API", $"Requesting on-time prediction for airportCode={airportCodeToSend}");

                var apiPrediction = await _apiProcessor.GetAirportPrediction(airportCodeToSend);

                // clears loading message and writes new prediction label
                textAirlinePredictionInfo.Inlines.Clear();
                textAirlinePredictionInfo.Inlines.Add(new Run("Prediction: ")
                {
                    FontWeight = FontWeights.Bold,
                    Foreground = textColour
                });

                // if api returns unknown, show unknown in the UI
                if (apiPrediction == null || apiPrediction.Status == "Unknown")
                {
                    textAirlinePredictionInfo.Inlines.Add(new Run("Unknown")
                    {
                        Foreground = textColour
                    });
                    return;
                }

                // shows the prediction status, percentage and date from the api response
                textAirlinePredictionInfo.Inlines.Add(new Run(
                    $"{apiPrediction.Status} ({apiPrediction.Percentage}%) on {apiPrediction.Date:dd/MM/yyyy}")
                {
                    Foreground = textColour
                });
            }
            catch (Exception ex)
            {
                // logs the full exception
                _log.AddLog("API Error",
                    $"AirlineScreen prediction failed. Origin={selectedRoute.OriginAirportCode}. {ex}");

                // tries cached/local delay prediction service if api fails
                try
                {
                    var cached = _predictions.DelayPredictionToday(selectedRoute.OriginAirportCode); // gets fallback delay prediction

                    textAirlinePredictionInfo.Inlines.Clear();
                    textAirlinePredictionInfo.Inlines.Add(new Run("Prediction: ")
                    {
                        FontWeight = FontWeights.Bold,
                        Foreground = textColour
                    });

                    // displays cached delay prediction
                    textAirlinePredictionInfo.Inlines.Add(new Run(
                        $"{cached.Status} ({cached.Percentage}%) on {cached.Date:dd/MM/yyyy}")
                    {
                        Foreground = textColour
                    });
                }
                catch
                {
                    // final fallback if even cached data is not available
                    textAirlinePredictionInfo.Inlines.Clear();
                    textAirlinePredictionInfo.Inlines.Add(new Run("Prediction: ")
                    {
                        FontWeight = FontWeights.Bold,
                        Foreground = textColour
                    });
                    textAirlinePredictionInfo.Inlines.Add(new Run("Unknown")
                    {
                        Foreground = textColour
                    });
                }
            }
        }
    }
}
