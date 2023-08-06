using BlasphemousRandomizer;
using BlasphemousRandomizer.ItemRando;
using Framework.Managers;
using LogicParser;
using System.Collections.Generic;

namespace RandoMap
{
    public static class ItemLocationExtensions
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

        public static string GetSpecialName(this ItemLocation location)
        {
            switch (location.Id)
            {
                case "RB18": return "Red candle";
                case "RB19": return "Red candle";
                case "RB25": return "Blue candle";
                case "RB26": return "Blue candle";
                case "QI32": return "Guilt arena";
                case "QI33": return "Guilt arena";
                case "QI34": return "Guilt arena";
                case "QI35": return "Guilt arena";
                case "QI79": return "Guilt arena";
                case "QI80": return "Guilt arena";
                case "QI81": return "Guilt arena";
                default: return location.Name;
            }
        }

        public static bool IsCollected(this ItemLocation location)
        {
            return Core.Events.GetFlag(location.GetSpecialFlag()) || Core.Events.GetFlag("APLOCATION_" + location.Id);
        }

        public static bool IsHinted(this ItemLocation location)
        {
            // Needs to account for special locations
            return Core.Events.GetFlag("APHINT_" + location.Id);
        }
    }
}
