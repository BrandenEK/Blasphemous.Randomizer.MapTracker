using HarmonyLib;
using Framework.Managers;
using Framework.Map;
using Gameplay.UI.Others.MenuLogic;
using System.Collections.Generic;

namespace RandoMap
{
    // Refresh map when pausing or toggling marks
    [HarmonyPatch(typeof(NewMapMenuWidget), "Initialize")]
    public class MapMenuWidgetInit_Patch
    {
        public static void Postfix(List<MapRenderer> ___MapRenderers)
        {
            foreach (MapRenderer renderer in ___MapRenderers)
            {
                renderer.Config.MovementSpeed = 400f;
            }

            Main.MapTracker.RefreshMap();
        }
    }

    // Reveal the entire map when display locations is turned on
    [HarmonyPatch(typeof(NewMapManager), "GetAllRevealedCells", typeof(MapData))]
    public class MapGetRevealed_Patch
    {
        public static bool Prefix(MapData map, ref List<CellData> __result)
        {
            if (!Main.MapTracker.DisplayLocationMarks)
                return true;

            __result = map.Cells;
            return false;
        }
    }
    [HarmonyPatch(typeof(NewMapManager), "GetAllRevealSecretsCells")]
    public class MapGetSecrets_Patch
    {
        public static bool Prefix(MapData ___CurrentMap, ref List<CellKey> __result)
        {
            if (!Main.MapTracker.DisplayLocationMarks)
                return true;

            __result = new List<CellKey>();
            foreach (SecretData secret in ___CurrentMap.Secrets.Values)
            {
                foreach (CellKey cell in secret.Cells.Keys)
                {
                    __result.Add(cell);
                }
            }

            return false;
        }
    }

    // Prevent centering view when refreshing map
    [HarmonyPatch(typeof(NewMapMenuWidget), "CenterView")]
    public class MapMenuWidgetCenter_Patch
    {
        public static bool Prefix()
        {
            return Main.MapTracker.DisplayLocationMarks == Main.MapTracker.DisplayLocationsLastFrame;
        }
    }
}
