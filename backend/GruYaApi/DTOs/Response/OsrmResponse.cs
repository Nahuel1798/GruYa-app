using System.Collections.Generic;

public class OsrmResponse
{
    public List<RouteData> Routes { get; set; } = [];
}

public class RouteData
{
    public double Distance { get; set; }

    public double Duration { get; set; }

    public GeometryData Geometry { get; set; } = null!;
}

public class GeometryData
{
    public string Type { get; set; } = "";

    public List<List<double>> Coordinates { get; set; } = [];
}
