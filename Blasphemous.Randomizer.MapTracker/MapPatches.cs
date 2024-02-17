using Blasphemous.Randomizer.ItemRando;
using Framework.Managers;
using Framework.Map;
using Gameplay.UI.Others.MenuLogic;
using HarmonyLib;
using System.Collections.Generic;

namespace Blasphemous.Randomizer.MapTracker;

// Change speed of map scrolling
[HarmonyPatch(typeof(NewMapMenuWidget), "Initialize")]
class MapMenuWidgetSpeed_Patch
{
    public static void Postfix(List<MapRenderer> ___MapRenderers)
    {
        foreach (MapRenderer renderer in ___MapRenderers)
        {
            renderer.Config.MovementSpeed = 200f;
        }
    }
}

// Refresh map when pausing or toggling marks
[HarmonyPatch(typeof(NewMapMenuWidget), "OnShow")]
class MapMenuWidgetShow_Patch
{
    public static void Postfix()
    {
        Main.MapTracker.RefreshMap();
    }

    public static System.Exception Finalizer()
    {
        return null;
    }
}

// Reveal the entire map when display locations is turned on
[HarmonyPatch(typeof(NewMapManager), "GetAllRevealedCells", typeof(MapData))]
class MapGetRevealed_Patch
{
    public static bool Prefix(MapData map, ref List<CellData> __result)
    {
        if (!Main.MapTracker.DisplayLocationMarks || !Main.MapTracker.IsShowingMap)
            return true;

        __result = new List<CellData>();
        foreach (CellData cell in map.Cells)
        {
            bool hasSecret = false;
            foreach (SecretData secret in map.Secrets.Values)
            {
                if (secret.Cells.ContainsKey(cell.CellKey))
                {
                    hasSecret = true;
                    __result.Add(secret.Cells[cell.CellKey]);
                    break;
                }
            }
            if (!hasSecret)
            {
                __result.Add(cell);
            }
        }

        return false;
    }
}
[HarmonyPatch(typeof(NewMapManager), "GetAllRevealSecretsCells")]
class MapGetSecrets_Patch
{
    public static bool Prefix(MapData ___CurrentMap, ref List<CellKey> __result)
    {
        if (!Main.MapTracker.DisplayLocationMarks || !Main.MapTracker.IsShowingMap)
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
class MapMenuWidgetCenter_Patch
{
    public static bool Prefix()
    {
        return Main.MapTracker.DisplayLocationMarks == Main.MapTracker.DisplayLocationsLastFrame;
    }
}

// Prevent placing marks
[HarmonyPatch(typeof(NewMapMenuWidget), "MarkPressed")]
class MapMenuWidgetMark_Patch
{
    public static bool Prefix() => false;
}

// Change selected map cell when moving map
[HarmonyPatch(typeof(NewMapMenuWidget), "UpdateCellData")]
class MapMenuWidgetUpdateCell_Patch
{
    public static void Postfix(CellData ___CurrentCell)
    {
        Main.MapTracker.UpdateSelectedCell(___CurrentCell);
    }
}

// Process tab input
[HarmonyPatch(typeof(NewMapMenuWidget), "UITabLeft")]
class MapMenuWidgetTabLeft_Patch
{
    public static void Postfix()
    {
        Main.MapTracker.TabLocationIndex(-1);
    }
}
[HarmonyPatch(typeof(NewMapMenuWidget), "UITabRight")]
class MapMenuWidgetTabRight_Patch
{
    public static void Postfix()
    {
        Main.MapTracker.TabLocationIndex(1);
    }
}

// Recalculate inventory when item is added
[HarmonyPatch(typeof(Item), "addToInventory")]
class ItemAdd_Patch
{
    public static void Postfix()
    {
        Main.MapTracker.ResetInventory();
    }
}
