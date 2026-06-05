using System.Collections.Generic;

public class OsrmResponse
{
    public List<OsrmRoute> Routes { get; set; } = new List<OsrmRoute>();
}

public class OsrmRoute
{
    public double Distance { get; set; } // metros
    public double Duration { get; set; } // segundos
}
