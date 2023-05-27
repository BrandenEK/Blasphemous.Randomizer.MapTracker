using BlasphemousRandomizer;
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

        readonly string singleLocation = null;
        readonly string[] multipleLocations = null;

        public MapLocation(string locationId)
        {
            singleLocation = locationId;
        }

        public MapLocation(params string[] locationIds)
        {
            multipleLocations = locationIds;
        }

        public CollectionStatus GetCurrentStatus(Config config, BlasphemousInventory inventory, List<string> visibleRooms)
        {
            if (singleLocation != null) // Only one location for this cell
            {
                ItemLocation itemLocation = Main.Randomizer.data.itemLocations[singleLocation];

                // Check if the location has already been collected
                if (!itemLocation.ShouldBeTracked(config) || Core.Events.GetFlag(itemLocation.GetSpecialFlag()))
                    return CollectionStatus.AllCollected;

                // Check if the location is in logic
                return itemLocation.IsReachable(itemLocation, visibleRooms, inventory) ? CollectionStatus.AllReachable : CollectionStatus.NoneReachable;
            }
            else if (multipleLocations != null) // Multiple locations for this cell
            {
                bool atLeastOneReachable = false, atLeastOneNotReachable = false;
                ItemLocation firstLocation = Main.Randomizer.data.itemLocations[multipleLocations[0]];

                foreach (string locationId in multipleLocations)
                {
                    ItemLocation itemLocation = Main.Randomizer.data.itemLocations[locationId];

                    // Check if the location has already been collected
                    if (!itemLocation.ShouldBeTracked(config) || Core.Events.GetFlag(itemLocation.GetSpecialFlag()))
                        continue;

                    // Check if the location is in logic
                    if (itemLocation.IsReachable(firstLocation, visibleRooms, inventory))
                        atLeastOneReachable = true;
                    else
                        atLeastOneNotReachable = true;
                }

                // Based on all locations in the cell
                if (!atLeastOneReachable && !atLeastOneNotReachable) return CollectionStatus.AllCollected;
                else if (atLeastOneReachable && !atLeastOneNotReachable) return CollectionStatus.AllReachable;
                else if (!atLeastOneReachable && atLeastOneNotReachable) return CollectionStatus.NoneReachable;
                else return CollectionStatus.SomeReachable;
            }

            throw new System.Exception("Map cell locations not set up correctly!");
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
        public static bool IsReachable(this ItemLocation location, ItemLocation firstLocation, List<string> visibleRooms, BlasphemousInventory inventory)
        {
            return visibleRooms.Contains(location.GetSpecialRoom(firstLocation)) && Parser.EvaluateExpression(location.GetSpecialLogic(), inventory);
        }

        public static bool ShouldBeTracked(this ItemLocation location, Config config)
        {
            if (!config.ShuffleSwordSkills && location.Type == 1)
                return false;
            if (!config.ShuffleThorns && location.Type == 2)
                return false;
            if (!config.ShuffleBootsOfPleading && location.Id == "RE401")
                return false;
            if (!config.ShufflePurifiedHand && location.Id == "RE402")
                return false;
            return true;
        }

        public static string GetSpecialFlag(this ItemLocation location)
        {
            if (location.LocationFlag == null)
                return "LOCATION_" + location.Id;
            else
                return location.LocationFlag.Split('~')[0];
        }

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

        public static string GetSpecialRoom(this ItemLocation location, ItemLocation firstLocation)
        {
            if (location.Type == 1) // Sword skills need to change their location to their shrine room
            {
                return firstLocation.Room;
            }

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
