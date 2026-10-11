namespace LocationTracker.App;
public sealed class App : Application
{
    private readonly MainPage _page;
    public App(MainPage page) => _page = page;
    protected override Window CreateWindow(IActivationState? activationState)
    {
        var window = new Window(new NavigationPage(_page)
        { BarBackgroundColor = Color.FromArgb("#512BD4"), BarTextColor = Colors.White });
        // No background permission or background tracking is requested.
        window.Stopped += (_, _) => _page.StopTracking();
        window.Destroying += (_, _) => _page.StopTracking();
        return window;
    }
}
