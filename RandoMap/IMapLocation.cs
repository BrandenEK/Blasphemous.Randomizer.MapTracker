using BlasphemousRandomizer;
using BlasphemousRandomizer.ItemRando;
using System.Collections.Generic;
using UnityEngine.UI;

namespace RandoMap
{
    public interface IMapLocation
    {
        public CollectionStatus GetCurrentStatusTotal(Config config, BlasphemousInventory inventory, List<string> visibleRooms);

        public CollectionStatus GetCurrentStatusIndividual(int idx, BlasphemousInventory inventory, List<string> visibleRooms);

        public string GetNameIndividual(int idx);

        public int GetNextSelectableIndex(int idx, int direction, Config config);

        public Image Image { get; set; }
    }
}
