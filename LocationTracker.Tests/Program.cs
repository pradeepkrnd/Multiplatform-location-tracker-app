using LocationTracker.Core;
using LocationTracker.Data;
SQLitePCL.Batteries_V2.Init();
var passed = 0;
void Check(bool condition, string name)
{
    if (!condition) throw new InvalidOperationException($"FAIL: {name}");
    Console.WriteLine($"PASS: {name}"); passed++;
}
Check(HeatMapBuilder.Build(Array.Empty<GeoSample>()).Count == 0, "empty heat map");
Check(!HeatMapBuilder.IsValid(double.NaN, 0) && !HeatMapBuilder.IsValid(91, 0), "reject invalid coordinates");
var samples = Enumerable.Repeat(new GeoSample(40.5001, -74.4001), 5)
    .Append(new GeoSample(40.5101, -74.4101)).ToArray();
var cells = HeatMapBuilder.Build(samples);
Check(cells.Count == 2 && cells[0].Count == 5 && cells[0].Intensity == 1 && cells[1].Intensity == .2, "density distinguishes repeated visits");
Check(HeatMapBuilder.Build(samples, 1).Count == 1, "heat cell rendering cap");
var path = Path.Combine(Path.GetTempPath(), $"locationtracker-tests-{Guid.NewGuid():N}.db3");
try
{
    await using (var repository = new LocationRepository(path))
    {
        await repository.AddAsync(new LocationRecord { Latitude = 40.5, Longitude = -74.4, TimestampUtcTicks = 100 });
        await repository.AddAsync(new LocationRecord { Latitude = 40.6, Longitude = -74.5, TimestampUtcTicks = 200 });
        Check((await repository.GetRecentAsync()).Count == 2, "SQLite inserts and reads");
        Check((await repository.GetRecentAsync(1))[0].TimestampUtcTicks == 200, "latest timestamp selection");
        try
        {
            await repository.AddAsync(new LocationRecord { Latitude = 91, Longitude = 0, TimestampUtcTicks = 300 });
            throw new InvalidOperationException("Invalid record accepted");
        }
        catch (ArgumentException) { Check(true, "invalid record refused"); }
    }
    await using (var reopened = new LocationRepository(path))
    {
        Check((await reopened.GetRecentAsync()).Count == 2, "SQLite survives reopen");
        await reopened.ClearAsync();
        Check((await reopened.GetRecentAsync()).Count == 0, "history deletion");
    }
    Console.WriteLine($"Completed: {passed} checks.");
}
finally { if (File.Exists(path)) File.Delete(path); }
