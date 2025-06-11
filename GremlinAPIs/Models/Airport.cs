namespace GremlinAPIs.Models
{
    record Airport
    {
        public string Name { get; set; }
        public string City { get; set; }
        public string Country { get; set; }
        public string Iata { get; set; }
        public string Icao { get; set; }
        public double Lat { get; set; }
        public double Lon { get; set; }
    }
}
