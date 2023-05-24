using BepInEx;
using BlasphemousRandomizer;

namespace RandoMap
{
    [BepInPlugin(MOD_ID, MOD_NAME, MOD_VERSION)]
    [BepInDependency("com.damocles.blasphemous.modding-api", "1.3.4")]
    [BepInDependency("com.damocles.blasphemous.randomizer", "2.0.0")]
    [BepInProcess("Blasphemous.exe")]
    public class Main : BaseUnityPlugin
    {
        public const string MOD_ID = "com.damocles.blasphemous.rando-map";
        public const string MOD_NAME = "Rando Map";
        public const string MOD_VERSION = "0.1.0";

        public static MapTracker MapTracker { get; private set; }
        public static Randomizer Randomizer { get; private set; }

        private void Start()
        {
            MapTracker = new MapTracker(MOD_ID, MOD_NAME, MOD_VERSION);
            Randomizer = BlasphemousRandomizer.Main.Randomizer;
        }
    }
}
