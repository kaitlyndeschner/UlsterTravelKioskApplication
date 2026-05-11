using System;

namespace UlsterTravelKioskApplication.Models
{
    public class Settings // rows in the Settings CSV file (settings.csv)
    {
        public string AdminUsername { get; set; } = "admin"; // Admin username
        public string AdminPassword { get; set; } = "HASH::admin"; // Admin password (stored securely)


        public string APIKeyA { get; set; } = string.Empty; // API key
        public string APISecretA { get; set; } = string.Empty; // API secret

        public DateTime AirlinesRefresh { get; set; } = DateTime.MinValue; // airlines refresh date
        public DateTime AirportsRefresh { get; set; } = DateTime.MinValue; // airports refresh date
        public DateTime DestinationsRefresh { get; set; } = DateTime.MinValue; // destinations refresh date

        public DateTime AirportRoutesRefresh { get; set; } = DateTime.MinValue;
        public DateTime AirlineDestinationsRefresh { get; set; } = DateTime.MinValue;

        

    }
}
