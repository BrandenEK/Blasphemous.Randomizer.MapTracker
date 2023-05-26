using HarmonyLib;
using Framework.Map;
using Gameplay.UI.Others.MenuLogic;
using System.Collections.Generic;

namespace RandoMap
{
    // Refresh map when opened
    [HarmonyPatch(typeof(NewMapMenuWidget), "OnShow")]
    public class MapMenuWidgetOpen_Patch
    {
        public static void Postfix()
        {
            Main.MapTracker.RefreshMap();
        }
    }

    // Change speed of map scrolling
    [HarmonyPatch(typeof(NewMapMenuWidget), "Initialize")]
    public class MapMenuWidgetInit_Patch
    {
        public static void Postfix(List<MapRenderer> ___MapRenderers)
        {
            foreach (MapRenderer renderer in ___MapRenderers)
            {
                renderer.Config.MovementSpeed = 400f;
            }
        }
    }
}
