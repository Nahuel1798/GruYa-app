
namespace GruYaApi.DTOs.Responses
{
    public class OverpassResponse
    {
        public List<OverpassElement> Elements { get; set; } = new();
    }

    public class OverpassElement
    {
        public long Id { get; set; }
        public double Lat { get; set; }
        public double Lon { get; set; }
        public Dictionary<string, string>? Tags { get; set; }
    }
}
