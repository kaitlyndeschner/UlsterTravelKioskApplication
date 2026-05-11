namespace UlsterTravelKioskApplication.Models
{
    public class Destinations // rows in the Destinations CSV file (destinations.csv)
    {
        public string Country { get; set; } = ""; // "Canada"
        public string City { get; set; } = "";  // "Saskatoon"
        public string Description { get; set; } = "";  // " Prairie city known for bridges river valley and sunshine"

        public string DestinationCountryCode { get; set; } = ""; // "UK"
        public string IataCode { get; set; } = ""; // "BFS"
        public string Name { get; set; } = "";

        public string DestinationDisplay => $"{Name} ({IataCode})";
    }
}
