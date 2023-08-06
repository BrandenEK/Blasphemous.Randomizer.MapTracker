using BlasphemousRandomizer;
using BlasphemousRandomizer.DoorRando;
using BlasphemousRandomizer.ItemRando;
using Framework.Managers;
using Framework.Map;
using Gameplay.UI.Others.MenuLogic;
using LogicParser;
using ModdingAPI;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace RandoMap
{
    public class MapTracker : Mod
    {
        public MapTracker(string modId, string modName, string modVersion) : base(modId, modName, modVersion) { }

        private Transform marksHolder;
        private Text locationText;
        private Sprite mapMarker;

        private BlasphemousInventory currentInventory;
        private List<string> currentVisibleRooms;

        public bool DisplayLocationMarks { get; private set; }
        public bool DisplayLocationsLastFrame { get; private set; }

        private MapLocation currentSelectedCell = null;
        private int currentSelectedIndex = 0;

        public bool IsShowingMap => PauseWidget.IsActive() && PauseWidget.CurrentWidget == PauseWidget.ChildWidgets.MAP && PauseWidget.InitialMapMode == PauseWidget.MapModes.SHOW;

        protected override void Initialize()
        {
            DisableFileLogging = true;
            DisplayLocationMarks = true;

            if (FileUtil.loadDataImages("marker.png", new Vector2Int(10, 10), new Vector2(0.5f, 0.5f), 10, 0, true, out Sprite[] images))
                mapMarker = images[0];

            ResetInventory();
        }

        protected override void LevelLoaded(string oldLevel, string newLevel)
        {
            if (newLevel == "MainMenu")
            {
                ResetInventory();
            }
        }

        protected override void Update()
        {
            // Toggle display status of location marks
            if (Input.GetKeyDown("ToggleLocations") && IsShowingMap)
            {
                DisplayLocationMarks = !DisplayLocationMarks;
                MapWidget.Initialize();
                MapWidget.OnShow(PauseWidget.MapModes.SHOW);
            }

            DisplayLocationsLastFrame = DisplayLocationMarks;
        }

        public void ResetInventory()
        {
            currentInventory = null;
            currentVisibleRooms = null;
        }

        public void RefreshMap()
        {
            if (marksHolder == null)
                CreateMarksHolder();
            if (marksHolder != null)
                marksHolder.SetAsLastSibling();
            if (locationText == null)
                CreateLocationText();

            currentSelectedCell = null;
            currentSelectedIndex = 0;

            if (DisplayLocationMarks && IsShowingMap) // Determine how marks should be shown based on logic
            {
                Config config = Main.Randomizer.GameSettings;
                if (currentInventory == null) // If new items have been obtained, need to recalculate inventory data
                {
                    currentInventory = CreateCurrentInventory(config, out currentVisibleRooms);
                }

                // Check if each one has been collected or is in logic
                foreach (Transform mark in marksHolder)
                {
                    Vector2 cellPosition = new Vector2(mark.localPosition.x / 16, mark.localPosition.y / 16);
                    if (!mapLocations.TryGetValue(cellPosition, out MapLocation mapLocation))
                    {
                        LogError(cellPosition + " is not a cell that contains locations!");
                        continue;
                    }
                    CollectionStatus collectionStatus = mapLocation.GetCurrentStatus(config, currentInventory, currentVisibleRooms);

                    mark.gameObject.SetActive(true);
                    mapLocation.Image.color = colorCodes[collectionStatus];
                }
            }
            else // Hide all marks
            {
                foreach (Transform mark in marksHolder)
                    mark.gameObject.SetActive(false);
            }
        }

        public void UpdateSelectedCell(CellData currentCell)
        {
            if (locationText == null)
                CreateLocationText();

            // Ensure that the location marks are showing and you are hovering over a real cell
            if (!DisplayLocationMarks || !IsShowingMap || currentCell == null)
            {
                currentSelectedCell = null;
                locationText.text = string.Empty;
                return;
            }

            // Ensure that the current cell has an item location on it
            Vector2 currentPosition = currentCell.CellKey.GetVector2();
            if (!mapLocations.ContainsKey(currentPosition))
            {
                currentSelectedCell = null;
                locationText.text = string.Empty;
                return;
            }

            // Ensure that not all of the locations on the cell have been collected
            Config config = Main.Randomizer.GameSettings;
            if (currentInventory == null) // If new items have been obtained, need to recalculate inventory data
            {
                currentInventory = CreateCurrentInventory(config, out currentVisibleRooms);
            }

            MapLocation currentLocation = mapLocations[currentPosition];
            CollectionStatus currentStatus = currentLocation.GetCurrentStatus(Main.Randomizer.GameSettings, currentInventory, currentVisibleRooms);
            //if (currentStatus == CollectionStatus.AllCollected)
            //{
            //    currentSelectedCell = null;
            //    locationText.text = string.Empty;
            //    return;
            //}

            // Only update the text if the current cell has changed
            if (currentSelectedCell != currentLocation)
            {
                currentSelectedCell = currentLocation;
                currentSelectedIndex = currentLocation.NextSelectableIndex(-1, 1, Main.Randomizer.GameSettings);
                UpdateLocationText();
            }
        }

        public void TabLocationIndex(int direction)
        {
            if (currentSelectedCell == null) return;

            currentSelectedIndex = currentSelectedCell.NextSelectableIndex(currentSelectedIndex, direction, Main.Randomizer.GameSettings);
            UpdateLocationText();
        }

        private void UpdateLocationText()
        {
            locationText.text = currentSelectedCell.SelectedLocationName(currentSelectedIndex);

            CollectionStatus selectedStatus = currentSelectedCell.SelectedLocationStatus(currentSelectedIndex, currentInventory, currentVisibleRooms);
            locationText.color = colorCodes[selectedStatus];
        }

        private void CreateMarksHolder()
        {
            Log("Creating marks holder");
            Transform rootRenderer = MapWidget?.transform.Find("Background/Map/MapMask/MapRoot/RootRenderer_0");
            if (rootRenderer == null) return;

            marksHolder = new GameObject("MarksHolder", typeof(RectTransform)).transform as RectTransform;
            marksHolder.SetParent(rootRenderer, false);

            foreach (KeyValuePair<Vector2, MapLocation> mapLocation in mapLocations)
            {
                RectTransform rect = new GameObject("ML", typeof(RectTransform)).transform as RectTransform;
                rect.SetParent(marksHolder, false);
                rect.localRotation = Quaternion.identity;
                rect.localScale = Vector3.one;
                rect.localPosition = new Vector2(16 * mapLocation.Key.x, 16 * mapLocation.Key.y);
                rect.sizeDelta = new Vector2(10, 10);
                mapLocation.Value.Image = rect.gameObject.AddComponent<Image>();
                mapLocation.Value.Image.sprite = mapMarker;
            }
        }

        private void CreateLocationText()
        {
            //LogWarning(MapWidget.transform.Find("Background/LowerZone").DisplayHierarchy(10, false));
            Log("Creating location text");
            Transform iconHolder = MapWidget?.transform.Find("Background/LowerZone/MarkSelector/IconList");
            if (iconHolder == null) return;

            // Hide marks stuff
            ((RectTransform)iconHolder.transform.GetChild(0)).anchoredPosition += Vector2.down * 100;
            //Transform buttonHolder = MapWidget.transform.Find("Background/LowerZone/Buttons/Marker"); // This somehow doesnt work
            //foreach (Transform button in buttonHolder)
            //{
            //    ((RectTransform)button).anchoredPosition += Vector2.down * 100;
            //}

            // Create text in holder
            locationText = Object.Instantiate(MapWidget.CherubsText.gameObject).GetComponent<Text>();
            locationText.rectTransform.SetParent(iconHolder, false);
            locationText.rectTransform.anchorMin = Vector2.zero;
            locationText.rectTransform.anchorMax = Vector2.one;
            locationText.rectTransform.anchoredPosition = Vector2.zero;
            locationText.alignment = TextAnchor.MiddleCenter;
            locationText.text = "Location name";
        }

        private BlasphemousInventory CreateCurrentInventory(Config settings, out List<string> visibleRooms)
        {
            Log("Calculating current inventory");
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
            var checkedDoors = new List<DoorLocation>();
            var unreachableDoors = new List<DoorLocation>();
            List<DoorLocation> previousUnreachableDoors;
            Stack<DoorLocation> currentDoors;

            DoorLocation startingDoor = allDoorLocations[Main.Randomizer.StartingDoor.Door];
            roomObjects["Initial"].AddRange(roomObjects[startingDoor.Room]); // Starting room is visible
            roomObjects["D02Z02S11"].AddRange(roomObjects["D01Z02S03"]); // Albero elevator room is also visible after graveyard elevator
            foreach (string obj in roomObjects["Initial"])
            {
                if (obj[0] == 'D')
                {
                    DoorLocation door = allDoorLocations[obj];
                    if (door.Direction != 5)
                        unreachableDoors.Add(door); // Maybe instead check visibility flags
                }
            }
            inventory.AddItem(startingDoor.Id);
            visibleRooms.Add(startingDoor.Room);
            visibleRooms.Add("Initial");

            while (true) // While more doors were made reachable this cycle
            {
                // Remake doors list
                currentDoors = new Stack<DoorLocation>(unreachableDoors);
                previousUnreachableDoors = new List<DoorLocation>(unreachableDoors);
                unreachableDoors.Clear();

                while (currentDoors.Count > 0) // While there are more visible doors, search them
                {
                    DoorLocation enterDoor = currentDoors.Pop();
                    if (checkedDoors.Contains(enterDoor)) continue;

                    if (Parser.EvaluateExpression(enterDoor.Logic, inventory))
                    {
                        DoorLocation exitDoor = Main.Randomizer.itemShuffler.GetTargetDoor(enterDoor.Id);
                        if (exitDoor == null) exitDoor = allDoorLocations[enterDoor.OriginalDoor];

                        checkedDoors.Add(enterDoor);
                        checkedDoors.Add(exitDoor);
                        inventory.AddItem(enterDoor.Id);
                        inventory.AddItem(exitDoor.Id);
                        unreachableDoors.Remove(enterDoor);
                        unreachableDoors.Remove(exitDoor);

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
                    else
                    {
                        // If door is unreachable, check it again on the next cycle
                        if (!unreachableDoors.Contains(enterDoor))
                            unreachableDoors.Add(enterDoor);
                    }
                }

                // If the exact same doors were in the unreachabled list last time, then no more are reachable
                // Otherwise, recheck all of the unreachable doors in case a door opened it up
                if (unreachableDoors.IsReverseOf(previousUnreachableDoors))
                    break;
            }

            return inventory;
        }

        private NewMapMenuWidget _mapWidget;
        private NewMapMenuWidget MapWidget => _mapWidget ??= _mapWidget = Object.FindObjectOfType<NewMapMenuWidget>();

        private PauseWidget _pauseWidget;
        private PauseWidget PauseWidget => _pauseWidget ??= _pauseWidget = Object.FindObjectOfType<PauseWidget>();

        private static Color RGBColor(int r, int g, int b)
        {
            return new Color(r / 255f, g / 255f, b / 255f);
        }

        private readonly Dictionary<CollectionStatus, Color> colorCodes = new()
        {
            { CollectionStatus.NoneReachable, RGBColor(207, 16, 16) },
            { CollectionStatus.SomeReachable, RGBColor(255, 159, 32) },
            { CollectionStatus.AllReachable, RGBColor(32, 255, 32) },
            { CollectionStatus.AllCollected, RGBColor(63, 63, 63) },
            { CollectionStatus.HintReachable, RGBColor(32, 255, 255) },
            { CollectionStatus.HintUnreachable, RGBColor(192, 16, 255) },
        };

        private readonly Dictionary<Vector2, MapLocation> mapLocations = new()
        {
            // Holy Line
            { new Vector2(17, 41), new MapLocation("QI31") },
            { new Vector2(23, 41), new MapLocation("PR14", "RB07") },
            { new Vector2(25, 40), new MapLocation("CO04") },
            { new Vector2(27, 40), new MapLocation("QI55") },
            { new Vector2(27, 41), new MapLocation("RESCUED_CHERUB_07") },
            // Albero
            { new Vector2(29, 40), new MapLocation("QI65") },
            { new Vector2(30, 40), new MapLocation("Sword[D01Z02S06]", "COMBO_1", "COMBO_2", "COMBO_3", "CHARGED_1", "CHARGED_2", "CHARGED_3", "RANGED_1", "RANGED_2", "RANGED_3", "LUNGE_1", "LUNGE_2", "LUNGE_3", "VERTICAL_1", "VERTICAL_2", "VERTICAL_3") },
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
            { new Vector2(50, 39), new MapLocation("QI32") },
            { new Vector2(51, 31), new MapLocation("BS01") },
            { new Vector2(51, 35), new MapLocation("CO03") },
            { new Vector2(51, 38), new MapLocation("CO30") },
            { new Vector2(53, 30), new MapLocation("CO38") },
            { new Vector2(53, 35), new MapLocation("PR01") },
            { new Vector2(55, 30), new MapLocation("CO21") },
            { new Vector2(56, 30), new MapLocation("RESCUED_CHERUB_33", "RB26") },
            // Cistern
            { new Vector2(31, 30), new MapLocation("Sword[D01Z05S24]", "COMBO_1", "COMBO_2", "COMBO_3", "CHARGED_1", "CHARGED_2", "CHARGED_3", "RANGED_1", "RANGED_2", "RANGED_3", "LUNGE_1", "LUNGE_2", "LUNGE_3", "VERTICAL_1", "VERTICAL_2", "VERTICAL_3") },
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
            { new Vector2(34, 47), new MapLocation("QI07") },
            { new Vector2(38, 43), new MapLocation("PR04") },
            { new Vector2(38, 45), new MapLocation("QI68") },
            { new Vector2(39, 44), new MapLocation("RESCUED_CHERUB_27") },
            { new Vector2(39, 45), new MapLocation("QI20") },
            { new Vector2(41, 47), new MapLocation("RESCUED_CHERUB_23") },
            { new Vector2(42, 44), new MapLocation("CO19") },
            { new Vector2(42, 45), new MapLocation("QI59", "RB10") },
            { new Vector2(43, 44), new MapLocation("CO11") },
            { new Vector2(43, 47), new MapLocation("HE05") },
            // Graveyard
            { new Vector2(31, 49), new MapLocation("RB38", "QI33") },
            { new Vector2(32, 48), new MapLocation("RESCUED_CHERUB_26") },
            { new Vector2(32, 50), new MapLocation("QI53") },
            { new Vector2(33, 49), new MapLocation("QI11", "RB37", "RB02") },
            { new Vector2(33, 50), new MapLocation("Lady[D02Z02S12]") },
            { new Vector2(33, 58), new MapLocation("HE11") },
            { new Vector2(34, 54), new MapLocation("RESCUED_CHERUB_25") },
            { new Vector2(34, 55), new MapLocation("RB32") },
            { new Vector2(34, 56), new MapLocation("CO01") },
            { new Vector2(35, 48), new MapLocation("CO42") },
            { new Vector2(35, 49), new MapLocation("RESCUED_CHERUB_31") },
            { new Vector2(35, 53), new MapLocation("RESCUED_CHERUB_24") },
            { new Vector2(35, 58), new MapLocation("RB15") },
            { new Vector2(36, 53), new MapLocation("QI46") },
            { new Vector2(36, 54), new MapLocation("CO29") },
            { new Vector2(36, 56), new MapLocation("QI08") },
            { new Vector2(36, 57), new MapLocation("Oil[D02Z02S10]") },
            { new Vector2(37, 56), new MapLocation("RB106", "Amanecida[D02Z02S14]") },
            // Convent
            { new Vector2(22, 63), new MapLocation("RB107") },
            { new Vector2(23, 59), new MapLocation("RB24") },
            { new Vector2(26, 58), new MapLocation("HE03") },
            { new Vector2(26, 62), new MapLocation("RB18") },
            { new Vector2(27, 60), new MapLocation("CO15") },
            { new Vector2(27, 62), new MapLocation("RB08") },
            { new Vector2(29, 63), new MapLocation("BS03") },
            { new Vector2(30, 62), new MapLocation("CO05") },
            { new Vector2(31, 63), new MapLocation("QI40", "QI57") },
            { new Vector2(31, 64), new MapLocation("Lady[D02Z03S15]") },
            { new Vector2(31, 65), new MapLocation("QI61") },
            { new Vector2(33, 63), new MapLocation("Sword[D02Z03S13]", "COMBO_1", "COMBO_2", "COMBO_3", "CHARGED_1", "CHARGED_2", "CHARGED_3", "RANGED_1", "RANGED_2", "RANGED_3", "LUNGE_1", "LUNGE_2", "LUNGE_3", "VERTICAL_1", "VERTICAL_2", "VERTICAL_3") },
            // Mountains
            { new Vector2(15, 36), new MapLocation("QI63") },
            { new Vector2(18, 36), new MapLocation("RESCUED_CHERUB_16") },
            { new Vector2(19, 36), new MapLocation("QI47", "Amanecida[D03Z01S03]") },
            { new Vector2(21, 36), new MapLocation("RB22") },
            { new Vector2(23, 37), new MapLocation("RB13", "QI14") },
            { new Vector2(32, 37), new MapLocation("CO13") },
            // Jondo
            { new Vector2(14, 33), new MapLocation("QI52") },
            { new Vector2(15, 34), new MapLocation("RB28") },
            { new Vector2(16, 35), new MapLocation("RESCUED_CHERUB_17") },
            { new Vector2(17, 29), new MapLocation("QI41") },
            { new Vector2(18, 28), new MapLocation("CO07") },
            { new Vector2(21, 28), new MapLocation("QI19") },
            { new Vector2(21, 29), new MapLocation("CO33") },
            { new Vector2(21, 34), new MapLocation("CO08") },
            { new Vector2(21, 35), new MapLocation("PR10") },
            { new Vector2(22, 32), new MapLocation("RESCUED_CHERUB_18") },
            { new Vector2(25, 32), new MapLocation("RESCUED_CHERUB_37") },
            { new Vector2(26, 32), new MapLocation("HE06") },
            { new Vector2(27, 32), new MapLocation("QI103") },
            // Grievance
            { new Vector2(21, 22), new MapLocation("QI44") },
            { new Vector2(22, 16), new MapLocation("QI13", "RB06") },
            { new Vector2(22, 25), new MapLocation("QI34") },
            { new Vector2(24, 19), new MapLocation("Oil[D03Z03S13]") },
            { new Vector2(25, 17), new MapLocation("RESCUED_CHERUB_20") },
            { new Vector2(25, 23), new MapLocation("CO12") },
            { new Vector2(25, 26), new MapLocation("RE07", "RESCUED_CHERUB_19") },
            { new Vector2(29, 19), new MapLocation("BS04") },
            { new Vector2(29, 22), new MapLocation("QI10", "RESCUED_CHERUB_21") },
            { new Vector2(31, 19), new MapLocation("QI39") },
            // Patio
            { new Vector2(57, 42), new MapLocation("RESCUED_CHERUB_35") },
            { new Vector2(58, 43), new MapLocation("CO23") },
            { new Vector2(58, 47), new MapLocation("QI102") },
            { new Vector2(61, 42), new MapLocation("RB14") },
            { new Vector2(62, 42), new MapLocation("QI37") },
            { new Vector2(62, 43), new MapLocation("CO39") },
            { new Vector2(64, 43), new MapLocation("RESCUED_CHERUB_28") },
            { new Vector2(65, 42), new MapLocation("RB21") },
            { new Vector2(66, 42), new MapLocation("Amanecida[D04Z01S04]") },
            // Mother of Mothers
            { new Vector2(67, 43), new MapLocation("RE402", "RESCUED_CHERUB_30") },
            { new Vector2(67, 44), new MapLocation("CO17") },
            { new Vector2(68, 44), new MapLocation("QI79") },
            { new Vector2(68, 46), new MapLocation("QI60") },
            { new Vector2(70, 46), new MapLocation("BS05") },
            { new Vector2(73, 44), new MapLocation("CO20") },
            { new Vector2(73, 46), new MapLocation("RESCUED_CHERUB_29") },
            { new Vector2(73, 47), new MapLocation("Sword[D04Z02S12]", "COMBO_1", "COMBO_2", "COMBO_3", "CHARGED_1", "CHARGED_2", "CHARGED_3", "RANGED_1", "RANGED_2", "RANGED_3", "LUNGE_1", "LUNGE_2", "LUNGE_3", "VERTICAL_1", "VERTICAL_2", "VERTICAL_3") },
            { new Vector2(74, 41), new MapLocation("Oil[D04Z02S14]") },
            { new Vector2(75, 44), new MapLocation("CO34") },
            { new Vector2(80, 45), new MapLocation("RB33") },
            { new Vector2(80, 50), new MapLocation("HE01") },
            { new Vector2(81, 44), new MapLocation("CO35") },
            { new Vector2(83, 46), new MapLocation("RE03", "QI54") },
            // Knot of Words
            { new Vector2(73, 41), new MapLocation("HE201") },
            // All the Tears
            { new Vector2(85, 42), new MapLocation("PR201") },
            // Library
            { new Vector2(59, 31), new MapLocation("Oil[D05Z01S19]") },
            { new Vector2(60, 32), new MapLocation("RESCUED_CHERUB_02") },
            { new Vector2(60, 33), new MapLocation("RB30") },
            { new Vector2(60, 35), new MapLocation("RB203", "CO28") },
            { new Vector2(60, 38), new MapLocation("RB31") },
            { new Vector2(62, 35), new MapLocation("PR07") },
            { new Vector2(64, 36), new MapLocation("Lady[D05Z01S14]") },
            { new Vector2(65, 34), new MapLocation("PR15") },
            { new Vector2(68, 37), new MapLocation("QI50") },
            { new Vector2(68, 41), new MapLocation("CO22") },
            { new Vector2(69, 39), new MapLocation("CO18") },
            { new Vector2(69, 41), new MapLocation("QI80") },
            { new Vector2(70, 40), new MapLocation("RESCUED_CHERUB_01") },
            { new Vector2(71, 39), new MapLocation("Sword[D05Z01S13]", "COMBO_1", "COMBO_2", "COMBO_3", "CHARGED_1", "CHARGED_2", "CHARGED_3", "RANGED_1", "RANGED_2", "RANGED_3", "LUNGE_1", "LUNGE_2", "LUNGE_3", "VERTICAL_1", "VERTICAL_2", "VERTICAL_3") },
            { new Vector2(71, 40), new MapLocation("RB301") },
            { new Vector2(72, 34), new MapLocation("RESCUED_CHERUB_32") },
            { new Vector2(73, 39), new MapLocation("QI62") },
            { new Vector2(74, 39), new MapLocation("RB19") },
            // Canvases
            { new Vector2(57, 31), new MapLocation("QI104") },
            { new Vector2(60, 30), new MapLocation("RB12", "QI49", "QI71") },
            { new Vector2(64, 30), new MapLocation("QI64") },
            { new Vector2(66, 30), new MapLocation("HE07") },
            { new Vector2(66, 32), new MapLocation("RE05", "PR05") },
            { new Vector2(71, 32), new MapLocation("BS06") },
            { new Vector2(72, 31), new MapLocation("CO31") },
            // Rooftops
            { new Vector2(72, 51), new MapLocation("CO06") },
            { new Vector2(72, 52), new MapLocation("RESCUED_CHERUB_36") },
            { new Vector2(72, 55), new MapLocation("PR12") },
            { new Vector2(74, 48), new MapLocation("HE04") },
            { new Vector2(76, 49), new MapLocation("QI02") },
            { new Vector2(76, 52), new MapLocation("QI03") },
            { new Vector2(77, 59), new MapLocation("BS16") },
            { new Vector2(78, 51), new MapLocation("Lady[D06Z01S24]") },
            { new Vector2(78, 56), new MapLocation("QI04") },
            { new Vector2(79, 54), new MapLocation("CO40") },
            { new Vector2(80, 56), new MapLocation("Sword[D06Z01S11]", "COMBO_1", "COMBO_2", "COMBO_3", "CHARGED_1", "CHARGED_2", "CHARGED_3", "RANGED_1", "RANGED_2", "RANGED_3", "LUNGE_1", "LUNGE_2", "LUNGE_3", "VERTICAL_1", "VERTICAL_2", "VERTICAL_3") },
            // Deambulatory
            { new Vector2(80, 59), new MapLocation("PR08") },
            // Bridge
            { new Vector2(52, 41), new MapLocation("BS12", "PR09") },
            { new Vector2(54, 44), new MapLocation("HE101") },
            // Hall
            { new Vector2(55, 47), new MapLocation("LaudesBossTrigger[30000]") },
            { new Vector2(56, 44), new MapLocation("QI105") },
            // Wall
            { new Vector2(55, 51), new MapLocation("QI81") },
            { new Vector2(57, 50), new MapLocation("BS14") },
            { new Vector2(57, 51), new MapLocation("QI72") },
            { new Vector2(58, 49), new MapLocation("Oil[D09Z01S12]") },
            { new Vector2(58, 51), new MapLocation("RESCUED_CHERUB_34") },
            { new Vector2(59, 48), new MapLocation("CO02") },
            { new Vector2(59, 50), new MapLocation("CO26") },
            { new Vector2(60, 49), new MapLocation("RB16") },
            { new Vector2(60, 52), new MapLocation("RESCUED_CHERUB_05") },
            { new Vector2(62, 48), new MapLocation("CO27", "RESCUED_CHERUB_04") },
            { new Vector2(62, 49), new MapLocation("QI70") },
            { new Vector2(62, 50), new MapLocation("QI51") },
            { new Vector2(62, 51), new MapLocation("RESCUED_CHERUB_03") },
            { new Vector2(63, 50), new MapLocation("CO10", "QI69") },
            { new Vector2(63, 51), new MapLocation("CO24") },
            { new Vector2(64, 49), new MapLocation("CO37") },
            { new Vector2(64, 50), new MapLocation("RB11") },
            { new Vector2(66, 52), new MapLocation("Amanecida[D09Z01S01]") },
            // Brotherhood
            { new Vector2(03, 44), new MapLocation("QI204", "QI301") },
            { new Vector2(05, 43), new MapLocation("RESCUED_CHERUB_06") },
            { new Vector2(05, 44), new MapLocation("PR203") },
            { new Vector2(07, 43), new MapLocation("RB204") },
            { new Vector2(11, 37), new MapLocation("Sword[D17Z01S08]", "COMBO_1", "COMBO_2", "COMBO_3", "CHARGED_1", "CHARGED_2", "CHARGED_3", "RANGED_1", "RANGED_2", "RANGED_3", "LUNGE_1", "LUNGE_2", "LUNGE_3", "VERTICAL_1", "VERTICAL_2", "VERTICAL_3") },
            { new Vector2(11, 40), new MapLocation("QI35") },
            { new Vector2(12, 40), new MapLocation("RE401", "CO25") },
            { new Vector2(13, 40), new MapLocation("RB25") },
            { new Vector2(15, 41), new MapLocation("BS13") },
            { new Vector2(16, 41), new MapLocation("RE01") },
            // Echoes
            { new Vector2(30, 26), new MapLocation("RB202") },
            { new Vector2(31, 33), new MapLocation("RB108") },
            // Mourning
            { new Vector2(43, 25), new MapLocation("PR202") },
            { new Vector2(61, 27), new MapLocation("BossTrigger[5000]", "QI202") },
            { new Vector2(81, 27), new MapLocation("RB201") },
            // Resting Place
            { new Vector2(41, 30), new MapLocation("QI203") },
        };
    }
}
