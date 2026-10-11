using LocationTracker.Core;
using Microsoft.Maui.Controls.Maps;
using Microsoft.Maui.Maps;
using MapControl = Microsoft.Maui.Controls.Maps.Map;
namespace LocationTracker.App;
public static class HeatMapRenderer
{
    public static void Draw(MapControl map, IEnumerable<GeoSample> samples)
    {
        map.MapElements.Clear();
        foreach (var cell in HeatMapBuilder.Build(samples))
        {
            var color = cell.Intensity <= .25 ? Colors.Blue :
                cell.Intensity <= .5 ? Colors.LimeGreen :
                cell.Intensity <= .75 ? Colors.Orange : Colors.Red;
            // Three transparent circles soften the edge of each frequency cell.
            foreach (var (radius, opacity) in new[] { (60d, .08f), (40d, .16f), (22d, .42f) })
                map.MapElements.Add(new Circle
                {
                    Center = new Location(cell.Latitude, cell.Longitude),
                    Radius = Distance.FromMeters(radius),
                    FillColor = color.WithAlpha(opacity),
                    StrokeColor = Colors.Transparent, StrokeWidth = 0
                });
        }
    }
}
