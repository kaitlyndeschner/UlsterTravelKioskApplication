namespace UlsterTravelKioskApplication.Models
{
    public class Route // rows in the Routes CSV file (routes.csv)
    {
        public string OriginAirportCode { get; set; } = ""; // "DUB"
        public string DestinationAirportCode { get; set; } = ""; // "YXE"
        public string AirlineCode { get; set; } = ""; // "WS"
        public string DestinationDisplay { get; set; } = "";

        public string RouteDisplay { get; set; } = "";
    }
}
