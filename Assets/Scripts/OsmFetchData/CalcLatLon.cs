using System;

public class TileToLatLon
{
    public static (string minLat, string minLon, string maxLat, string maxLon) GetTileBoundingBox(int zoom, int x, int y)
    {
        double n = Math.PI - (2.0 * Math.PI * y) / Math.Pow(2.0, zoom);
        double latMin = (180.0 / Math.PI) * Math.Atan(Math.Sinh(n));
        double lonMin = x / Math.Pow(2.0, zoom) * 360.0 - 180.0;
        double latMax = (180.0 / Math.PI) * Math.Atan(Math.Sinh(n + (2.0 * Math.PI / Math.Pow(2.0, zoom))));
        double lonMax = (x + 1) / Math.Pow(2.0, zoom) * 360.0 - 180.0;

        double RlatMin = Math.Floor(latMin * 10000) / 10000;
        double RlatMax = Math.Floor(latMax * 10000) / 10000;
        double RlonMin = Math.Floor(lonMin * 10000) / 10000;
        double RlonMax = Math.Floor(lonMax * 10000) / 10000;

        string formattedLatMin = FormatCoordinates(RlatMin);
        string formattedLonMin = FormatCoordinates(RlonMin);
        string formattedLatMax = FormatCoordinates(RlatMax);
        string formattedLonMax = FormatCoordinates(RlonMax);

        return (formattedLatMin, formattedLonMin, formattedLatMax, formattedLonMax);
    }

    public static string FormatCoordinates(double latitude)
    {
        // Convert to string and replace comma with dot
        return latitude.ToString("0.0000");
        //.Replace(',', '.')
    }
}

