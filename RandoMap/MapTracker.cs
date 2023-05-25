using ModdingAPI;
using BlasphemousRandomizer;
using BlasphemousRandomizer.ItemRando;
using BlasphemousRandomizer.DoorRando;
using Framework.Inventory;
using Framework.Managers;
using Framework.Map;
using Gameplay.UI.Others.MenuLogic;
using LogicParser;
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

            // Get current inventory based on items
            BlasphemousInventory inventory = CreateCurrentInventory(out List<string> visibleRooms); // Only calculate if displaying marks!!

            // Check if each one has been collected or is in logic
            foreach (Transform mark in marksHolder)
            {
                if (!Main.MapTracker.DisplayLocationMarks)
                {
                    mark.gameObject.SetActive(false);
                    continue;
                }

                MapLocation mapLocation = mapLocations[new Vector2(mark.localPosition.x / 16, mark.localPosition.y / 16)];
                MapLocation.CollectionStatus collectionStatus = mapLocation.GetCurrentStatus(inventory, visibleRooms);

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

        private BlasphemousInventory CreateCurrentInventory(out List<string> visibleRooms)
        {
            Config settings = Main.Randomizer.GameSettings;
            BlasphemousInventory inventory = new BlasphemousInventory();
            inventory.SetConfigSettings(settings);
            
            // Add all obtained items to inventory

            foreach (Item item in Main.Randomizer.data.items.Values)
            {
                switch (item.type)
                {
                    case 0: // Beads
                    case 1: // Prayers
                    case 2: // Relics
                    case 3: // Hearts
                    case 4: // Bones
                    case 5: // Quest items
                        if (item is ProgressiveItem progressiveItem)
                        {
                            foreach (string subItemId in progressiveItem.items)
                            {
                                if (Core.Events.GetFlag("ITEM_" + subItemId))
                                    inventory.AddItem(item.id);
                            }
                        }
                        else
                        {
                            if (Core.Events.GetFlag("ITEM_" + item.id))
                                inventory.AddItem(item.id);
                        }
                        break;
                    case 6: // Cherubs
                        int cherubs = CherubCaptorPersistentObject.CountRescuedCherubs();
                        for (int i = 0; i < cherubs; i++)
                            inventory.AddItem(item.id);
                        break;
                    case 7: // Life
                    case 8: // Fervour
                    case 9: // Strength
                        int upgrades;
                        if (item.type == 7)
                            upgrades = Core.Logic.Penitent.Stats.Life.GetUpgrades();
                        else if (item.type == 8)
                            upgrades = Core.Logic.Penitent.Stats.Fervour.GetUpgrades();
                        else
                            upgrades = Core.Logic.Penitent.Stats.Strength.GetUpgrades();
                        for (int i = 0; i < upgrades; i++)
                            inventory.AddItem(item.id);
                        break;
                    case 10: // Tears
                        break;
                    case 11: // Sword skills
                        ProgressiveItem skillItem = item as ProgressiveItem;
                        foreach (string skillId in skillItem.items)
                        {
                            if (Core.Events.GetFlag("ITEM_" + skillId))
                                inventory.AddItem(item.id);
                        }
                        break;
                    case 12: // Special items
                        if (Core.Events.GetFlag("ITEM_" + item.id))
                            inventory.AddItem(item.id);
                        break;
                }
            }
            if (!settings.ShuffleDash) inventory.AddItem("Slide");
            if (!settings.ShuffleWallClimb) inventory.AddItem("WallClimb");

            // Add all reachable doors to inventory

            // Sort all item locations & doors into rooms
            Dictionary<string, ItemLocation> allItemLocations = Main.Randomizer.data.itemLocations;
            Dictionary<string, DoorLocation> allDoorLocations = Main.Randomizer.data.doorLocations;
            Dictionary<string, List<string>> roomObjects = new Dictionary<string, List<string>>();
            foreach (DoorLocation door in allDoorLocations.Values)
            {
                string scene = door.Room;
                if (!roomObjects.ContainsKey(scene))
                    roomObjects.Add(scene, new List<string>());
                roomObjects[scene].Add(door.Id);
            }
            foreach (ItemLocation itemLoc in allItemLocations.Values)
            {
                string scene = itemLoc.Room;
                if (!roomObjects.ContainsKey(scene))
                    roomObjects.Add(scene, new List<string>());
                roomObjects[scene].Add(itemLoc.Id);
            }

            // Set up starting room
            visibleRooms = new List<string>();
            List<DoorLocation> checkedDoors = new List<DoorLocation>();
            Stack<DoorLocation> currentDoors = new Stack<DoorLocation>();
            DoorLocation startingDoor = allDoorLocations[Main.Randomizer.StartingDoor.Door];
            roomObjects["Initial"].AddRange(roomObjects[startingDoor.Room]); // Starting room is visible
            roomObjects["D02Z02S11"].AddRange(roomObjects["D01Z02S03"]); // Albero elevator room is also visible after graveyard elevator
            foreach (string obj in roomObjects["Initial"])
            {
                if (obj[0] == 'D')
                {
                    DoorLocation door = allDoorLocations[obj];
                    if (door.Direction != 5)
                        currentDoors.Push(door); // Maybe instead check visibility flags
                }
            }
            inventory.AddItem(startingDoor.Id);
            visibleRooms.Add(startingDoor.Room);
            visibleRooms.Add("Initial");

            // While there are more visible doors, search them
            while (currentDoors.Count > 0)
            {
                DoorLocation enterDoor = currentDoors.Pop();
                if (checkedDoors.Contains(enterDoor)) continue;

                if (enterDoor.Logic == null || Parser.EvaluateExpression(enterDoor.Logic, inventory))
                {
                    DoorLocation exitDoor = Main.Randomizer.itemShuffler.GetTargetDoor(enterDoor.Id);
                    if (exitDoor == null) exitDoor = allDoorLocations[enterDoor.OriginalDoor];

                    checkedDoors.Add(enterDoor);
                    checkedDoors.Add(exitDoor);
                    inventory.AddItem(enterDoor.Id);
                    inventory.AddItem(exitDoor.Id);

                    string newRoom = exitDoor.Room;
                    foreach (string obj in roomObjects[newRoom])
                    {
                        if (obj[0] == 'D')
                        {
                            // If this door hasn't already been processed, make it visible
                            DoorLocation newDoor = allDoorLocations[obj];
                            if (newDoor.ShouldBeMadeVisible(settings, inventory))
                            {
                                currentDoors.Push(newDoor);
                            }
                        }
                    }
                    if (!visibleRooms.Contains(newRoom))
                        visibleRooms.Add(newRoom);
                    if (newRoom == "D02Z02S11" && !visibleRooms.Contains("D01Z02S03"))
                        visibleRooms.Add("D01Z02S03");
                }
            }

            return inventory;
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
            { new Vector2(23, 40), new MapLocation("QI101") },
            // Olive Trees
            { new Vector2(), new MapLocation("") },
            { new Vector2(), new MapLocation("") },

            // { new Vector2(), new MapLocation("") },
        };
    }
}
