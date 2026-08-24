namespace HomeServicesPortal.Helpers;

public static class DistanceHelper
{
    private const double EarthRadiusKm = 6371.0;

    /// <summary>Great-circle distance in kilometers between two lat/lng points (Haversine formula).</summary>
    public static double HaversineDistanceKm(decimal lat1, decimal lng1, decimal lat2, decimal lng2)
    {
        var dLat = ToRadians((double)(lat2 - lat1));
        var dLng = ToRadians((double)(lng2 - lng1));
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2)
              + Math.Cos(ToRadians((double)lat1)) * Math.Cos(ToRadians((double)lat2))
              * Math.Sin(dLng / 2) * Math.Sin(dLng / 2);
        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return EarthRadiusKm * c;
    }

    private static double ToRadians(double degrees) => degrees * Math.PI / 180.0;
}
