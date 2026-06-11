using Microsoft.EntityFrameworkCore;

namespace GruYaApi.Models
{
    [Owned]
    public class Location
    {
        public decimal Latitude { get; set; }

        public decimal Longitude { get; set; }
    }
}