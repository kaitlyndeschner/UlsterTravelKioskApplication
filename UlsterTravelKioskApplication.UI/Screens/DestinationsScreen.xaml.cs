using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using UlsterTravelKioskApplication.Services;
using UlsterTravelKioskApplication.Models;

namespace UlsterTravelKioskApplication.UI.Screens
{
    // destinations screen (view routes and destination info from selected country)
    public partial class DestinationsScreen : Window
    {
        private readonly DataManager _data; // stores airports, routes, destinations and settings loaded from csv files
        private readonly DelayPredictionService _predictions; // local prediction service (fallback/cached logic if needed)
        private readonly APIProcessor _apiProcessor; // handles amadeus api requests and caching rules
        private readonly LogService _log; // writes events and errors to logs.csv

        private string _selectedCountry = "";

        // constructor receives shared services
        public DestinationsScreen(
            DataManager data,
            DelayPredictionService predictions,
            APIProcessor apiProcessor,
            LogService log)
        {
            InitializeComponent(); // loads the XAML UI

            _data = data; // stores the shared data manager instance
            _predictions = predictions; // stores prediction service instance
            _apiProcessor = apiProcessor; // stores api processor instance
            _log = log; // stores log service instance

            // builds list of countries from the airports list
            comboboxDestinations.ItemsSource = _data.Airports
                .Select(a => a.Country)
                .Where(c => !string.IsNullOrWhiteSpace(c))
                .Distinct()
                .OrderBy(c => c)
                .ToList();

            comboboxDestinations.SelectedValuePath = ".";
        }

        // runs when the user clicks confirm after selecting a country
        private void DestinationsConfirmClick(object sender, RoutedEventArgs e)
        {
            // reads selected country from the drop down
            string countrySelected = comboboxDestinations.SelectedValue as string;
            if (string.IsNullOrWhiteSpace(countrySelected)) return; // stops if nothing selected

            _selectedCountry = countrySelected; // stores selection

            // finds all airport codes where the airport is located in the selected country
            var originAirportCodes = _data.Airports
                .Where(a => a.Country == countrySelected)
                .Select(a => a.AirportCode)
                .ToList();

            // filters routes so only routes that start in the selected country are shown
            var routes = _data.Routes
                .Where(r => originAirportCodes.Contains(r.OriginAirportCode))
                .ToList();

            // builds a display string for each route (format: "airport name (code) → airport name (code))
            foreach (var route in routes)
            {
                // lookup origin airport details
                var originAirport = _data.Airports
                    .FirstOrDefault(a => a.AirportCode == route.OriginAirportCode);

                // lookup destination airport details
                var destinationAirport = _data.Airports
                    .FirstOrDefault(a => a.AirportCode == route.DestinationAirportCode);

                // fallback to airportcode if name is not found
                string originText = originAirport != null
                    ? $"{originAirport.AirportName} ({originAirport.AirportCode})"
                    : route.OriginAirportCode;

                string destinationText = destinationAirport != null
                    ? $"{destinationAirport.AirportName} ({destinationAirport.AirportCode})"
                    : route.DestinationAirportCode;

                route.RouteDisplay = $"{originText} → {destinationText}";
            }

            // displays the filtered routes in the listbox
            DestinationsList.ItemsSource = routes;

            // clears old selections
            textDestinationsInfo.Inlines.Clear();
            textDestinationsRouteInfo.Inlines.Clear();
            textDelayPredictionInfo.Inlines.Clear();

            var textColour = textDestinationsInfo.Foreground; // consistent color theme

            // placeholder message
            textDestinationsRouteInfo.Inlines.Add(new Run("Description: ")
            {
                FontWeight = FontWeights.Bold,
                Foreground = textColour
            });
            textDestinationsRouteInfo.Inlines.Add(new Run(
                "Please select a route to view destination description.")
            {
                Foreground = textColour
            });
        }

        // runs when the user selects a route from the listbox
        private async void DestinationSelection(object sender, SelectionChangedEventArgs e)
        {
            var selectedRoute = DestinationsList.SelectedItem as Route; // reads the selected route object
            if (selectedRoute == null) return; // stops if selection is cleared

            var textColour = textDestinationsRouteInfo.Foreground; // consistent color theme

            textDestinationsInfo.Inlines.Clear(); // clears old destination info

            try
            {
                // finds the destination airport record from its airport code
                var destinationAirport = _data.Airports
                    .FirstOrDefault(a => a.AirportCode == selectedRoute.DestinationAirportCode);

                if (destinationAirport != null)
                {
                    // tries to find a matching destination record using country and city
                    var destination = _data.Destinations
                        .FirstOrDefault(d =>
                            d.Country.Equals(destinationAirport.Country, StringComparison.OrdinalIgnoreCase) &&
                            d.City.Equals(destinationAirport.City, StringComparison.OrdinalIgnoreCase));

                    // writes destination heading and city/country
                    textDestinationsInfo.Inlines.Add(new Run("Destination: ")
                    {
                        FontWeight = FontWeights.Bold,
                        Foreground = textColour
                    });
                    textDestinationsInfo.Inlines.Add(new Run(
                        $"{destinationAirport.City}, {destinationAirport.Country}\n")
                    {
                        Foreground = textColour
                    });

                    // writes description heading and description text (or fallback)
                    textDestinationsInfo.Inlines.Add(new Run("Description: ")
                    {
                        FontWeight = FontWeights.Bold,
                        Foreground = textColour
                    });
                    textDestinationsInfo.Inlines.Add(new Run(
                        destination != null
                            ? destination.Description
                            : "No destination description available")
                    {
                        Foreground = textColour
                    });
                }
                else
                {
                    // fallback if airport code was not found in airports list
                    textDestinationsInfo.Inlines.Add(new Run("Destination: ")
                    {
                        FontWeight = FontWeights.Bold,
                        Foreground = textColour
                    });
                    textDestinationsInfo.Inlines.Add(new Run("Unknown")
                    {
                        Foreground = textColour
                    });
                }
            }
            catch (Exception ex)
            {
                // logs the error
                _log.AddLog("Error", $"Destination lookup failed: {ex}");
            }

            textDestinationsRouteInfo.Inlines.Clear(); // clears old route text

            // shows the selected route string that was created in confirm click
            textDestinationsRouteInfo.Inlines.Add(new Run("Route: ")
            {
                FontWeight = FontWeights.Bold,
                Foreground = textColour
            });
            textDestinationsRouteInfo.Inlines.Add(new Run(selectedRoute.RouteDisplay)
            {
                Foreground = textColour
            });


            textDelayPredictionInfo.Inlines.Clear(); // clears old prediction text

            // shows a loading message while awaiting the api call
            textDelayPredictionInfo.Inlines.Add(new Run("Prediction: ")
            {
                FontWeight = FontWeights.Bold,
                Foreground = textColour
            });
            textDelayPredictionInfo.Inlines.Add(new Run("Fetching prediction from Amadeus...")
            {
                Foreground = textColour
            });

            try
            {
                // requests delay prediction using the origin airport code
                var prediction = await _apiProcessor.GetAirportPrediction(
                    selectedRoute.OriginAirportCode);

                textDelayPredictionInfo.Inlines.Clear();
                textDelayPredictionInfo.Inlines.Add(new Run("Prediction: ")
                {
                    FontWeight = FontWeights.Bold,
                    Foreground = textColour
                });

                // displays fallback text if prediction is missing or unknown
                if (prediction == null || prediction.Status == "Unknown")
                {
                    textDelayPredictionInfo.Inlines.Add(new Run("Unknown")
                    {
                        Foreground = textColour
                    });
                }
                else
                {
                    // displays prediction result with percentage and the date used for the api response
                    textDelayPredictionInfo.Inlines.Add(new Run(
                        $"{prediction.Status} ({prediction.Percentage}%) on {prediction.Date:dd/MM/yyyy}")
                    {
                        Foreground = textColour
                    });
                }
            }
            catch (Exception ex)
            {
                // logs api failures
                _log.AddLog("API Error",
                    $"Prediction failed for {selectedRoute.OriginAirportCode}: {ex}");

                // displays message
                textDelayPredictionInfo.Inlines.Clear();
                textDelayPredictionInfo.Inlines.Add(new Run("Prediction: ")
                {
                    FontWeight = FontWeights.Bold,
                    Foreground = textColour
                });
                textDelayPredictionInfo.Inlines.Add(new Run("Unavailable (check logs)")
                {
                    Foreground = textColour
                });
            }
        }

        // clears the screen and resets selection back to default state
        private void ResetClick(object sender, RoutedEventArgs e)
        {
            comboboxDestinations.SelectedIndex = -1; // clears dropdown selection
            DestinationsList.ItemsSource = null; // clears route list

            // clears all details panels
            textDestinationsInfo.Inlines.Clear();
            textDestinationsRouteInfo.Inlines.Clear();
            textDelayPredictionInfo.Inlines.Clear();
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
