using LocationTracker.Core;
using LocationTracker.Data;
using Microsoft.Maui.Maps;
using MapControl = Microsoft.Maui.Controls.Maps.Map;
namespace LocationTracker.App;
public sealed class MainPage : ContentPage
{
    private readonly LocationRepository _repository;
    private readonly MapControl _map = new() { IsShowingUser = false };
    private readonly Label _status = new() { Text = "Ready. Start to allow foreground tracking.", FontSize = 13 };
    private readonly Label _count = new() { Text = "Saved samples displayed: 0", FontSize = 13 };
    private readonly Button _start = new() { Text = "Start" };
    private readonly Button _stop = new() { Text = "Stop", IsEnabled = false };
    private readonly Button _refresh = new() { Text = "Refresh" };
    private readonly Button _clear = new() { Text = "Clear" };
    private CancellationTokenSource? _tracking;
    private bool _busy;

    public MainPage(LocationRepository repository)
    {
        _repository = repository;
        Title = "MainView";
        var controls = new Grid { ColumnDefinitions = new ColumnDefinitionCollection
        { new(GridLength.Star), new(GridLength.Star), new(GridLength.Star), new(GridLength.Star) }, ColumnSpacing = 4 };
        controls.Add(_start, 0); controls.Add(_stop, 1); controls.Add(_refresh, 2); controls.Add(_clear, 3);
        var panel = new VerticalStackLayout { Padding = 10, Spacing = 6,
            Children = { controls, _status, _count,
                new Label { Text = "Heat: blue low | green | orange | red high", FontSize = 12 },
                new Label { Text = "Tracks only while open. Locations stay in this app. Clear deletes history.", FontSize = 12 } } };
        var layout = new Grid { RowDefinitions = new RowDefinitionCollection
            { new(GridLength.Auto), new(GridLength.Star) } };
        layout.Add(panel, 0, 0); layout.Add(_map, 0, 1); Content = layout;
        _start.Clicked += StartClicked;
        _stop.Clicked += (_, _) => StopTracking();
        _refresh.Clicked += async (_, _) => await PerformAsync(async () => await RefreshMapAsync(true));
        _clear.Clicked += async (_, _) => await PerformAsync(async () =>
        {
            if (!await DisplayAlertAsync("Delete saved locations", "Delete all location history?", "Delete", "Cancel")) return;
            await _repository.ClearAsync();
            await RefreshMapAsync(false); _status.Text = "Location history deleted.";
        });
    }
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await PerformAsync(async () => await RefreshMapAsync(true));
    }
    protected override void OnDisappearing() { StopTracking(); base.OnDisappearing(); }
    public void StopTracking() => _tracking?.Cancel();
    private void SetControls()
    {
        _start.IsEnabled = _tracking is null && !_busy;
        _stop.IsEnabled = _tracking is not null;
        _clear.IsEnabled = _refresh.IsEnabled = _tracking is null && !_busy;
    }
    private async Task PerformAsync(Func<Task> action)
    {
        if (_busy || _tracking is not null) return;
        _busy = true; SetControls();
        try { await action(); }
        catch (Exception) { _status.Text = "Storage or map update failed. Retry after checking device storage."; }
        finally { _busy = false; SetControls(); }
    }
    private async void StartClicked(object? sender, EventArgs e)
    {
        if (_tracking is not null || _busy) return;
        using var cancellation = new CancellationTokenSource();
        _tracking = cancellation; SetControls();
        try
        {
            var permission = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();
            if (permission != PermissionStatus.Granted)
                permission = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
            cancellation.Token.ThrowIfCancellationRequested();
            if (permission != PermissionStatus.Granted)
            { _status.Text = "Location permission denied. Allow location in Settings, then press Start."; return; }
            _map.IsShowingUser = true;
            await TrackAsync(cancellation.Token);
        }
        catch (OperationCanceledException) { _status.Text = "Tracking stopped. Saved locations remain available."; }
        catch (Exception) { _status.Text = "Cannot start tracking. Check permission and location services."; }
        finally { _map.IsShowingUser = false; _tracking = null; SetControls(); }
    }
    private async Task TrackAsync(CancellationToken cancellation)
    {
        while (true)
        {
            cancellation.ThrowIfCancellationRequested();
            try
            {
                var request = new GeolocationRequest(GeolocationAccuracy.Best, TimeSpan.FromSeconds(15));
                var location = await Geolocation.Default.GetLocationAsync(request, cancellation);
                cancellation.ThrowIfCancellationRequested();
                if (location is null) _status.Text = "No location fix. Retrying.";
                else if (location.IsFromMockProvider) _status.Text = "Mock location ignored. Use a real device for recording.";
                else if (!HeatMapBuilder.IsValid(location.Latitude, location.Longitude) ||
                    (location.Accuracy.HasValue && (!double.IsFinite(location.Accuracy.Value) || location.Accuracy.Value > 100)))
                    _status.Text = "Location fix is invalid or less accurate than 100 metres. Retrying.";
                else
                {
                    await _repository.AddAsync(new LocationRecord
                    {
                        Latitude = location.Latitude, Longitude = location.Longitude,
                        TimestampUtcTicks = location.Timestamp.UtcDateTime.Ticks,
                        AccuracyMeters = location.Accuracy
                    });
                    await RefreshMapAsync(false);
                    _status.Text = $"Saved {location.Latitude:F5}, {location.Longitude:F5} at {DateTime.Now:t}";
                }
            }
            catch (OperationCanceledException) when (cancellation.IsCancellationRequested) { throw; }
            catch (FeatureNotEnabledException) { _status.Text = "Turn on device location services. Retrying."; }
            catch (PermissionException) { _status.Text = "Location permission was revoked. Tracking ended."; return; }
            catch (FeatureNotSupportedException) { _status.Text = "This device does not provide geolocation."; return; }
            catch (Exception) { _status.Text = "Location or storage unavailable. Retrying."; }
            // Serial loop prevents overlapping requests. Actual interval includes acquisition time.
            await Task.Delay(TimeSpan.FromSeconds(10), cancellation);
        }
    }
    private async Task RefreshMapAsync(bool center)
    {
        var rows = await _repository.GetRecentAsync();
        HeatMapRenderer.Draw(_map, rows.Select(r => new GeoSample(r.Latitude, r.Longitude)));
        _count.Text = $"Saved samples displayed: {rows.Count} (latest 5000; densest 300 cells)";
        if (center && rows.Count > 0)
        {
            var last = rows[^1];
            _map.MoveToRegion(MapSpan.FromCenterAndRadius(new Location(last.Latitude, last.Longitude), Distance.FromKilometers(1)));
        }
        else if (_tracking is not null && rows.Count == 1)
            _map.MoveToRegion(MapSpan.FromCenterAndRadius(new Location(rows[0].Latitude, rows[0].Longitude), Distance.FromKilometers(1)));
    }
}
