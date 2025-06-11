using GremlinAPIs.Services;
using Microsoft.AspNetCore.Mvc;

namespace GremlinAPIs.Endpoints
{
    public static class Airports
    {
        public static void MapAirports(this IEndpointRouteBuilder app)
        {
           
            app.MapGet("/airports/LoadData", async (
                     [FromQuery(Name = "country")] string country,
                     [FromServices] AirportsServices service)
                     => {
                         await service.LoadData(country);
                         return Results.Ok();
                     })
                .WithName("LoadData")
                .WithOpenApi();

            app.MapGet("/Airports/GetAll", GetAll)
               .WithName("GetAll")
               .WithOpenApi();

            app.MapGet("/Airports/Getiata", async (
                     [FromQuery(Name = "aita")] string aita,
                     [FromServices] AirportsServices service)
                     => {
                         var result = await service.Getiata(aita);
                         return Results.Ok(result);
                     })
                 .WithName("Getiata")
                 .WithOpenApi();

            app.MapGet("/Airports/GetDestinations", async (
                    [FromQuery(Name = "orign")] string orign,
                    [FromServices] AirportsServices service)
                    => {
                        var result = await service.GetDestinations(orign);

                        return Results.Ok(result);
                    })
                .WithName("GetDestinations")
                .WithOpenApi();

            app.MapGet("/Airports/GetRoute", async (
                  [FromQuery(Name = "orign")] string orign,
                  [FromQuery(Name = "destination")] string destination,
                  [FromServices] AirportsServices service)
                  => {
                      var result = await service.GetRoute(orign, destination);

                      return Results.Ok(result);
                  })
              .WithName("GetRoute")
              .WithOpenApi();

        }

        private static async Task<IResult> LoadData(AirportsServices service)
        {
            await service.LoadData();
            
            return Results.Ok();
        }

        private static async Task<IResult> GetAll(AirportsServices service)
        {
            var result = await service.GetAirports();

            return Results.Ok(result);
        }
    }
}
