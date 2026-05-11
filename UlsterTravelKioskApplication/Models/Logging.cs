using System;

namespace UlsterTravelKioskApplication.Models
{
    public class Logging  // rows in the Logs CSV file (logs.csv)
    {
        public string Description { get; set; } = ""; // "API Call", "API Response", "Error"
        public string Details { get; set; } = "";     // details message
        public DateTime Timestamp { get; set; }       // dd/MM/yyyy HH:mm
    }
}
