namespace GremlinAPIs.Models
{
    record Route
    {
        public string AirlineId { get; set; }
        public string OriginId { get; set; }
        public string DestinationId { get; set; }
        public double Distance { get; set; }
    }
}
