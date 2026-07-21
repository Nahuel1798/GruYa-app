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

    public List<LegData> Legs { get; set; } = [];
}

public class LegData
{
    public List<StepData> Steps { get; set; } = [];
}

public class StepData
{
    public double Distance { get; set; }

    public double Duration { get; set; }

    public string Name { get; set; } = "";

    public ManeuverData Maneuver { get; set; } = null!;
}

public class ManeuverData
{
    public string Type { get; set; } = "";

    public string Modifier { get; set; } = "";

    public string? Instruction { get; set; }
}

public class GeometryData
{
    public string Type { get; set; } = "";

    public List<List<double>> Coordinates { get; set; } = [];
}
