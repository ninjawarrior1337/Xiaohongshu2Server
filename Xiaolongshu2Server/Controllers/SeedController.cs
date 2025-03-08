using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xiaolongshu2Model;
using Xiaolongshu2Server.Data;

namespace Xiaolongshu2Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SeedController(WorldcitiesSrcContext context, IHostEnvironment environment) : ControllerBase
    {
        string _pathName = Path.Combine(environment.ContentRootPath, "Data", "worldcities.csv");

        [HttpPost("Countries")]
        public async Task<ActionResult> ImportCountriesAsync()
        {
            // create a lookup dictionary containing all the countries already existing 
            // into the Database (it will be empty on first run).
            Dictionary<string, Country> countriesByName = context.Countries
                .AsNoTracking().ToDictionary(x => x.Name, StringComparer.OrdinalIgnoreCase);

            CsvConfiguration config = new(CultureInfo.InvariantCulture)
            {
                HasHeaderRecord = true,
                HeaderValidated = null
            };

            using StreamReader reader = new(_pathName);
            using CsvReader csv = new(reader, config);

            List<WorldCitiesDto> records = csv.GetRecords<WorldCitiesDto>().ToList();

            var promises = records
                .Where(t => !countriesByName.ContainsKey(t.country))
                .Select(t => new Country()
                {
                    Name = t.country,
                    Iso2 = t.iso2,
                    Iso3 = t.iso3
                })
                .Select(t => context.Countries.AddAsync(t).AsTask());

            Task.WaitAll(promises.ToArray());

            //foreach (WorldCitiesDto record in records)
            //{
            //    if (countriesByName.ContainsKey(record.country))
            //    {
            //        continue;
            //    }

            //    Country country = new()
            //    {
            //        Name = record.country,
            //        Iso2 = record.iso2,
            //        Iso3 = record.iso3
            //    };
            //    await context.Countries.AddAsync(country);
            //    countriesByName.Add(record.country, country);
            //}

            await context.SaveChangesAsync();

            return new JsonResult(countriesByName.Count);
        }

        [HttpPost("Cities")]
        public async Task<ActionResult> ImportCitiesAsync()
        {
            Dictionary<string, Country> countries = await context.Countries//.AsNoTracking()
            .ToDictionaryAsync(c => c.Name);

            CsvConfiguration config = new(CultureInfo.InvariantCulture)
            {
                HasHeaderRecord = true,
                HeaderValidated = null
            };
            int cityCount = 0;
            using (StreamReader reader = new(_pathName))
            using (CsvReader csv = new(reader, config))
            {
                IEnumerable<WorldCitiesDto>? records = csv.GetRecords<WorldCitiesDto>();
                foreach (WorldCitiesDto record in records)
                {
                    if (!countries.TryGetValue(record.country, out Country? value))
                    {
                        Console.WriteLine($"Not found country for {record.city}");
                        return NotFound(record);
                    }

                    if (!record.population.HasValue || string.IsNullOrEmpty(record.city_ascii))
                    {
                        Console.WriteLine($"Skipping {record.city}");
                        continue;
                    }
                    City city = new()
                    {
                        Name = record.city,
                        Lat = record.lat,
                        Lon = record.lng,
                        Population = (int)record.population.Value,
                        CountryId = value.Id
                    };
                    context.Cities.Add(city);
                    cityCount++;
                }
                await context.SaveChangesAsync();
            }
            return new JsonResult(cityCount);
        }
    }
}
