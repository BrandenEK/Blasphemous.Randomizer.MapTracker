using HarmonyLib;
using Framework.Managers;
using Gameplay.UI.Others.MenuLogic;
using BlasphemousRandomizer.ItemRando;

namespace RandoMap
{
    [HarmonyPatch(typeof(NewMapMenuWidget), "OnShow")]
    public class MapMenuWidgetOpen_Patch
    {
        public static void Postfix()
        {
            Main.MapTracker.RefreshMap();
        }
    }

    //[HarmonyPatch(typeof(NewMapMenuWidget), "UpdateCurrentRenderer")]
    //public class MapMenuWidgetUpdate_Patch
    //{
    //    public static void Postfix(bool ___mapEnabled, int ___CurrentRendererIndex)
    //    {
    //        bool mapVisible = ___mapEnabled && ___CurrentRendererIndex == 0;

    //        Main.MapTracker.ShowingMap = mapVisible;
    //        if (mapVisible)
    //            Main.MapTracker.RefreshMap();
    //    }
    //}

    [HarmonyPatch(typeof(BlasphemousInventory), "AddItem")]
    public class temp
    {
        public static void Postfix(string itemId)
        {
            //Main.MapTracker.LogWarning("Adding item: " + itemId);
        }
    }
}
