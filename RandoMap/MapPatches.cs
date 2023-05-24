using HarmonyLib;
using Framework.Managers;
using Gameplay.UI.Others.MenuLogic;

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
}
