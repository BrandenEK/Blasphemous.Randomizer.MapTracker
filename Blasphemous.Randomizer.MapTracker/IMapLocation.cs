using Blasphemous.Randomizer.ItemRando;
using System.Collections.Generic;
using UnityEngine.UI;

namespace Blasphemous.Randomizer.MapTracker;

internal interface IMapLocation
{
    /// <summary>
    /// Gets the status of all locations in this cell
    /// </summary>
    public CollectionStatus GetCurrentStatusTotal(Config config, BlasphemousInventory inventory, List<string> visibleRooms);

    /// <summary>
    /// Gets the status of a single location in this cell
    /// </summary>
    public CollectionStatus GetCurrentStatusIndividual(int idx, BlasphemousInventory inventory, List<string> visibleRooms);

    /// <summary>
    /// Gets the name of a single location in this cell
    /// </summary>
    public string GetNameIndividual(int idx);

    /// <summary>
    /// Gets the next location that will be selected when tabbing
    /// </summary>
    public int GetNextSelectableIndex(int idx, int direction, Config config);

    /// <summary>
    /// The image component for this cell
    /// </summary>
    public Image Image { get; set; }
}
