using Blasphemous.Randomizer.ItemRando;
using System.Collections.Generic;
using UnityEngine.UI;

namespace Blasphemous.Randomizer.MapTracker;

internal class MultipleLocation : IMapLocation
{
    private readonly string[] locations;

    public MultipleLocation(params string[] locationIds)
    {
        locations = locationIds;
    }

    public CollectionStatus GetCurrentStatusIndividual(int idx, BlasphemousInventory inventory, List<string> visibleRooms)
    {
        if (idx < 0 && idx >= locations.Length)
        {
            Main.MapTracker.LogError("Location idx out of bounds for " + locations[0]);
            return CollectionStatus.AllUnreachable;
        }

        ItemLocation itemLocation = Main.Randomizer.data.itemLocations[locations[idx]];
        ItemLocation firstLocation = Main.Randomizer.data.itemLocations[locations[0]];

        if (itemLocation.IsCollected())
            return CollectionStatus.Finished;

        bool isReachable = itemLocation.IsReachable(firstLocation, visibleRooms, inventory);
        bool isHinted = itemLocation.IsHinted();

        if (isHinted)
        {
            return isReachable ? CollectionStatus.HintedReachable : CollectionStatus.HintedAllUnreachable;
        }
        else
        {
            return isReachable ? CollectionStatus.AllReachable : CollectionStatus.AllUnreachable;
        }
    }

    public CollectionStatus GetCurrentStatusTotal(Config config, BlasphemousInventory inventory, List<string> visibleRooms)
    {
        ItemLocation firstLocation = Main.Randomizer.data.itemLocations[locations[0]];
        int numUntracked = 0, numCollected = 0, numReachable = 0, numUnreachable = 0, numHinted = 0;

        foreach (string location in locations)
        {
            ItemLocation itemLocation = Main.Randomizer.data.itemLocations[location];

            if (!itemLocation.ShouldBeTracked(config))
            {
                numUntracked++;
                continue;
            }

            if (itemLocation.IsCollected())
            {
                numCollected++;
                continue;
            }

            bool isReachable = itemLocation.IsReachable(firstLocation, visibleRooms, inventory);
            bool isHinted = itemLocation.IsHinted();

            if (isHinted)
            {
                if (isReachable)
                    return CollectionStatus.HintedReachable;

                numUnreachable++;
                numHinted++;
            }
            else
            {
                if (isReachable)
                    numReachable++;
                else
                    numUnreachable++;
            }
        }

        int totalLocations = locations.Length;

        if (numUntracked == totalLocations)
            return CollectionStatus.Untracked;
        if (numHinted > 0)
        {
            if (numReachable > 0)
                return CollectionStatus.HintedSomeReachable;
            else
                return CollectionStatus.HintedAllUnreachable;
        }

        totalLocations -= numUntracked;

        if (numCollected == totalLocations)
            return CollectionStatus.Finished;

        totalLocations -= numCollected;

        if (numReachable == totalLocations)
            return CollectionStatus.AllReachable;
        if (numUnreachable == totalLocations)
            return CollectionStatus.AllUnreachable;
        return CollectionStatus.SomeReachable;
    }

    public string GetNameIndividual(int idx)
    {
        if (idx < 0 && idx >= locations.Length)
        {
            Main.MapTracker.LogError("Location idx out of bounds for " + locations[0]);
            return "???";
        }

        return Main.Randomizer.data.itemLocations[locations[idx]].GetSpecialName();
    }

    // (-1, 1) is used to select the first available location when switching to a new cell, but
    // be careful with this because it could cause an infinite loop if there isnt a valid one after that
    // I just didn't feel like fixing that
    public int GetNextSelectableIndex(int idx, int direction, Config config)
    {
        int startIdx = idx, currIdx = idx;
        do
        {
            if (currIdx == 0 && direction < 0)
                currIdx = locations.Length - 1;
            else if (currIdx == locations.Length - 1 && direction > 0)
                currIdx = 0;
            else
                currIdx += direction;

            if (Main.Randomizer.data.itemLocations[locations[currIdx]].ShouldBeTracked(config))
                return currIdx;
        }
        while (currIdx != startIdx);

        // Failed to find a different selectable one, so return the same
        return idx;
    }

    private Image _image;
    public Image Image
    {
        get => _image;
        set => _image = value;
    }
}
