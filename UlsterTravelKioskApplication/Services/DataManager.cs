using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using UlsterTravelKioskApplication.Models; // provides access to model classes

namespace UlsterTravelKioskApplication.Services
{
    public class DataManager
    {
        // in-memory lists
        public List<Airline> Airlines { get; } = new();
        public List<Airport> Airports { get; } = new();
        public List<Route> Routes { get; } = new();
        public List<Destinations> Destinations { get; } = new();
        public List<DelayPrediction> DelayPredictions { get; } = new();
        public List<Logging> Logs { get; } = new();

        // API cache lists (backed up in CSV files)
        public List<Route> AirportDirectDestinations { get; } = new();
        public List<Route> AirlineDestinations { get; } = new();

        public Settings Settings { get; } = new(); // loaded from settings.csv (admin login, API keys, refresh date/times)

        private readonly string _data; // stores the Data folder path

        public string DataFolderPath => _data; // shows the Data folder path to the UI

        // constructor
        public DataManager()
        {
            _data = Path.Combine(AppContext.BaseDirectory, "Data"); // locates Data folder
            Directory.CreateDirectory(_data); // ensures Data folder exists prior to reading and writing CSV files
        }

        // loads all CSV files into lists
        public void LoadData()
        {
            // clears first to prevent data duplication
            Airlines.Clear();
            Airports.Clear();
            Routes.Clear();
            Destinations.Clear();
            DelayPredictions.Clear();
            Logs.Clear();
            AirportDirectDestinations.Clear();
            AirlineDestinations.Clear();

            // loads data from their CSV file
            LoadAirlines();
            LoadAirports();
            LoadRoutes();
            LoadDestinations();
            LoadDelayPredictions();
            LoadLogs();
            LoadSettings();
            LoadAirportDirectDestinations();
            LoadAirlineDestinations();

            // Reference codes (IATA airport/airline) are essentially static, so a stale
            // timestamp should not lock the kiosk out when the data itself is present.
            // Bump the refresh marker when CSVs are loaded with data but the timestamp
            // is past the 7-day staleness window. Empty lists (admin "clear") are left
            // alone so the missing-data block still fires.
            var staleCutoff = DateTime.Now.AddDays(-7);
            bool settingsChanged = false;
            if (Airports.Count > 0 && Settings.AirportsRefresh < staleCutoff)
            {
                Settings.AirportsRefresh = DateTime.Now;
                settingsChanged = true;
            }
            if (Airlines.Count > 0 && Settings.AirlinesRefresh < staleCutoff)
            {
                Settings.AirlinesRefresh = DateTime.Now;
                settingsChanged = true;
            }
            if (settingsChanged) SaveSettings();
        }

        // saves lists back to CSV files
        public void SaveData()
        {
            SaveAirlines();
            SaveAirports();
            SaveDestinations();
            SaveDelayPredictions();
            SaveLogs();
            SaveSettings();
            SaveAirportDirectDestinations();
            SaveAirlineDestinations();
        }

        // reads airlines CSV file into Airlines list
        public void LoadAirlines()
        {
            string path = Path.Combine(_data, "airlines.csv");
            if (!File.Exists(path)) return;

            var lines = File.ReadAllLines(path);
            for (int i = 1; i < lines.Length; i++) // starts at 1 (skips header row)
            {
                if (string.IsNullOrWhiteSpace(lines[i])) continue;

                var parts = lines[i].Split(',');
                if (parts.Length < 2) continue;

                Airlines.Add(new Airline
                {
                    AirlineCode = parts[0].Trim(),
                    AirlineName = parts[1].Trim()
                });
            }
        }

        // writes Airlines list back to airlines.csv
        public void SaveAirlines()
        {
            string path = Path.Combine(_data, "airlines.csv");

            var lines = new List<string> { "AirlineCode,AirlineName" };
            foreach (var a in Airlines)
                lines.Add($"{a.AirlineCode},{a.AirlineName}");

            File.WriteAllLines(path, lines);
        }

        // imports airline CSV from user selection
        public int ImportAirlinesFromCSV(string sourcePath)
        {
            if (!File.Exists(sourcePath))
                throw new Exception("Selected file does not exist");

            var lines = File.ReadAllLines(sourcePath);
            if (lines.Length < 2)
                throw new Exception("CSV file is empty or missing data rows");

            Airlines.Clear(); // replaces existing airlines list with imported data

            for (int i = 1; i < lines.Length; i++) // starts at 1 (skips header row)
            {
                if (string.IsNullOrWhiteSpace(lines[i])) continue;

                var parts = lines[i].Split(',');
                if (parts.Length < 2) continue;

                string code = parts[0].Trim();
                string name = parts[1].Trim();

                if (string.IsNullOrWhiteSpace(code)) continue;

                Airlines.Add(new Airline
                {
                    AirlineCode = code,
                    AirlineName = name
                });
            }

            SaveAirlines(); // saves imported datato the Data folder
            return Airlines.Count;
        }

        // reads airports.csv into the Airports list
        public void LoadAirports()
        {
            string path = Path.Combine(_data, "airports.csv");
            if (!File.Exists(path)) return;

            var lines = File.ReadAllLines(path);
            for (int i = 1; i < lines.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(lines[i])) continue;

                var parts = lines[i].Split(',');
                if (parts.Length < 4) continue;

                Airports.Add(new Airport
                {
                    AirportCode = parts[0].Trim(),
                    AirportName = parts[1].Trim(),
                    City = parts[2].Trim(),
                    Country = parts[3].Trim()
                });
            }
        }

        // writes Airports list back to the airports.csv
        public void SaveAirports()
        {
            string path = Path.Combine(_data, "airports.csv");

            var lines = new List<string> { "AirportCode,AirportName,City,Country" };
            foreach (var a in Airports)
                lines.Add($"{a.AirportCode},{a.AirportName},{a.City},{a.Country}");

            File.WriteAllLines(path, lines);
        }

        // imports airports CSV from user selection
        public int ImportAirportsFromCSV(string sourcePath)
        {
            if (!File.Exists(sourcePath))
                throw new Exception("Selected file does not exist"); // displays error message if selected csv file doesn't exist

            var lines = File.ReadAllLines(sourcePath);
            if (lines.Length < 2)
                throw new Exception("CSV file is empty or missing data rows"); // displays error message if csv file is empty or invalid

            Airports.Clear(); // replaces existing Airports list with imported data

            for (int i = 1; i < lines.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(lines[i])) continue;

                var parts = lines[i].Split(',');
                if (parts.Length < 4) continue;

                string code = parts[0].Trim();
                string name = parts[1].Trim();
                string city = parts[2].Trim();
                string country = parts[3].Trim();

                if (string.IsNullOrWhiteSpace(code)) continue;

                Airports.Add(new Airport
                {
                    AirportCode = code,
                    AirportName = name,
                    City = city,
                    Country = country
                });
            }

            SaveAirports(); // saves existing Airports list with imported data
            return Airports.Count;
        }

        // reads routes.csv into Routes list
        public void LoadRoutes()
        {
            string path = Path.Combine(_data, "routes.csv");
            if (!File.Exists(path)) return;

            var lines = File.ReadAllLines(path);
            for (int i = 1; i < lines.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(lines[i])) continue;

                var parts = lines[i].Split(',');
                if (parts.Length < 3) continue;

                Routes.Add(new Route
                {
                    OriginAirportCode = parts[0].Trim(),
                    DestinationAirportCode = parts[1].Trim(),
                    AirlineCode = parts[2].Trim()
                });
            }

            // real-world scheduled airline routes are virtually always bidirectional,
            // so synthesise the reverse for any one-way entry. fixes airports that
            // only appear as destinations in routes.csv showing zero outbound routes.
            var existing = new HashSet<(string, string, string)>(
                Routes.Select(r => (
                    r.OriginAirportCode.ToUpperInvariant(),
                    r.DestinationAirportCode.ToUpperInvariant(),
                    r.AirlineCode.ToUpperInvariant())));

            var reverses = new List<Route>();
            foreach (var r in Routes)
            {
                if (string.Equals(r.OriginAirportCode, r.DestinationAirportCode, StringComparison.OrdinalIgnoreCase))
                    continue;

                var reverseKey = (
                    r.DestinationAirportCode.ToUpperInvariant(),
                    r.OriginAirportCode.ToUpperInvariant(),
                    r.AirlineCode.ToUpperInvariant());

                if (existing.Add(reverseKey))
                {
                    reverses.Add(new Route
                    {
                        OriginAirportCode = r.DestinationAirportCode,
                        DestinationAirportCode = r.OriginAirportCode,
                        AirlineCode = r.AirlineCode
                    });
                }
            }
            Routes.AddRange(reverses);
        }

        // reads destinations.csv into Destinations list
        public void LoadDestinations()
        {
            string path = Path.Combine(_data, "destinations.csv");
            if (!File.Exists(path)) return;

            var lines = File.ReadAllLines(path);
            for (int i = 1; i < lines.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(lines[i])) continue;

                var parts = lines[i].Split(',', 3);
                if (parts.Length < 3) continue;

                Destinations.Add(new Destinations
                {
                    Country = parts[0].Trim(),
                    City = parts[1].Trim(),
                    Description = parts[2].Trim()
                });
            }
        }

        // writes Destinations list back to destinations.csv
        public void SaveDestinations()
        {
            string path = Path.Combine(_data, "destinations.csv");

            var lines = new List<string> { "Country,City,Description" };
            foreach (var d in Destinations)
                lines.Add($"{d.Country},{d.City},{d.Description}");

            File.WriteAllLines(path, lines);
        }

        // reads delayPredictions.csv into DelayPredictions list
        public void LoadDelayPredictions()
        {
            string path = Path.Combine(_data, "delayPredictions.csv");
            if (!File.Exists(path)) return;

            var lines = File.ReadAllLines(path);
            for (int i = 1; i < lines.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(lines[i])) continue;

                var parts = lines[i].Split(',');
                if (parts.Length < 4) continue;

                if (!DateTime.TryParse(parts[3].Trim(), out var date))
                    continue;

                int.TryParse(parts[2].Trim(), out int percent); // percent == 0 if parse fails

                DelayPredictions.Add(new DelayPrediction
                {
                    AirportCode = parts[0].Trim(),
                    Status = parts[1].Trim(),
                    Percentage = percent,
                    Date = date
                });
            }
        }

        // writes DelayPredictions list back to delayPredictions.csv
        public void SaveDelayPredictions()
        {
            string path = Path.Combine(_data, "delayPredictions.csv");

            var lines = new List<string> { "AirportCode,Status,Percentage,Date" };
            foreach (var dp in DelayPredictions)
                lines.Add($"{dp.AirportCode},{dp.Status},{dp.Percentage},{dp.Date:yyyy-MM-dd}");

            File.WriteAllLines(path, lines);
        }


        // reads logs.csv into Logs list
        public void LoadLogs()
        {
            string path = Path.Combine(_data, "logs.csv"); // build path to logs.csv
            if (!File.Exists(path)) return; // exits method if logs.csv does not exist

            var lines = File.ReadAllLines(path); // reads all lines from CSV
            Logs.Clear(); // clears existing logs (prevents duplication)

            for (int i = 1; i < lines.Length; i++) // skips header row (starts from 1)
            {
                if (string.IsNullOrWhiteSpace(lines[i])) continue; // ignores empty lines

                
                var parts = lines[i].Split(',', 3); // splits into 3 columns to prevent commas from breaking parsing
                if (parts.Length < 3) continue; // ensures columns exist

                // parses timestamp; reverts to current time if invalid
                if (!DateTime.TryParse(parts[0].Trim(), out DateTime ts))
                    ts = DateTime.Now;

                // adds new log entry to the Logs CSV file
                Logs.Add(new Logging
                {
                    Timestamp = ts,
                    Description = parts[1].Trim().Trim('"'),
                    Details = parts[2].Trim().Trim('"')
                });
            }
        }

        // writes Logs list back to logs.csv
        public void SaveLogs()
        {
            string path = Path.Combine(_data, "logs.csv"); // builds path to logs.csv

            // csv file header
            var lines = new List<string> { "Timestamp,Description,Details" };
            foreach (var l in Logs)
                lines.Add($"{l.Timestamp:dd/MM/yyyy HH:mm},{l.Description},{l.Details}");

            File.WriteAllLines(path, lines); // updates data in csv file
        }

        // reads settings.csv into Settings
        public void LoadSettings()
        {
            string path = Path.Combine(_data, "settings.csv"); // builds path to settings.csv
            if (!File.Exists(path)) return; // exits method if settings csv file does not exist

            var lines = File.ReadAllLines(path);
            if (lines.Length < 2) return;

            // splits into maximum 9 columns
            var parts = lines[1].Split(',', 9);
            if (parts.Length < 2) return;

            // admin credentials
            Settings.AdminUsername = parts[0];
            Settings.AdminPassword = parts[1];

            // api secret and key
            if (parts.Length >= 4)
            {
                Settings.APIKeyA = parts[2];
                Settings.APISecretA = parts[3];
            }

            // main content (refreshes timestamps)
            if (parts.Length >= 7)
            {
                DateTime.TryParse(parts[4], out var airlinesRefresh);
                DateTime.TryParse(parts[5], out var airportsRefresh);
                DateTime.TryParse(parts[6], out var destinationsRefresh);

                Settings.AirlinesRefresh = airlinesRefresh;
                Settings.AirportsRefresh = airportsRefresh;
                Settings.DestinationsRefresh = destinationsRefresh;
            }

            // airport routes (refreshes timestamp)
            if (parts.Length >= 8)
            {
                DateTime.TryParse(parts[7], out var airportRoutesRefresh);
                Settings.AirportRoutesRefresh = airportRoutesRefresh;
            }

            // airline destinations (refreshes timestamp)
            if (parts.Length >= 9)
            {
                DateTime.TryParse(parts[8], out var airlineDestRefresh);
                Settings.AirlineDestinationsRefresh = airlineDestRefresh;
            }
        }

        // writes settings back to settings.csv
        public void SaveSettings()
        {
            string path = Path.Combine(_data, "settings.csv");

            
            var lines = new List<string>
            {
                // settings values
                "AdminUsername,AdminPassword,APIKeyA,APISecretA,AirlinesRefresh,AirportsRefresh,DestinationsRefresh,AirportRoutesRefresh,AirlineDestinationsRefresh",
                $"{Settings.AdminUsername},{Settings.AdminPassword},{Settings.APIKeyA},{Settings.APISecretA}," +
                $"{Settings.AirlinesRefresh:yyyy-MM-dd HH:mm:ss},{Settings.AirportsRefresh:yyyy-MM-dd HH:mm:ss},{Settings.DestinationsRefresh:yyyy-MM-dd HH:mm:ss}," +
                $"{Settings.AirportRoutesRefresh:yyyy-MM-dd HH:mm:ss},{Settings.AirlineDestinationsRefresh:yyyy-MM-dd HH:mm:ss}"
            };

            File.WriteAllLines(path, lines); // overwrites csv file with updated settings
        }

        // reads airportDirectDestinations.csv into AirportDirectDestinations list
       public void LoadAirportDirectDestinations()
       {
           string path = Path.Combine(_data, "airportDirectDestinations.csv");
           if (!File.Exists(path)) return; // exits method if file doesn't exist
      
           var lines = File.ReadAllLines(path);
           AirportDirectDestinations.Clear(); // resets list before loading
      
           for (int i = 1; i < lines.Length; i++)
           {
               if (string.IsNullOrWhiteSpace(lines[i])) continue;
      
               var parts = lines[i].Split(',');
               if (parts.Length < 2) continue;
      
               AirportDirectDestinations.Add(new Route
               {
                   OriginAirportCode = parts[0].Trim(),
                   DestinationAirportCode = parts[1].Trim(),
                   AirlineCode = "" //airlineCode not stored here
               });
           }
       }

      // writes AirportDirectDestinations list back to CSV
      public void SaveAirportDirectDestinations()
      {
          string path = Path.Combine(_data, "airportDirectDestinations.csv");
      
          var lines = new List<string> { "OriginAirportCode,DestinationAirportCode" };
          foreach (var r in AirportDirectDestinations)
              lines.Add($"{r.OriginAirportCode},{r.DestinationAirportCode}");
      
          File.WriteAllLines(path, lines);
      }


        // reads airlinedestinations.csv back to the AirlineDestinations list
       public void LoadAirlineDestinations()
       {
           string path = Path.Combine(_data, "airlineDestinations.csv");
           if (!File.Exists(path)) return;
      
           var lines = File.ReadAllLines(path);
           AirlineDestinations.Clear();
      
           for (int i = 1; i < lines.Length; i++) // skips header
           {
               if (string.IsNullOrWhiteSpace(lines[i])) continue;
      
               var parts = lines[i].Split(',');
               if (parts.Length < 2) continue;
      
               AirlineDestinations.Add(new Route
               {
                   AirlineCode = parts[0].Trim(),
                   OriginAirportCode = "", // OriginAirportCode not used here
                   DestinationAirportCode = parts[1].Trim()
               });
           }
       }
      
        // writes AirlineDestinations list back to CSV file
      public void SaveAirlineDestinations()
      {
          string path = Path.Combine(_data, "airlineDestinations.csv");
      
          var lines = new List<string> { "AirlineCode,DestinationAirportCode" };
          foreach (var r in AirlineDestinations)
              lines.Add($"{r.AirlineCode} , {r.DestinationAirportCode}");
      
          File.WriteAllLines(path, lines);
      }

    
        // checks if data is older than 7 days
        public bool IsDataStale(DateTime lastRefresh)
        {
            if (lastRefresh == DateTime.MinValue) return false;
            return (DateTime.Now - lastRefresh).TotalDays > 7;
        }

        // airports data stale if list is empty or data is older than 7 days
        public bool IsAirportsDataStale()
        {
            if (Airports == null || Airports.Count == 0) return true;

            var refresh = Settings?.AirportsRefresh ?? DateTime.MinValue;
            if (refresh == DateTime.MinValue) return false;

            return (DateTime.Now - refresh) > TimeSpan.FromDays(7);
        }

        // airlines data stale if list is empty or data is older than 7 days
        public bool IsAirlinesDataStale()
        {
            if (Airlines == null || Airlines.Count == 0) return true;

            var refresh = Settings?.AirlinesRefresh ?? DateTime.MinValue;
            if (refresh == DateTime.MinValue) return false;

            return (DateTime.Now - refresh) > TimeSpan.FromDays(7);
        }

    }
}
