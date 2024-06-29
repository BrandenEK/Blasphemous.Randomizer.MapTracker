
namespace Blasphemous.Randomizer.MapTracker;

/// <summary>
/// Stores information relating to a map location
/// </summary>
public class LocationInfo(int x, int y, string[] locations)
{
    /// <summary>
    /// X coordinate of this location
    /// </summary>
    public int X { get; } = x;

    /// <summary>
    /// Y coordinate of this location
    /// </summary>
    public int Y { get; } = y;

    /// <summary>
    /// List of location ids present at this location
    /// </summary>
    public string[] Locations { get; } = locations;
}
