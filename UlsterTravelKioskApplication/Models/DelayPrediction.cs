using System;

namespace UlsterTravelKioskApplication.Models
{
    public class DelayPrediction // rows in the delayPredictions CSV file (delayPredictions.csv)
    {
        public string AirportCode { get; set; } = ""; // "YXE"

        public string Status { get; set; } = ""; // "On time/Delayed"

        public int Percentage { get; set; } // "0-100"

        public DateTime Date { get; set; } // Date of delay predictions
    }
}
