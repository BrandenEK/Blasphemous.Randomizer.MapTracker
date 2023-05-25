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
                bool isReachable = visibleRooms.Contains(itemLocation.GetSpecialRoom()) && (itemLocation.Logic == null || Parser.EvaluateExpression(itemLocation.GetSpecialLogic(), inventory));
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
                    bool isReachable = visibleRooms.Contains(itemLocation.GetSpecialRoom()) && (itemLocation.Logic == null || Parser.EvaluateExpression(itemLocation.GetSpecialLogic(), inventory));
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

    public static class LogicExtensions
    {
        public static string GetSpecialLogic(this ItemLocation location)
        {
            switch (location.Id)
            {
                case "RB18": return "redWax > 0";
                case "RB19": return "redWax > 0 && D05Z01S02[W]";
                case "RB25": return "blueWax > 0 && (D17Z01S04[N] || D17Z01S04[FrontR])";
                case "RB26": return "blueWax > 0";
                case "QI32": return "guiltBead";
                case "QI33": return "guiltBead";
                case "QI34": return "guiltBead";
                case "QI35": return "guiltBead";
                case "QI79": return "guiltBead";
                case "QI80": return "guiltBead";
                case "QI81": return "guiltBead";
                default:     return location.Logic;
            }
        }

        public static string GetSpecialRoom(this ItemLocation location)
        {
            switch (location.Id)
            {
                case "RB18": return "D02Z03S06";
                case "RB19": return "D05Z01S02";
                case "RB25": return "D17Z01S04";
                case "RB26": return "D01Z04S16";
                case "QI32": return "D01Z04S17";
                case "QI33": return "D02Z02S06";
                case "QI34": return "D03Z03S14";
                case "QI35": return "D17Z01S12";
                case "QI79": return "D04Z02S17";
                case "QI80": return "D05Z01S17";
                case "QI81": return "D09Z01S13";
                default:     return location.Room;
            }
        }
    }
}
