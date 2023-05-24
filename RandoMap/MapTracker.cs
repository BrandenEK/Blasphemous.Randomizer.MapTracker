using ModdingAPI;
using BlasphemousRandomizer.ItemRando;
using Framework.Managers;
using Framework.Map;
using Gameplay.UI.Others.MenuLogic;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace RandoMap
{
    public class MapTracker : Mod
    {
        public MapTracker(string modId, string modName, string modVersion) : base(modId, modName, modVersion) { }

        private Transform marksHolder;
        private Sprite greenMark, redMark;

        public bool DisplayLocationMarks { get; private set; }

        protected override void Update()
        {
            // Debug
            if (UnityEngine.Input.GetKeyDown(KeyCode.P))
            {
                CellKey cell = Core.NewMapManager.GetPlayerCell();
                LogWarning("Current cell position: " + new Vector2(cell.X, cell.Y));
            }

            // Toggle display status of location marks
            if (UnityEngine.Input.GetKeyDown(KeyCode.F1))
            {
                DisplayLocationMarks = !DisplayLocationMarks;
                RefreshMap();
            }
        }

        public void RefreshMap()
        {
            // Only refresh map if paused
            if (!Core.Logic.IsPaused) return;

            LogWarning("Refreshing map locations!");
            if (marksHolder == null)
                CreateMarksHolder();
            if (marksHolder != null)
                marksHolder.SetAsLastSibling();

            // temp
            System.Random rng = new System.Random();

            // Check if each one has been collected or is in logic
            foreach (Transform mark in marksHolder)
            {
                ItemLocation location = Main.Randomizer.data.itemLocations[mark.name];
                string flag = location.LocationFlag == null ? "LOCATION_" + location.Id : location.LocationFlag.Split('~')[0];
                bool shouldDisplayLocation = DisplayLocationMarks && !Core.Events.GetFlag(flag);

                mark.gameObject.SetActive(shouldDisplayLocation);
                if (shouldDisplayLocation)
                {
                    // These needs to be better !! Cant get compoennet for each of them every time
                    mark.GetComponent<Image>().sprite = rng.Next(2) == 1 ? redMark : greenMark;
                }
            }
        }

        private void CreateMarksHolder()
        {
            Log("Creating marks holder");

            NewMapMenuWidget widget = Object.FindObjectOfType<NewMapMenuWidget>();
            if (widget == null) return;
            Transform rootRenderer = widget.transform.Find("Background/Map/MapMask/MapRoot/RootRenderer_0");
            if (rootRenderer == null) return;

            marksHolder = new GameObject("MarksHolder", typeof(RectTransform)).transform as RectTransform;
            marksHolder.SetParent(rootRenderer, false);

            MapRendererConfig cfg = widget.RendererConfigs[0];
            greenMark = cfg.Marks[MapData.MarkType.Green];
            redMark = cfg.Marks[MapData.MarkType.Red];
            Vector2 markSize = new Vector2(greenMark.rect.width, greenMark.rect.height);

            foreach (KeyValuePair<string, Vector2> mapLocation in mapLocations)
            {
                RectTransform rect = new GameObject(mapLocation.Key, typeof(RectTransform)).transform as RectTransform;
                rect.SetParent(marksHolder, false);
                rect.localRotation = Quaternion.identity;
                rect.localScale = Vector3.one;
                rect.localPosition = new Vector2(16 * mapLocation.Value.x, 16 * mapLocation.Value.y);
                rect.sizeDelta = markSize;
                rect.gameObject.AddComponent<Image>();
                Main.MapTracker.Log($"Creating mark at " + rect.localPosition);
            }
        }

        private Dictionary<string, Vector2> mapLocations = new Dictionary<string, Vector2>()
        {
            // Brotherhood
            { "RESCUED_CHERUB_06", new Vector2(5, 43) },
            { "RB204", new Vector2(7, 43) },
            { "RE401", new Vector2(12, 40) },
            { "Sword[D17Z01S08]", new Vector2(11, 37) },
            { "BS13", new Vector2(15, 41) },
            { "PR203", new Vector2(5, 44) },
            { "QI204", new Vector2(3, 44) },
            { "QI301", new Vector2(3, 44) },
            { "RE01", new Vector2(16, 41) },
            { "CO25", new Vector2(12, 40) },
            { "QI35", new Vector2(11, 40) },
            { "RB25", new Vector2(13, 40) },
            // Holy Line
            { "PR14", new Vector2(23, 41) },
            { "RB07", new Vector2(23, 41) },
            { "CO04", new Vector2(25, 40) },
            { "QI55", new Vector2(27, 40) },
            { "RESCUED_CHERUB_07", new Vector2(27, 41) },
            { "QI31", new Vector2(17, 41) },
            // Albero
            { "RE02", new Vector2(30, 41) },
            { "RE04", new Vector2(30, 41) },
            { "RE10", new Vector2(30, 41) },
            { "RB01", new Vector2(31, 42) },
            { "QI66", new Vector2(31, 41) }, // More tirso
            { "RESCUED_CHERUB_08", new Vector2(32, 42) },
            { "PR03", new Vector2(32, 41) }, // Lvdovico
            { "CO43", new Vector2(32, 40) },
            { "CO16", new Vector2(34, 41) },
            { "Sword[D01Z02S06]", new Vector2(30, 40) },
            { "QI65", new Vector2(29, 40) },
            { "RB104", new Vector2(33, 41) },
            { "RB105", new Vector2(33, 41) },
            { "PR11", new Vector2(33, 41) },
            { "Undertaker[250]", new Vector2(32, 40) }, // undertaker
            { "QI201", new Vector2(32, 40) },

            //{ "xx-xx", new Vector2() },
        };
    }
}
