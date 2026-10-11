namespace LocationTracker.Core;
public sealed record GeoSample(double Latitude, double Longitude);
public sealed record HeatCell(double Latitude, double Longitude, int Count, double Intensity);
