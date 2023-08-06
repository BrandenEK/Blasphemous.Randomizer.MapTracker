using BlasphemousRandomizer;
using BlasphemousRandomizer.ItemRando;
using System.Collections.Generic;
using UnityEngine.UI;

namespace RandoMap
{
    public class SingleLocation : IMapLocation
    {
        private readonly string location;

        public SingleLocation(string locationId)
        {
            location = locationId;
        }

        public CollectionStatus GetCurrentStatusIndividual(int idx, BlasphemousInventory inventory, List<string> visibleRooms)
        {
            ItemLocation itemLocation = Main.Randomizer.data.itemLocations[location];

            if (itemLocation.IsCollected())
                return CollectionStatus.Finished;

            bool isReachable = itemLocation.IsReachable(itemLocation, visibleRooms, inventory);
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
            ItemLocation itemLocation = Main.Randomizer.data.itemLocations[location];

            if (!itemLocation.ShouldBeTracked(config))
                return CollectionStatus.Untracked;

            if (itemLocation.IsCollected())
                return CollectionStatus.Finished;

            bool isReachable = itemLocation.IsReachable(itemLocation, visibleRooms, inventory);
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

        public string GetNameIndividual(int idx)
        {
            return Main.Randomizer.data.itemLocations[location].GetSpecialName();
        }

        public int GetNextSelectableIndex(int idx, int direction, Config config)
        {
            return 0;
        }

        private Image _image;
        public Image Image
        {
            get => _image;
            set => _image = value;
        }
    }
}
