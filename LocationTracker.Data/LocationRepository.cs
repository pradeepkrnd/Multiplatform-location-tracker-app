using LocationTracker.Core;
using SQLite;
namespace LocationTracker.Data;
/// <summary>Serializes initialization and every database operation.</summary>
public sealed class LocationRepository : IAsyncDisposable
{
    private readonly SQLiteAsyncConnection _database;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private bool _initialized;
    public LocationRepository(string databasePath)
    {
        _database = new SQLiteAsyncConnection(databasePath,
            SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.Create | SQLiteOpenFlags.FullMutex);
    }
    private async Task InitializeAsync()
    {
        if (_initialized) return;
        await _database.CreateTableAsync<LocationRecord>();
        _initialized = true;
    }
    public async Task AddAsync(LocationRecord record)
    {
        if (!HeatMapBuilder.IsValid(record.Latitude, record.Longitude) ||
            record.TimestampUtcTicks <= 0 ||
            (record.AccuracyMeters.HasValue &&
             (!double.IsFinite(record.AccuracyMeters.Value) || record.AccuracyMeters.Value < 0)))
            throw new ArgumentException("Invalid location record.", nameof(record));
        await _gate.WaitAsync();
        try { await InitializeAsync(); await _database.InsertAsync(record); }
        finally { _gate.Release(); }
    }
    public async Task<List<LocationRecord>> GetRecentAsync(int limit = 5000)
    {
        if (limit < 1 || limit > 5000) throw new ArgumentOutOfRangeException(nameof(limit));
        await _gate.WaitAsync();
        try
        {
            await InitializeAsync();
            var rows = await _database.Table<LocationRecord>()
                .OrderByDescending(r => r.TimestampUtcTicks).ThenByDescending(r => r.Id)
                .Take(limit).ToListAsync();
            rows.Reverse();
            return rows;
        }
        finally { _gate.Release(); }
    }
    public async Task ClearAsync()
    {
        await _gate.WaitAsync();
        try { await InitializeAsync(); await _database.DeleteAllAsync<LocationRecord>(); }
        finally { _gate.Release(); }
    }
    public async ValueTask DisposeAsync()
    {
        await _gate.WaitAsync();
        try { await _database.CloseAsync(); }
        finally { _gate.Release(); }
    }
}
