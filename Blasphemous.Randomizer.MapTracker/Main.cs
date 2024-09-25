using BepInEx;
using Blasphemous.ModdingAPI;

namespace Blasphemous.Randomizer.MapTracker;

[BepInPlugin(ModInfo.MOD_ID, ModInfo.MOD_NAME, ModInfo.MOD_VERSION)]
[BepInDependency("Blasphemous.ModdingAPI", "2.4.1")]
[BepInDependency("Blasphemous.Randomizer", "3.0.3")]
internal class Main : BaseUnityPlugin
{
    public static MapTracker MapTracker { get; private set; }
    public static Randomizer Randomizer { get; private set; }

    private void Start()
    {
        MapTracker = new MapTracker();
        Randomizer = MapTracker.IsModLoadedName("Randomizer", out BlasMod mod) ? mod as Randomizer
            : throw new System.Exception("Randomizer not loaded");
    }
}
