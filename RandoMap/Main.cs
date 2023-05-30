using BepInEx;
using BlasphemousRandomizer;
using System.Collections.Generic;

namespace RandoMap
{
    [BepInPlugin(MOD_ID, MOD_NAME, MOD_VERSION)]
    [BepInDependency("com.damocles.blasphemous.modding-api", "1.4.0")]
    [BepInDependency("com.damocles.blasphemous.randomizer", "2.0.0")]
    [BepInProcess("Blasphemous.exe")]
    public class Main : BaseUnityPlugin
    {
        public const string MOD_ID = "com.damocles.blasphemous.rando-map";
        public const string MOD_NAME = "Rando Map";
        public const string MOD_VERSION = "1.1.0";

        public static MapTracker MapTracker { get; private set; }
        public static Randomizer Randomizer { get; private set; }

        private void Start()
        {
            MapTracker = new MapTracker(MOD_ID, MOD_NAME, MOD_VERSION);
            Randomizer = BlasphemousRandomizer.Main.Randomizer;
        }
    }

    public static class ListExtensions
    {
        public static bool IsReverseOf<T>(this List<T> mainList, List<T> otherList)
        {
            if (mainList.Count != otherList.Count)
                return false;

            for (int i = 0; i < mainList.Count; i++)
            {
                if (!mainList[i].Equals(otherList[mainList.Count - 1 - i]))
                    return false;
            }

            return true;
        }
    }
}
