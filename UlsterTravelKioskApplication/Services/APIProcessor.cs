using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UlsterTravelKioskApplication.Models;

namespace UlsterTravelKioskApplication.Services
{
    // handles API processing
    public class APIProcessor
    {
        private readonly DataManager _data;
        private readonly APIHelper _apiHelper;
        private readonly LogService _log;

        // constructor receives shared services (DataManager, APIHelper, LogService)
        public APIProcessor(DataManager data, APIHelper apiHelper, LogService log)
        {
            _data = data;
            _apiHelper = apiHelper;
            _log = log;
        }

        // known-busy hubs see worse on-time performance in real life — apply a penalty
        // so the demo data reflects that hubs are more delay-prone than regional airports.
        private static readonly HashSet<string> BusyHubs = new(StringComparer.OrdinalIgnoreCase)
        {
            "LHR", "LGW", "CDG", "AMS", "FRA", "MAD", "FCO", "MUC", "IST",
            "JFK", "EWR", "LAX", "ORD", "ATL", "DFW",
            "DXB", "DOH", "PEK", "PVG", "HND", "ICN", "SIN", "HKG"
        };

        // returns a deterministic mock delay prediction for the selected airport.
        // the Amadeus self-service API portal was shut down, so this stands in for the
        // live on-time prediction call. result is stable for a given airport+date,
        // and varies day to day so demo screenshots don't always look identical.
        public Task<DelayPrediction> GetAirportPrediction(string airportCode)
        {
            airportCode = (airportCode ?? "").Trim().ToUpper();

            if (airportCode.Length != 3)
            {
                return Task.FromResult(new DelayPrediction
                {
                    AirportCode = airportCode,
                    Date = DateTime.Today,
                    Status = "Unknown",
                    Percentage = 0
                });
            }

            // hash airport code + today's date so the value evolves day to day
            // (string.GetHashCode is randomised per process — we roll our own for stability)
            int seed = 0;
            foreach (char c in airportCode) seed = seed * 31 + c;
            seed = seed * 31 + DateTime.Today.Year;
            seed = seed * 31 + DateTime.Today.DayOfYear;

            // averaging two rolls produces a centred distribution rather than uniform —
            // most values land near the middle, with a thinner tail at the extremes
            int rollA = Math.Abs(seed) % 35;
            int rollB = Math.Abs(seed / 13) % 35;
            int percent = 65 + (rollA + rollB) / 2;

            if (BusyHubs.Contains(airportCode)) percent -= 12;

            percent = Math.Clamp(percent, 45, 99);

            string status =
                percent >= 85 ? "On time"
                : percent >= 70 ? "Possible delays"
                : "Delayed";

            _log.AddLog("Mock", $"Mock prediction airportCode={airportCode} percent={percent} status={status}");

            return Task.FromResult(new DelayPrediction
            {
                AirportCode = airportCode,
                Date = DateTime.Today,
                Status = status,
                Percentage = percent
            });
        }

        // gets destination airports from selected airport
        public async Task<List<Route>> GetAirportDirectDestinations(string departureAirportCode)
        {
            departureAirportCode = (departureAirportCode ?? "").Trim().ToUpper();

            // uses cached data if not stale/older than 7 days
            if (!_data.IsDataStale(_data.Settings.AirportRoutesRefresh))
            {
                var cachedForAirport = _data.AirportDirectDestinations
                    .Where(r => r.OriginAirportCode == departureAirportCode)
                    .ToList();

                if (cachedForAirport.Count > 0)
                    return cachedForAirport;
            }

            string apiKey = _data.Settings.APIKeyA;
            string apiSecret = _data.Settings.APISecretA;

            // uses routes CSV file (routes.csv) if API key and/or secret are missing
            if (string.IsNullOrWhiteSpace(apiKey) || string.IsNullOrWhiteSpace(apiSecret))
            {
                _log.AddLog("API", "No API credentials. Using routes.csv fallback for airport destinations.");

                return _data.Routes
                    .Where(r => r.OriginAirportCode.Equals(departureAirportCode, StringComparison.OrdinalIgnoreCase))
                    .GroupBy(r => r.DestinationAirportCode)
                    .Select(g => new Route
                    {
                        OriginAirportCode = departureAirportCode,
                        DestinationAirportCode = g.Key,
                        AirlineCode = ""
                    })
                    .OrderBy(r => r.DestinationAirportCode)
                    .ToList();
            }

            try
            {
                _log.AddLog("API Call", $"GET /v1/airport/direct-destinations departureAirportCode={departureAirportCode}");

                string token = await _apiHelper.GetAccessTokenAsync(apiKey, apiSecret);
                var destCodes = await _apiHelper.GetAirportDirectDestinationsAsync(token, departureAirportCode);

                _log.AddLog("API Response", $"Direct destinations returned {destCodes.Count} results for {departureAirportCode}");

                // removes old data for selected airport
                _data.AirportDirectDestinations.RemoveAll(r => r.OriginAirportCode == departureAirportCode);

                // stores new data
                foreach (var code in destCodes)
                {
                    _data.AirportDirectDestinations.Add(new Route
                    {
                        OriginAirportCode = departureAirportCode,
                        DestinationAirportCode = code,
                        AirlineCode = ""
                    });
                }

                _data.SaveAirportDirectDestinations(); // saves cached data to CSV

                _data.Settings.AirportRoutesRefresh = DateTime.Now; // updates refresh time
                _data.SaveSettings();

                return _data.AirportDirectDestinations
                    .Where(r => r.OriginAirportCode == departureAirportCode)
                    .ToList();
            }
            catch (Exception ex)
            {
                _log.AddLog("Error", $"Direct destinations failed for {departureAirportCode}: {ex.Message}");
                return new List<Route>();
            }
        }

        // gets destination airports for selected airline
        public async Task<List<Route>> GetAirlineDestinations(string airlineCode)
        {
            airlineCode = (airlineCode ?? "").Trim().ToUpper();

            // uses cached data is not stale/older than 7 days
            if (!_data.IsDataStale(_data.Settings.AirlineDestinationsRefresh))
            {
                var cachedForAirline = _data.AirlineDestinations
                    .Where(r => r.AirlineCode == airlineCode)
                    .ToList();

                if (cachedForAirline.Count > 0)
                    return cachedForAirline;
            }

            string apiKey = _data.Settings.APIKeyA;
            string apiSecret = _data.Settings.APISecretA;

            // uses routes CSV file (routes.csv) if API key and/or secret are missing
            if (string.IsNullOrWhiteSpace(apiKey) || string.IsNullOrWhiteSpace(apiSecret))
            {
                _log.AddLog("API", "No API credentials. Using routes.csv fallback for airline destinations.");

                return _data.Routes
                    .Where(r => r.AirlineCode.Equals(airlineCode, StringComparison.OrdinalIgnoreCase))
                    .GroupBy(r => r.DestinationAirportCode)
                    .Select(g => new Route
                    {
                        AirlineCode = airlineCode,
                        OriginAirportCode = "", // OriginAirportCode not required here
                        DestinationAirportCode = g.Key
                    })
                    .OrderBy(r => r.DestinationAirportCode)
                    .ToList();
            }

            try
            {
                _log.AddLog("API Call", $"GET /v1/airline/destinations airlineCode={airlineCode}");

                string token = await _apiHelper.GetAccessTokenAsync(apiKey, apiSecret);
                var destCodes = await _apiHelper.GetAirlineDestinationsAsync(token, airlineCode);

                _log.AddLog("API Response", $"Airline destinations returned {destCodes.Count} results for {airlineCode}");

                _data.AirlineDestinations.RemoveAll(r => r.AirlineCode == airlineCode); // removes old cached data for selected airline

                // stores new results
                foreach (var code in destCodes)
                {
                    _data.AirlineDestinations.Add(new Route
                    {
                        AirlineCode = airlineCode,
                        OriginAirportCode = "",
                        DestinationAirportCode = code
                    });
                }

                _data.SaveAirlineDestinations(); // persists cached data to CSV

                _data.Settings.AirlineDestinationsRefresh = DateTime.Now; // updates refresh timestamp
                _data.SaveSettings();

                return _data.AirlineDestinations
                    .Where(r => r.AirlineCode == airlineCode)
                    .ToList();
            }
            catch (Exception ex)
            {
                _log.AddLog("Error", $"Airline destinations failed for {airlineCode}: {ex.Message}");
                return new List<Route>();
            }
        }
    }
}
