using SQLite;
namespace LocationTracker.Data;
[Table("Locations")]
public sealed class LocationRecord
{
    [PrimaryKey, AutoIncrement] public int Id { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    [Indexed] public long TimestampUtcTicks { get; set; }
    public double? AccuracyMeters { get; set; }
}
