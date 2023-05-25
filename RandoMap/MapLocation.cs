using BlasphemousRandomizer.ItemRando;
using Framework.Managers;
using LogicParser;
using System.Collections.Generic;
using UnityEngine.UI;

namespace RandoMap
{
    public class MapLocation
    {
        public Image Image { get; set; }

        string singleLocation = null;
        string[] multipleLocations = null;

        public MapLocation(string locationId)
        {
            singleLocation = locationId;
        }

        public MapLocation(params string[] locationIds)
        {
            multipleLocations = locationIds;
        }

        public CollectionStatus GetCurrentStatus(BlasphemousInventory inventory, List<string> visibleRooms)
        {
            if (singleLocation != null) // Only one location for this cell
            {
                ItemLocation itemLocation = Main.Randomizer.data.itemLocations[singleLocation];

                // Check if the location has already been collected
                string flag = itemLocation.LocationFlag == null ? "LOCATION_" + itemLocation.Id : itemLocation.LocationFlag.Split('~')[0];
                if (Core.Events.GetFlag(flag))
                    return CollectionStatus.AllCollected;

                // Check if the location is in logic
                bool isReachable = visibleRooms.Contains(itemLocation.Room) && (itemLocation.Logic == null || Parser.EvaluateExpression(itemLocation.Logic, inventory));
                return (isReachable) ? CollectionStatus.AllReachable : CollectionStatus.NoneReachable;
            }
            else if (multipleLocations != null) // Multiple locations for this cell
            {
                bool oneIsReachable = false, oneIsNotReachable = false;

                foreach (string locationId in multipleLocations)
                {
                    ItemLocation itemLocation = Main.Randomizer.data.itemLocations[locationId];

                    // Check if the location has already been collected
                    string flag = itemLocation.LocationFlag == null ? "LOCATION_" + itemLocation.Id : itemLocation.LocationFlag.Split('~')[0];
                    if (Core.Events.GetFlag(flag))
                        continue;

                    // Check if the location is in logic
                    bool isReachable = visibleRooms.Contains(itemLocation.Room) && (itemLocation.Logic == null || Parser.EvaluateExpression(itemLocation.Logic, inventory));
                    if (isReachable)
                        oneIsReachable = true;
                    else
                        oneIsNotReachable = true;
                }

                // Based on all locations in the cell
                if (!oneIsReachable && !oneIsNotReachable) return CollectionStatus.AllCollected;
                else if (oneIsReachable && !oneIsNotReachable) return CollectionStatus.AllReachable;
                else if (!oneIsReachable && oneIsNotReachable) return CollectionStatus.NoneReachable;
                else return CollectionStatus.SomeReachable;
            }

            return CollectionStatus.NoneReachable;
        }

        public enum CollectionStatus
        {
            NoneReachable,
            SomeReachable,
            AllReachable,
            AllCollected,
        }
    }
}
