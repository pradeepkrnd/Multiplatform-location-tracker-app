namespace LocationTracker.Core;
/// <summary>Spatial frequency heat map. A cell is 0.0005 degrees on each axis.</summary>
public static class HeatMapBuilder
{
    public const double CellDegrees = 0.0005;
    public static bool IsValid(double latitude, double longitude) =>
        double.IsFinite(latitude) && double.IsFinite(longitude) &&
        latitude is >= -90 and <= 90 && longitude is >= -180 and <= 180;

    public static IReadOnlyList<HeatCell> Build(IEnumerable<GeoSample> samples, int maximumCells = 300)
    {
        ArgumentNullException.ThrowIfNull(samples);
        if (maximumCells < 1) throw new ArgumentOutOfRangeException(nameof(maximumCells));
        var groups = samples.Where(p => IsValid(p.Latitude, p.Longitude))
            .GroupBy(p => ((long)Math.Floor(p.Latitude / CellDegrees),
                          (long)Math.Floor(p.Longitude / CellDegrees)))
            .Select(g => new { Latitude = g.Average(p => p.Latitude),
                Longitude = g.Average(p => p.Longitude), Count = g.Count() })
            .OrderByDescending(g => g.Count).ThenBy(g => g.Latitude).ThenBy(g => g.Longitude)
            .Take(maximumCells).ToArray();
        // A singleton stays cool; repeated visits become progressively warmer.
        var scale = Math.Max(5, groups.Select(g => g.Count).DefaultIfEmpty(0).Max());
        return groups.Select(g => new HeatCell(g.Latitude, g.Longitude, g.Count,
            (double)g.Count / scale)).ToArray();
    }
}
