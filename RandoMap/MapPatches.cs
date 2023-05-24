using HarmonyLib;
using Gameplay.UI.Others.MenuLogic;

namespace RandoMap
{
    [HarmonyPatch(typeof(NewMapMenuWidget), "OnShow")]
    public class MapMenuWidget_Patch
    {
        public static void Postfix(NewMapMenuWidget __instance)
        {
            Main.MapTracker.OpenMap(__instance);
        }
    }
}
