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
        //public bool ShowingMap { get; set; }

        protected override void Initialize()
        {
            DisableFileLogging = true;
            DisplayLocationMarks = true;
        }

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

            // Check if each one has been collected or is in logic
            foreach (Transform mark in marksHolder)
            {
                if (!Main.MapTracker.DisplayLocationMarks)
                {
                    mark.gameObject.SetActive(false);
                    continue;
                }

                MapLocation mapLocation = mapLocations[new Vector2(mark.localPosition.x / 16, mark.localPosition.y / 16)];
                MapLocation.CollectionStatus collectionStatus = mapLocation.CurrentStatus;

                mark.gameObject.SetActive(collectionStatus != MapLocation.CollectionStatus.AllCollected);
                if (collectionStatus == MapLocation.CollectionStatus.NoneReachable)
                    mapLocation.Image.sprite = redMark;
                else if (collectionStatus == MapLocation.CollectionStatus.SomeReachable)
                    mapLocation.Image.sprite = greenMark;
                else if (collectionStatus == MapLocation.CollectionStatus.AllReachable)
                    mapLocation.Image.sprite = greenMark;
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

            foreach (KeyValuePair<Vector2, MapLocation> mapLocation in mapLocations)
            {
                RectTransform rect = new GameObject("ML", typeof(RectTransform)).transform as RectTransform;
                rect.SetParent(marksHolder, false);
                rect.localRotation = Quaternion.identity;
                rect.localScale = Vector3.one;
                rect.localPosition = new Vector2(16 * mapLocation.Key.x, 16 * mapLocation.Key.y);
                rect.sizeDelta = markSize;
                mapLocation.Value.Image = rect.gameObject.AddComponent<Image>();
                Main.MapTracker.Log($"Creating mark at " + rect.localPosition);
            }
        }

        //private Transform m_MapRenderer;
        //private Transform MapRenderer
        //{
        //    get
        //    {
        //        if (m_MapRenderer == null)
        //        {
        //            m_MapRenderer = Object.FindObjectOfType<NewMapMenuWidget>()?.transform.Find("Background/Map/MapMask/MapRoot/RootRenderer_0");
        //        }
        //        return m_MapRenderer;
        //    }
        //}

        private Dictionary<Vector2, MapLocation> mapLocations = new Dictionary<Vector2, MapLocation>()
        {
            // Brotherhood
            { new Vector2(3, 44), new MapLocation("QI204", "QI301") },
            { new Vector2(5, 43), new MapLocation("RESCUED_CHERUB_06") },
            { new Vector2(5, 44), new MapLocation("PR203") },
            { new Vector2(7, 43), new MapLocation("RB204") },
            { new Vector2(11, 37), new MapLocation("Sword[D17Z01S08]") },
            { new Vector2(11, 40), new MapLocation("QI35") },
            { new Vector2(12, 40), new MapLocation("RE401", "CO25") },
            { new Vector2(13, 40), new MapLocation("RB25") },
            { new Vector2(15, 41), new MapLocation("BS13") },
            { new Vector2(16, 41), new MapLocation("RE01") },
            // Holy Line
            { new Vector2(17, 41), new MapLocation("QI31") },
            { new Vector2(23, 41), new MapLocation("PR14", "RB07") },
            { new Vector2(25, 40), new MapLocation("CO04") },
            { new Vector2(27, 40), new MapLocation("QI55") },
            { new Vector2(27, 41), new MapLocation("RESCUED_CHERUB_07") },
            // Albero
            { new Vector2(29, 40), new MapLocation("QI65") },
            { new Vector2(30, 40), new MapLocation("Sword[D01Z02S06]") },
            { new Vector2(30, 41), new MapLocation("RE02", "RE04", "RE10") },
            { new Vector2(31, 41), new MapLocation("QI66", "Tirso[500]", "Tirso[1000]", "Tirso[2000]", "Tirso[5000]", "Tirso[10000]", "QI56") },
            { new Vector2(31, 42), new MapLocation("RB01") },
            { new Vector2(32, 40), new MapLocation("CO43", "QI201", "Undertaker[250]", "Undertaker[500]", "Undertaker[750]", "Undertaker[1000]", "Undertaker[1250]", "Undertaker[1500]", "Undertaker[1750]", "Undertaker[2000]", "Undertaker[2500]", "Undertaker[3000]", "Undertaker[5000]") },
            { new Vector2(32, 41), new MapLocation("Lvdovico[500]", "Lvdovico[1000]", "PR03", "QI01") },
            { new Vector2(32, 42), new MapLocation("RESCUED_CHERUB_08") },
            { new Vector2(33, 41), new MapLocation("RB104", "RB105", "PR11") },
            { new Vector2(34, 41), new MapLocation("CO16") },
            // Wasteland
            { new Vector2(37, 40), new MapLocation("RB04") },
            { new Vector2(40, 41), new MapLocation("CO14") },
            { new Vector2(42, 41), new MapLocation("CO36") },
            { new Vector2(43, 41), new MapLocation("RESCUED_CHERUB_10") },
            { new Vector2(44, 43), new MapLocation("HE02", "RESCUED_CHERUB_38") },
            { new Vector2(47, 41), new MapLocation("RB20") },
            { new Vector2(48, 39), new MapLocation("QI06") },
            // Mercy Dreams
            { new Vector2(48, 31), new MapLocation("QI38") },
            { new Vector2(48, 34), new MapLocation("QI58", "RB05", "RB09") },
            { new Vector2(48, 35), new MapLocation("RB17") },
            { new Vector2(50, 32), new MapLocation("QI48") },
            { new Vector2(50, 35), new MapLocation("RESCUED_CHERUB_09") },
            { new Vector2(51, 31), new MapLocation("BS01") },
            { new Vector2(51, 35), new MapLocation("CO03") },
            { new Vector2(51, 38), new MapLocation("CO30") },
            { new Vector2(53, 30), new MapLocation("CO38") },
            { new Vector2(53, 35), new MapLocation("PR01") },
            { new Vector2(55, 30), new MapLocation("CO21") },
            { new Vector2(56, 30), new MapLocation("RESCUED_CHERUB_33", "RB26") },
            // Cistern
            { new Vector2(31, 30), new MapLocation("Sword[D01Z05S24]") },
            { new Vector2(32, 30), new MapLocation("QI75") },
            { new Vector2(33, 20), new MapLocation("CO44") },
            { new Vector2(33, 31), new MapLocation("RESCUED_CHERUB_22") },
            { new Vector2(34, 19), new MapLocation("Lady[D01Z05S26]") },
            { new Vector2(34, 30), new MapLocation("RB03") },
            { new Vector2(34, 37), new MapLocation("RESCUED_CHERUB_15") },
            { new Vector2(35, 33), new MapLocation("RESCUED_CHERUB_12") },
            { new Vector2(35, 36), new MapLocation("Oil[D01Z05S07]") },
            { new Vector2(36, 30), new MapLocation("CO32") },
            { new Vector2(37, 36), new MapLocation("QI12") },
            { new Vector2(38, 36), new MapLocation("RESCUED_CHERUB_14") },
            { new Vector2(40, 35), new MapLocation("QI67") },
            { new Vector2(40, 39), new MapLocation("CO09") },
            { new Vector2(42, 32), new MapLocation("RESCUED_CHERUB_11") },
            { new Vector2(43, 31), new MapLocation("Lady[D01Z05S22]") },
            { new Vector2(43, 38), new MapLocation("PR16", "RESCUED_CHERUB_13") },
            { new Vector2(44, 33), new MapLocation("CO41") },
            { new Vector2(46, 36), new MapLocation("QI45") },
            // Petrous
            { new Vector2(), new MapLocation("") },
            { new Vector2(), new MapLocation("") },
            { new Vector2(), new MapLocation("") },

            // { new Vector2(), new MapLocation("") },
        };
    }
}
