using CsvHelper;
using Gremlin.Net.Driver;
using Gremlin.Net.Structure.IO.GraphSON;
using GremlinAPIs.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Collections.Generic;
using System.Formats.Asn1;
using System.Globalization;

namespace GremlinAPIs.Services
{
    public class AirportsServices
    {
        GremlinClient gremlinClient;
        IWebHostEnvironment _env;


        public AirportsServices(IOptions<GremlinSettings> options, IWebHostEnvironment env) 
        {
            _env = env;

            var hostname = options.Value.Host;
            var port = Convert.ToInt32(options.Value.Port);
            
            var gremlinServer = new GremlinServer(hostname, port, enableSsl: false, username: "", password: "");

            IMessageSerializer serializer = new GraphSON3MessageSerializer();

            gremlinClient = new GremlinClient(gremlinServer, serializer);
        }


        public async Task LoadData(string countryFilter = "Finland") 
        {
            var readVertices = new Dictionary<string, Airport>();
            var linkedVertices = new Dictionary<string, Airport>();
            var edges = new List<Models.Route>();

            await DropAll();
            ReadVertices(readVertices);
            ReadRoutes(readVertices, linkedVertices, edges, countryFilter);
            await WrittenVertices(linkedVertices);
            await WrittenEdges(edges);
        }

        async Task DropAll()
        {
            var dropQuery = "g.V().drop()";
            await gremlinClient.SubmitAsync<dynamic>(dropQuery);
        }


        void ReadVertices(Dictionary<string, Airport> readVertices)
        {
            var filePath = Path.Combine(_env.ContentRootPath, "data", "airports.dat");

            using (var airportReader = new StreamReader(filePath))
            using (var csv = new CsvReader(airportReader, CultureInfo.InvariantCulture))
            {
                while (csv.Read())
                {

                    var id = csv.GetField(0);

                    var x = csv.GetField(6);
                    double.TryParse(csv.GetField(6), out var lat1);

                    var airport = new Airport
                    {
                        Name = csv.GetField(1),
                        City = csv.GetField(2),
                        Country = csv.GetField(3),
                        Iata = csv.GetField(4),
                        Icao = csv.GetField(5),
                        Lat = double.TryParse(csv.GetField(6), NumberStyles.Float, CultureInfo.InvariantCulture, out var lat) ? lat : 0,
                        Lon = double.TryParse(csv.GetField(7), NumberStyles.Float, CultureInfo.InvariantCulture, out var lon) ? lon : 0
                    };

                    readVertices[id] = airport;
                }
            }
        }

        void ReadRoutes(Dictionary<string, Airport> readVertices, Dictionary<string, Airport> linkedVertices,
            List<Models.Route> edges, string countryFilter)
        {
            var filePath = Path.Combine(_env.ContentRootPath, "data", "routes.dat");

            using (var routeReader = new StreamReader(filePath))
            using (var csv = new CsvReader(routeReader, CultureInfo.InvariantCulture))
            {
                while (csv.Read())
                {
                    var originId = csv.GetField(3);
                    var destId = csv.GetField(5);

                    if (!string.IsNullOrWhiteSpace(originId) && !string.IsNullOrWhiteSpace(destId) &&
                        readVertices.ContainsKey(originId) &&
                        readVertices.ContainsKey(destId) &&
                        readVertices[originId].Country == countryFilter &&
                        readVertices[destId].Country == countryFilter)
                    {
                        linkedVertices.TryAdd(originId, readVertices[originId]);
                        linkedVertices.TryAdd(destId, readVertices[destId]);

                        var distance = GetDistance(readVertices[originId].Lat, readVertices[originId].Lon,
                                                   readVertices[destId].Lat, readVertices[destId].Lon);

                        edges.Add(new Models.Route
                        {
                            AirlineId = csv.GetField(0),
                            OriginId = originId,
                            DestinationId = destId,
                            Distance = distance
                        });
                    }
                }
            }
        }

        async Task WrittenVertices(Dictionary<string, Airport> linkedVertices)
        {
            foreach (var kvp in linkedVertices)
            {
                var id = kvp.Key;
                var a = kvp.Value;

                var query = @"
                    g.V().hasLabel('airport').has('airportId', airportId)
                     .fold()
                     .coalesce(
                         unfold(),
                         addV('airport')
                            .property('airportId', airportId)
                            .property('name', name)
                            .property('city', city)
                            .property('country', country)
                            .property('iata', iata)
                            .property('icao', icao)
                            .property('lat', lat)
                            .property('lon', lon)
                     )";

                var parameters = new Dictionary<string, object>
                {
                    {"airportId", id},
                    {"name", a.Name},
                    {"city", a.City},
                    {"country", a.Country},
                    {"iata", a.Iata},
                    {"icao", a.Icao},
                    {"lat", a.Lat},
                    {"lon", a.Lon}
                };

                await gremlinClient.SubmitAsync<dynamic>(query, parameters);
            }
        }

        async Task WrittenEdges(List<Models.Route> edges)
        {
            foreach (var edge in edges)
            {
                string query = @"
                    g.V().hasLabel('airport').has('airportId', originId)
                     .as('a')
                     .V().hasLabel('airport').has('airportId', destinationId)
                     .as('b')
                     .coalesce(
                         __.select('a').outE('fliesTo').where(inV().as('b')),
                         __.select('a').addE('fliesTo').to('b')
                             .property('airlineId', airlineId)
                             .property('distance', distance)
                    )";


                var parameters = new Dictionary<string, object>
                {
                    {"originId", edge.OriginId},
                    {"destinationId", edge.DestinationId},
                    {"airlineId", edge.AirlineId},
                    {"distance", edge.Distance}
                };

                try
                {
                    await gremlinClient.SubmitAsync<dynamic>(query, parameters);
                }
                catch (Exception ex) 
                { 
                   var message = ex.Message;    
                }
              
            }
        }

        public async Task<List<dynamic>> GetAirports()
        {
            string gremlinStr = "g.V().hasLabel('airport')";
            var results = await gremlinClient.SubmitAsync<dynamic>(gremlinStr);
            List<dynamic> airports = new List<dynamic>();
            foreach (var result in results)
            {
                airports.Add(result);
            }

            return airports;
        }

        public async Task<dynamic> Getiata(string iata)
        {
            string gremlinStr = String.Format("g.V().hasLabel('airport').has('iata','{0}')", iata);
            var airport = await gremlinClient.SubmitAsync<dynamic>(gremlinStr);
            return airport;
        }

        public async Task<dynamic> GetDestinations(string origin)
        {
            string gremlinStr = String.Format("g.V().has('iata','{0}').outE().inV()", origin);
            var results = await gremlinClient.SubmitAsync<dynamic>(gremlinStr);
            List<dynamic> airports = new List<dynamic>();
            foreach (var result in results)
            {
                airports.Add(result);
            }
            return airports;
        }


        public async Task<dynamic> GetRoute(string origin, string destination)
        {
            string gremlinStr = String.Format("g.V().has('iata','{0}').repeat(out().simplePath()).until(has('iata','{1}')).path().limit(1)", origin, destination);
            var route = await gremlinClient.SubmitAsync<dynamic>(gremlinStr);
            return route;
        }


        double GetDistance(double lat1, double lon1, double lat2, double lon2)
        {
            const double R = 6371; // km
            var dLat = DegreesToRadians(lat2 - lat1);
            var dLon = DegreesToRadians(lon2 - lon1);
            var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                    Math.Cos(DegreesToRadians(lat1)) * Math.Cos(DegreesToRadians(lat2)) *
                    Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            return R * c;
        }

        double DegreesToRadians(double deg) => deg * (Math.PI / 180);

    }
}
