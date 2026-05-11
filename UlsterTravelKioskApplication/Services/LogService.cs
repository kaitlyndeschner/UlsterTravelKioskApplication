using System; // provides basic system types
using System.Diagnostics; // allows writing debug output (if logging fails)
using System.IO; // file and directory handling
using System.Text; // enables encoded text file output


namespace UlsterTravelKioskApplication.Services
{
    // handles writing logs to the Logs CSV file (logs.csv)
    public class LogService
    {
        private readonly string _logPath; // path to the logs.csv file

        // constrctor for setting up the Data folder and log file location
        public LogService()
        {
            string dataDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data");
            Directory.CreateDirectory(dataDir); // ensures Data folder exists

            _logPath = Path.Combine(dataDir, "logs.csv"); // full path to the logs CSV file

            EnsureHeader(); // ensures header exists before writing logs
        }

        // method for ensuring logs.sv exists and contains a header row
        private void EnsureHeader()
        {
            try
            {
                // if log CSV file doesn't exist, create a default file
                if (!File.Exists(_logPath))
                {
                    File.WriteAllText(_logPath, "Timestamp,Description,Details" + Environment.NewLine, Encoding.UTF8);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("LOG HEADER FAILED: " + ex); // prevents log errors from crashing the app
            }
        }

        // method for adding log entries to the Logs CSV file
        public void AddLog(string description, string details)
        {
            try
            {
                EnsureHeader();

                // creates one row in CSV with current timestamp
                string line =
                    $"{DateTime.Now:dd/MM/yyyy HH:mm},{description},{details}";

                // opens log file in append to ensure logs are not overwritten
                using var fs = new FileStream(_logPath, FileMode.Open, FileAccess.Write, FileShare.ReadWrite);
                fs.Seek(0, SeekOrigin.End); // moves to end of the file
                using var sw = new StreamWriter(fs, Encoding.UTF8);
                sw.WriteLine(line); // writes log entry
            }
            catch (Exception ex)
            {
                Debug.WriteLine("LOG WRITE FAILED: " + ex);
            }
        }
    }
}
