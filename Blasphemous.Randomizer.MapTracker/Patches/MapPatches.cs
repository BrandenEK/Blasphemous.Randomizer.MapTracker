using Blasphemous.Randomizer.ItemRando;
using Framework.Managers;
using Framework.Map;
using Gameplay.UI.Others.MenuLogic;
using HarmonyLib;
using System.Collections.Generic;

namespace Blasphemous.Randomizer.MapTracker.Patches;

/// <summary>
/// Change speed of map scrolling
/// </summary>
[HarmonyPatch(typeof(NewMapMenuWidget), nameof(NewMapMenuWidget.Initialize))]
class NewMapMenuWidget_Initialize_Patch
{
    public static void Postfix(List<MapRenderer> ___MapRenderers)
    {
        foreach (MapRenderer renderer in ___MapRenderers)
        {
            renderer.Config.MovementSpeed = 200f;
        }
    }
}

/// <summary>
/// Refresh map when pausing or toggling marks
/// </summary>
[HarmonyPatch(typeof(NewMapMenuWidget), nameof(NewMapMenuWidget.OnShow))]
class NewMapMenuWidget_OnShow_Patch
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

/// <summary>
/// Reveal the entire map when display locations is turned on
/// </summary>
[HarmonyPatch(typeof(NewMapManager), nameof(NewMapManager.GetAllRevealedCells), typeof(MapData))]
class NewMapManager_GetAllRevealedCells_Patch
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
[HarmonyPatch(typeof(NewMapManager), nameof(NewMapManager.GetAllRevealSecretsCells))]
class NewMapManager_GetAllRevealSecretsCells_Patch
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

/// <summary>
/// Prevent centering view when refreshing map
/// </summary>
[HarmonyPatch(typeof(NewMapMenuWidget), nameof(NewMapMenuWidget.CenterView))]
class NewMapMenuWidget_CenterView_Patch
{
    public static bool Prefix()
    {
        return Main.MapTracker.DisplayLocationMarks == Main.MapTracker.DisplayLocationsLastFrame;
    }
}

/// <summary>
/// Prevent placing marks
/// </summary>
[HarmonyPatch(typeof(NewMapMenuWidget), nameof(NewMapMenuWidget.MarkPressed))]
class NewMapMenuWidget_MarkPressed_Patch
{
    public static bool Prefix() => false;
}

/// <summary>
/// Change selected map cell when moving map
/// </summary>
[HarmonyPatch(typeof(NewMapMenuWidget), nameof(NewMapMenuWidget.UpdateCellData))]
class NewMapMenuWidget_UpdateCellData_Patch
{
    public static void Postfix(CellData ___CurrentCell)
    {
        Main.MapTracker.UpdateSelectedCell(___CurrentCell);
    }
}

/// <summary>
/// Process tab input
/// </summary>
[HarmonyPatch(typeof(NewMapMenuWidget), nameof(NewMapMenuWidget.UITabLeft))]
class NewMapMenuWidget_UITabLeft_Patch
{
    public static void Postfix()
    {
        Main.MapTracker.TabLocationIndex(-1);
    }
}
[HarmonyPatch(typeof(NewMapMenuWidget), nameof(NewMapMenuWidget.UITabRight))]
class NewMapMenuWidget_UITabRight_Patch
{
    public static void Postfix()
    {
        Main.MapTracker.TabLocationIndex(1);
    }
}

/// <summary>
/// Recalculate inventory when item is added
/// </summary>
[HarmonyPatch(typeof(Item), nameof(Item.addToInventory))]
class Item_addToInventory_Patch
{
    public static void Postfix()
    {
        Main.MapTracker.ResetInventory();
    }
}
