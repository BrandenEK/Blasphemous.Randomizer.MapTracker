using Blasphemous.Randomizer.ItemRando;
using Framework.Managers;
using System.Collections.Generic;

namespace Blasphemous.Randomizer.MapTracker.Extensions;

/// <summary>
/// Add functionality for Randomizer item locations
/// </summary>
public static class ItemLocationExtensions
{
    /// <summary>
    /// Whether this location is reachable
    /// </summary>
    public static bool IsReachable(this ItemLocation location, ItemLocation firstLocation, List<string> visibleRooms, BlasphemousInventory inventory)
    {
        return visibleRooms.Contains(location.GetSpecialRoom(firstLocation)) && inventory.Evaluate(location.GetSpecialLogic());
    }

    /// <summary>
    /// If this location is randomized
    /// </summary>
    public static bool ShouldBeTracked(this ItemLocation location, Config config)
    {
        //if (!config.ShuffleSwordSkills && location.Type == 1)
        //    return false;
        //if (!config.ShuffleThorns && location.Type == 2)
        //    return false;
        if (!config.ShuffleBootsOfPleading && location.Id == "RE401")
            return false;
        if (!config.ShufflePurifiedHand && location.Id == "RE402")
            return false;
        return true;
    }

    /// <summary>
    /// Some locations have a special flag used for tracking it
    /// </summary>
    public static string GetSpecialFlag(this ItemLocation location)
    {
        if (location.LocationFlag == null)
            return "LOCATION_" + location.Id;
        else
            return location.LocationFlag.Split('~')[0];
    }

    /// <summary>
    /// Some locations have special logic once you have access to their room
    /// </summary>
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
            default: return location.Logic;
        }
    }

    /// <summary>
    /// Some locations appear in multiple rooms
    /// </summary>
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
            default: return location.Room;
        }
    }

    /// <summary>
    /// Some locations should have a special name
    /// </summary>
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

    /// <summary>
    /// Some locations use a special flag to determine if it is collected
    /// </summary>
    public static bool IsCollected(this ItemLocation location)
    {
        switch (location.Id) // These special locations should never show as collected out from the multiworld
        {
            case "RB18":
            case "RB19":
            case "RB25":
            case "RB26":
            case "QI32":
            case "QI33":
            case "QI34":
            case "QI35":
            case "QI79":
            case "QI80":
            case "QI81":
            case "Amanecida[D02Z02S14]":
            case "Amanecida[D03Z01S03]":
            case "Amanecida[D04Z01S04]":
            case "Amanecida[D09Z01S01]":
                return Core.Events.GetFlag(location.GetSpecialFlag());
            default:
                return Core.Events.GetFlag(location.GetSpecialFlag()) || Core.Events.GetFlag("APLOCATION_" + location.Id);
        }
    }

    /// <summary>
    /// Checks if the location is hinted
    /// </summary>
    public static bool IsHinted(this ItemLocation location)
    {
        string[] idsToCheck; // If checking for any of these special locations, also check for the others in the group
        switch (location.Id)
        {
            case "RB18":
            case "RB19":
                idsToCheck = Data.redCandleIds; break;
            case "RB25":
            case "RB26":
                idsToCheck = Data.blueCandleIds; break;
            case "QI32":
            case "QI33":
            case "QI34":
            case "QI35":
            case "QI79":
            case "QI80":
            case "QI81":
                idsToCheck = Data.guiltArenaIds; break;
            case "Amanecida[D02Z02S14]":
            case "Amanecida[D03Z01S03]":
            case "Amanecida[D04Z01S04]":
            case "Amanecida[D09Z01S01]":
                idsToCheck = Data.amanecidaIds; break;
            default:
                return Core.Events.GetFlag("APHINT_" + location.Id);
        }

        bool thisLocationHinted = Core.Events.GetFlag("APHINT_" + location.Id);
        if (thisLocationHinted)
            return true;

        foreach (string id in idsToCheck)
        {
            if (Core.Events.GetFlag("APHINT_" + id) && !Core.Events.GetFlag("LOCATION_" + id))
                return true;
        }

        return false;
    }
}
