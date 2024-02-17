using Blasphemous.ModdingAPI;
using Blasphemous.Randomizer.DoorRando;
using Blasphemous.Randomizer.ItemRando;
using Framework.Managers;
using Framework.Map;
using Gameplay.UI.Others.MenuLogic;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Blasphemous.Randomizer.MapTracker;

/// <summary>
/// Handles tracking randomizer locations on the in-game map
/// </summary>
public class MapTracker : BlasMod
{
    internal MapTracker() : base(ModInfo.MOD_ID, ModInfo.MOD_NAME, ModInfo.MOD_AUTHOR, ModInfo.MOD_VERSION) { }

    private Transform marksHolder;
    private Text locationText;
    private Sprite mapMarker;

    private BlasphemousInventory currentInventory;
    private List<string> currentVisibleRooms;

    internal bool DisplayLocationMarks { get; private set; }
    internal bool DisplayLocationsLastFrame { get; private set; }

    private IMapLocation currentSelectedCell = null;
    private int currentSelectedIndex = 0;

    /// <summary>
    /// If you are paused on the map screen
    /// </summary>
    public bool IsShowingMap => PauseWidget.IsActive() && PauseWidget.CurrentWidget == PauseWidget.ChildWidgets.MAP && PauseWidget.InitialMapMode == PauseWidget.MapModes.SHOW;

    /// <summary>
    /// Register handlers
    /// </summary>
    protected override void OnInitialize()
    {
        InputHandler.RegisterDefaultKeybindings(new Dictionary<string, KeyCode>()
        {
            { "ToggleDisplay", KeyCode.F7 }
        });
        FileHandler.LoadDataAsSprite("marker.png", out mapMarker, new ModdingAPI.Files.SpriteImportOptions()
        {
            PixelsPerUnit = 10
        });

        DisplayLocationMarks = true;
        ResetInventory();
    }

    /// <summary>
    /// Reset inventory when exiting
    /// </summary>
    protected override void OnExitGame()
    {
        ResetInventory();
    }

    /// <summary>
    /// Process toggling marks
    /// </summary>
    protected override void OnUpdate()
    {
        // Toggle display status of location marks
        if (InputHandler.GetKeyDown("ToggleDisplay") && IsShowingMap)
        {
            DisplayLocationMarks = !DisplayLocationMarks;
            MapWidget.Initialize();
            MapWidget.OnShow(PauseWidget.MapModes.SHOW);
        }

        DisplayLocationsLastFrame = DisplayLocationMarks;
    }

    /// <summary>
    /// Resets the current inventory when collecting a new item or leaving game
    /// </summary>
    public void ResetInventory()
    {
        currentInventory = null;
        currentVisibleRooms = null;
    }

    /// <summary>
    /// Refreshes the color of all map icons when opening it
    /// </summary>
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
                if (!mapLocations.TryGetValue(cellPosition, out IMapLocation mapLocation))
                {
                    LogError(cellPosition + " is not a cell that contains locations!");
                    continue;
                }
                CollectionStatus collectionStatus = mapLocation.GetCurrentStatusTotal(config, currentInventory, currentVisibleRooms);

                mark.gameObject.SetActive(collectionStatus != CollectionStatus.Untracked);
                mapLocation.Image.color = colorCodes[collectionStatus];
            }
        }
        else // Hide all marks
        {
            foreach (Transform mark in marksHolder)
                mark.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// Refreshes the name text when changing selected cell
    /// </summary>
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

        IMapLocation currentLocation = mapLocations[currentPosition];
        CollectionStatus currentStatus = currentLocation.GetCurrentStatusTotal(Main.Randomizer.GameSettings, currentInventory, currentVisibleRooms);
        if (currentStatus == CollectionStatus.Untracked)
        {
            currentSelectedCell = null;
            locationText.text = string.Empty;
            return;
        }

        // Only update the text if the current cell has changed
        if (currentSelectedCell != currentLocation)
        {
            currentSelectedCell = currentLocation;
            currentSelectedIndex = currentLocation.GetNextSelectableIndex(-1, 1, Main.Randomizer.GameSettings);
            UpdateLocationText();
        }
    }

    /// <summary>
    /// Changes the selected cell index
    /// </summary>
    public void TabLocationIndex(int direction)
    {
        if (currentSelectedCell == null) return;

        currentSelectedIndex = currentSelectedCell.GetNextSelectableIndex(currentSelectedIndex, direction, Main.Randomizer.GameSettings);
        UpdateLocationText();
    }

    private void UpdateLocationText()
    {
        locationText.text = currentSelectedCell.GetNameIndividual(currentSelectedIndex);

        CollectionStatus selectedStatus = currentSelectedCell.GetCurrentStatusIndividual(currentSelectedIndex, currentInventory, currentVisibleRooms);
        locationText.color = colorCodes[selectedStatus];
    }

    private void CreateMarksHolder()
    {
        Log("Creating marks holder");
        Transform rootRenderer = MapWidget?.transform.Find("Background/Map/MapMask/MapRoot/RootRenderer_0");
        if (rootRenderer == null) return;

        marksHolder = new GameObject("MarksHolder", typeof(RectTransform)).transform as RectTransform;
        marksHolder.SetParent(rootRenderer, false);

        foreach (KeyValuePair<Vector2, IMapLocation> mapLocation in mapLocations)
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

                if (inventory.Evaluate(enterDoor.Logic))
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
            { CollectionStatus.Untracked, RGBColor(0, 0, 0) },
            { CollectionStatus.Finished, RGBColor(63, 63, 63) },
            { CollectionStatus.AllUnreachable, RGBColor(207, 16, 16) },
            { CollectionStatus.SomeReachable, RGBColor(255, 159, 32) },
            { CollectionStatus.AllReachable, RGBColor(32, 255, 32) },
            { CollectionStatus.HintedAllUnreachable, RGBColor(192, 16, 255) },
            { CollectionStatus.HintedSomeReachable, RGBColor(32, 255, 255) },
            { CollectionStatus.HintedReachable, RGBColor(48, 64, 255) },
        };

    private readonly Dictionary<Vector2, IMapLocation> mapLocations = new()
        {
            // Holy Line
            { new Vector2(17, 41), new SingleLocation("QI31") },
            { new Vector2(23, 41), new MultipleLocation("PR14", "RB07") },
            { new Vector2(25, 40), new SingleLocation("CO04") },
            { new Vector2(27, 40), new SingleLocation("QI55") },
            { new Vector2(27, 41), new SingleLocation("RESCUED_CHERUB_07") },
            // Albero
            { new Vector2(29, 40), new SingleLocation("QI65") },
            { new Vector2(30, 40), new MultipleLocation("Sword[D01Z02S06]", "COMBO_1", "COMBO_2", "COMBO_3", "CHARGED_1", "CHARGED_2", "CHARGED_3", "RANGED_1", "RANGED_2", "RANGED_3", "LUNGE_1", "LUNGE_2", "LUNGE_3", "VERTICAL_1", "VERTICAL_2", "VERTICAL_3") },
            { new Vector2(30, 41), new MultipleLocation("RE02", "RE04", "RE10") },
            { new Vector2(31, 41), new MultipleLocation("QI66", "Tirso[500]", "Tirso[1000]", "Tirso[2000]", "Tirso[5000]", "Tirso[10000]", "QI56") },
            { new Vector2(31, 42), new SingleLocation("RB01") },
            { new Vector2(32, 40), new MultipleLocation("CO43", "QI201", "Undertaker[250]", "Undertaker[500]", "Undertaker[750]", "Undertaker[1000]", "Undertaker[1250]", "Undertaker[1500]", "Undertaker[1750]", "Undertaker[2000]", "Undertaker[2500]", "Undertaker[3000]", "Undertaker[5000]") },
            { new Vector2(32, 41), new MultipleLocation("Lvdovico[500]", "Lvdovico[1000]", "PR03", "QI01") },
            { new Vector2(32, 42), new MultipleLocation("RESCUED_CHERUB_08") },
            { new Vector2(33, 41), new MultipleLocation("RB104", "RB105", "PR11") },
            { new Vector2(34, 41), new SingleLocation("CO16") },
            // Wasteland
            { new Vector2(37, 40), new SingleLocation("RB04") },
            { new Vector2(40, 41), new SingleLocation("CO14") },
            { new Vector2(42, 41), new SingleLocation("CO36") },
            { new Vector2(43, 41), new SingleLocation("RESCUED_CHERUB_10") },
            { new Vector2(44, 43), new MultipleLocation("HE02", "RESCUED_CHERUB_38") },
            { new Vector2(47, 41), new SingleLocation("RB20") },
            { new Vector2(48, 39), new SingleLocation("QI06") },
            // Mercy Dreams
            { new Vector2(48, 31), new SingleLocation("QI38") },
            { new Vector2(48, 34), new MultipleLocation("QI58", "RB05", "RB09") },
            { new Vector2(48, 35), new SingleLocation("RB17") },
            { new Vector2(50, 32), new SingleLocation("QI48") },
            { new Vector2(50, 35), new SingleLocation("RESCUED_CHERUB_09") },
            { new Vector2(50, 39), new SingleLocation("QI32") },
            { new Vector2(51, 31), new SingleLocation("BS01") },
            { new Vector2(51, 35), new SingleLocation("CO03") },
            { new Vector2(51, 38), new SingleLocation("CO30") },
            { new Vector2(53, 30), new SingleLocation("CO38") },
            { new Vector2(53, 35), new SingleLocation("PR01") },
            { new Vector2(55, 30), new SingleLocation("CO21") },
            { new Vector2(56, 30), new MultipleLocation("RESCUED_CHERUB_33", "RB26") },
            // Cistern
            { new Vector2(31, 30), new MultipleLocation("Sword[D01Z05S24]", "COMBO_1", "COMBO_2", "COMBO_3", "CHARGED_1", "CHARGED_2", "CHARGED_3", "RANGED_1", "RANGED_2", "RANGED_3", "LUNGE_1", "LUNGE_2", "LUNGE_3", "VERTICAL_1", "VERTICAL_2", "VERTICAL_3") },
            { new Vector2(32, 30), new SingleLocation("QI75") },
            { new Vector2(33, 20), new SingleLocation("CO44") },
            { new Vector2(33, 31), new SingleLocation("RESCUED_CHERUB_22") },
            { new Vector2(34, 19), new SingleLocation("Lady[D01Z05S26]") },
            { new Vector2(34, 30), new SingleLocation("RB03") },
            { new Vector2(34, 37), new SingleLocation("RESCUED_CHERUB_15") },
            { new Vector2(35, 33), new SingleLocation("RESCUED_CHERUB_12") },
            { new Vector2(35, 36), new SingleLocation("Oil[D01Z05S07]") },
            { new Vector2(36, 30), new SingleLocation("CO32") },
            { new Vector2(37, 36), new SingleLocation("QI12") },
            { new Vector2(38, 36), new SingleLocation("RESCUED_CHERUB_14") },
            { new Vector2(40, 35), new SingleLocation("QI67") },
            { new Vector2(40, 39), new SingleLocation("CO09") },
            { new Vector2(42, 32), new SingleLocation("RESCUED_CHERUB_11") },
            { new Vector2(43, 31), new SingleLocation("Lady[D01Z05S22]") },
            { new Vector2(43, 38), new MultipleLocation("PR16", "RESCUED_CHERUB_13") },
            { new Vector2(44, 33), new SingleLocation("CO41") },
            { new Vector2(46, 36), new SingleLocation("QI45") },
            // Petrous
            { new Vector2(23, 40), new SingleLocation("QI101") },
            // Olive Trees
            { new Vector2(34, 47), new SingleLocation("QI07") },
            { new Vector2(38, 43), new SingleLocation("PR04") },
            { new Vector2(38, 45), new SingleLocation("QI68") },
            { new Vector2(39, 44), new SingleLocation("RESCUED_CHERUB_27") },
            { new Vector2(39, 45), new SingleLocation("QI20") },
            { new Vector2(41, 47), new SingleLocation("RESCUED_CHERUB_23") },
            { new Vector2(42, 44), new SingleLocation("CO19") },
            { new Vector2(42, 45), new MultipleLocation("QI59", "RB10") },
            { new Vector2(43, 44), new SingleLocation("CO11") },
            { new Vector2(43, 47), new SingleLocation("HE05") },
            // Graveyard
            { new Vector2(31, 49), new MultipleLocation("RB38", "QI33") },
            { new Vector2(32, 48), new SingleLocation("RESCUED_CHERUB_26") },
            { new Vector2(32, 50), new SingleLocation("QI53") },
            { new Vector2(33, 49), new MultipleLocation("QI11", "RB37", "RB02") },
            { new Vector2(33, 50), new SingleLocation("Lady[D02Z02S12]") },
            { new Vector2(33, 58), new SingleLocation("HE11") },
            { new Vector2(34, 54), new SingleLocation("RESCUED_CHERUB_25") },
            { new Vector2(34, 55), new SingleLocation("RB32") },
            { new Vector2(34, 56), new SingleLocation("CO01") },
            { new Vector2(35, 48), new SingleLocation("CO42") },
            { new Vector2(35, 49), new SingleLocation("RESCUED_CHERUB_31") },
            { new Vector2(35, 53), new SingleLocation("RESCUED_CHERUB_24") },
            { new Vector2(35, 58), new SingleLocation("RB15") },
            { new Vector2(36, 53), new SingleLocation("QI46") },
            { new Vector2(36, 54), new SingleLocation("CO29") },
            { new Vector2(36, 56), new SingleLocation("QI08") },
            { new Vector2(36, 57), new SingleLocation("Oil[D02Z02S10]") },
            { new Vector2(37, 56), new MultipleLocation("RB106", "Amanecida[D02Z02S14]") },
            // Convent
            { new Vector2(22, 63), new SingleLocation("RB107") },
            { new Vector2(23, 59), new SingleLocation("RB24") },
            { new Vector2(26, 58), new SingleLocation("HE03") },
            { new Vector2(26, 62), new SingleLocation("RB18") },
            { new Vector2(27, 60), new SingleLocation("CO15") },
            { new Vector2(27, 62), new SingleLocation("RB08") },
            { new Vector2(29, 63), new SingleLocation("BS03") },
            { new Vector2(30, 62), new SingleLocation("CO05") },
            { new Vector2(31, 63), new MultipleLocation("QI40", "QI57") },
            { new Vector2(31, 64), new SingleLocation("Lady[D02Z03S15]") },
            { new Vector2(31, 65), new SingleLocation("QI61") },
            { new Vector2(33, 63), new MultipleLocation("Sword[D02Z03S13]", "COMBO_1", "COMBO_2", "COMBO_3", "CHARGED_1", "CHARGED_2", "CHARGED_3", "RANGED_1", "RANGED_2", "RANGED_3", "LUNGE_1", "LUNGE_2", "LUNGE_3", "VERTICAL_1", "VERTICAL_2", "VERTICAL_3") },
            // Mountains
            { new Vector2(15, 36), new SingleLocation("QI63") },
            { new Vector2(18, 36), new SingleLocation("RESCUED_CHERUB_16") },
            { new Vector2(19, 36), new MultipleLocation("QI47", "Amanecida[D03Z01S03]") },
            { new Vector2(21, 36), new SingleLocation("RB22") },
            { new Vector2(23, 37), new MultipleLocation("RB13", "QI14") },
            { new Vector2(32, 37), new SingleLocation("CO13") },
            // Jondo
            { new Vector2(14, 33), new SingleLocation("QI52") },
            { new Vector2(15, 34), new SingleLocation("RB28") },
            { new Vector2(16, 35), new SingleLocation("RESCUED_CHERUB_17") },
            { new Vector2(17, 29), new SingleLocation("QI41") },
            { new Vector2(18, 28), new SingleLocation("CO07") },
            { new Vector2(21, 28), new SingleLocation("QI19") },
            { new Vector2(21, 29), new SingleLocation("CO33") },
            { new Vector2(21, 34), new SingleLocation("CO08") },
            { new Vector2(21, 35), new SingleLocation("PR10") },
            { new Vector2(22, 32), new SingleLocation("RESCUED_CHERUB_18") },
            { new Vector2(25, 32), new SingleLocation("RESCUED_CHERUB_37") },
            { new Vector2(26, 32), new SingleLocation("HE06") },
            { new Vector2(27, 32), new SingleLocation("QI103") },
            // Grievance
            { new Vector2(21, 22), new SingleLocation("QI44") },
            { new Vector2(22, 16), new MultipleLocation("QI13", "RB06") },
            { new Vector2(22, 25), new SingleLocation("QI34") },
            { new Vector2(24, 19), new SingleLocation("Oil[D03Z03S13]") },
            { new Vector2(25, 17), new SingleLocation("RESCUED_CHERUB_20") },
            { new Vector2(25, 23), new SingleLocation("CO12") },
            { new Vector2(25, 26), new MultipleLocation("RE07", "RESCUED_CHERUB_19") },
            { new Vector2(29, 19), new SingleLocation("BS04") },
            { new Vector2(29, 22), new MultipleLocation("QI10", "RESCUED_CHERUB_21") },
            { new Vector2(31, 19), new SingleLocation("QI39") },
            // Patio
            { new Vector2(57, 42), new SingleLocation("RESCUED_CHERUB_35") },
            { new Vector2(58, 43), new SingleLocation("CO23") },
            { new Vector2(58, 47), new SingleLocation("QI102") },
            { new Vector2(61, 42), new SingleLocation("RB14") },
            { new Vector2(62, 42), new SingleLocation("QI37") },
            { new Vector2(62, 43), new SingleLocation("CO39") },
            { new Vector2(64, 43), new SingleLocation("RESCUED_CHERUB_28") },
            { new Vector2(65, 42), new SingleLocation("RB21") },
            { new Vector2(66, 42), new SingleLocation("Amanecida[D04Z01S04]") },
            // Mother of Mothers
            { new Vector2(67, 43), new MultipleLocation("RE402", "RESCUED_CHERUB_30") },
            { new Vector2(67, 44), new SingleLocation("CO17") },
            { new Vector2(68, 44), new SingleLocation("QI79") },
            { new Vector2(68, 46), new SingleLocation("QI60") },
            { new Vector2(70, 46), new SingleLocation("BS05") },
            { new Vector2(73, 44), new SingleLocation("CO20") },
            { new Vector2(73, 46), new SingleLocation("RESCUED_CHERUB_29") },
            { new Vector2(73, 47), new MultipleLocation("Sword[D04Z02S12]", "COMBO_1", "COMBO_2", "COMBO_3", "CHARGED_1", "CHARGED_2", "CHARGED_3", "RANGED_1", "RANGED_2", "RANGED_3", "LUNGE_1", "LUNGE_2", "LUNGE_3", "VERTICAL_1", "VERTICAL_2", "VERTICAL_3") },
            { new Vector2(74, 41), new SingleLocation("Oil[D04Z02S14]") },
            { new Vector2(75, 44), new SingleLocation("CO34") },
            { new Vector2(80, 45), new SingleLocation("RB33") },
            { new Vector2(80, 50), new SingleLocation("HE01") },
            { new Vector2(81, 44), new SingleLocation("CO35") },
            { new Vector2(83, 46), new MultipleLocation("RE03", "QI54") },
            // Knot of Words
            { new Vector2(73, 41), new SingleLocation("HE201") },
            // All the Tears
            { new Vector2(85, 42), new SingleLocation("PR201") },
            // Library
            { new Vector2(59, 31), new SingleLocation("Oil[D05Z01S19]") },
            { new Vector2(60, 32), new SingleLocation("RESCUED_CHERUB_02") },
            { new Vector2(60, 33), new SingleLocation("RB30") },
            { new Vector2(60, 35), new MultipleLocation("RB203", "CO28") },
            { new Vector2(60, 38), new SingleLocation("RB31") },
            { new Vector2(62, 35), new SingleLocation("PR07") },
            { new Vector2(64, 36), new SingleLocation("Lady[D05Z01S14]") },
            { new Vector2(65, 34), new SingleLocation("PR15") },
            { new Vector2(68, 37), new SingleLocation("QI50") },
            { new Vector2(68, 41), new SingleLocation("CO22") },
            { new Vector2(69, 39), new SingleLocation("CO18") },
            { new Vector2(69, 41), new SingleLocation("QI80") },
            { new Vector2(70, 40), new SingleLocation("RESCUED_CHERUB_01") },
            { new Vector2(71, 39), new MultipleLocation("Sword[D05Z01S13]", "COMBO_1", "COMBO_2", "COMBO_3", "CHARGED_1", "CHARGED_2", "CHARGED_3", "RANGED_1", "RANGED_2", "RANGED_3", "LUNGE_1", "LUNGE_2", "LUNGE_3", "VERTICAL_1", "VERTICAL_2", "VERTICAL_3") },
            { new Vector2(71, 40), new SingleLocation("RB301") },
            { new Vector2(72, 34), new SingleLocation("RESCUED_CHERUB_32") },
            { new Vector2(73, 39), new SingleLocation("QI62") },
            { new Vector2(74, 39), new SingleLocation("RB19") },
            // Canvases
            { new Vector2(57, 31), new SingleLocation("QI104") },
            { new Vector2(60, 30), new MultipleLocation("RB12", "QI49", "QI71") },
            { new Vector2(64, 30), new SingleLocation("QI64") },
            { new Vector2(66, 30), new SingleLocation("HE07") },
            { new Vector2(66, 32), new MultipleLocation("RE05", "PR05") },
            { new Vector2(71, 32), new SingleLocation("BS06") },
            { new Vector2(72, 31), new SingleLocation("CO31") },
            // Rooftops
            { new Vector2(72, 51), new SingleLocation("CO06") },
            { new Vector2(72, 52), new SingleLocation("RESCUED_CHERUB_36") },
            { new Vector2(72, 55), new SingleLocation("PR12") },
            { new Vector2(74, 48), new SingleLocation("HE04") },
            { new Vector2(76, 49), new SingleLocation("QI02") },
            { new Vector2(76, 52), new SingleLocation("QI03") },
            { new Vector2(77, 59), new SingleLocation("BS16") },
            { new Vector2(78, 51), new SingleLocation("Lady[D06Z01S24]") },
            { new Vector2(78, 56), new SingleLocation("QI04") },
            { new Vector2(79, 54), new SingleLocation("CO40") },
            { new Vector2(80, 56), new MultipleLocation("Sword[D06Z01S11]", "COMBO_1", "COMBO_2", "COMBO_3", "CHARGED_1", "CHARGED_2", "CHARGED_3", "RANGED_1", "RANGED_2", "RANGED_3", "LUNGE_1", "LUNGE_2", "LUNGE_3", "VERTICAL_1", "VERTICAL_2", "VERTICAL_3") },
            // Deambulatory
            { new Vector2(80, 59), new SingleLocation("PR08") },
            // Bridge
            { new Vector2(52, 41), new MultipleLocation("BS12", "PR09") },
            { new Vector2(54, 44), new SingleLocation("HE101") },
            // Hall
            { new Vector2(55, 47), new SingleLocation("LaudesBossTrigger[30000]") },
            { new Vector2(56, 44), new SingleLocation("QI105") },
            // Wall
            { new Vector2(55, 51), new SingleLocation("QI81") },
            { new Vector2(57, 50), new SingleLocation("BS14") },
            { new Vector2(57, 51), new SingleLocation("QI72") },
            { new Vector2(58, 49), new SingleLocation("Oil[D09Z01S12]") },
            { new Vector2(58, 51), new SingleLocation("RESCUED_CHERUB_34") },
            { new Vector2(59, 48), new SingleLocation("CO02") },
            { new Vector2(59, 50), new SingleLocation("CO26") },
            { new Vector2(60, 49), new SingleLocation("RB16") },
            { new Vector2(60, 52), new SingleLocation("RESCUED_CHERUB_05") },
            { new Vector2(62, 48), new MultipleLocation("CO27", "RESCUED_CHERUB_04") },
            { new Vector2(62, 49), new SingleLocation("QI70") },
            { new Vector2(62, 50), new SingleLocation("QI51") },
            { new Vector2(62, 51), new SingleLocation("RESCUED_CHERUB_03") },
            { new Vector2(63, 50), new MultipleLocation("CO10", "QI69") },
            { new Vector2(63, 51), new SingleLocation("CO24") },
            { new Vector2(64, 49), new SingleLocation("CO37") },
            { new Vector2(64, 50), new SingleLocation("RB11") },
            { new Vector2(66, 52), new SingleLocation("Amanecida[D09Z01S01]") },
            // Brotherhood
            { new Vector2(03, 44), new MultipleLocation("QI204", "QI301") },
            { new Vector2(05, 43), new SingleLocation("RESCUED_CHERUB_06") },
            { new Vector2(05, 44), new SingleLocation("PR203") },
            { new Vector2(07, 43), new SingleLocation("RB204") },
            { new Vector2(11, 37), new MultipleLocation("Sword[D17Z01S08]", "COMBO_1", "COMBO_2", "COMBO_3", "CHARGED_1", "CHARGED_2", "CHARGED_3", "RANGED_1", "RANGED_2", "RANGED_3", "LUNGE_1", "LUNGE_2", "LUNGE_3", "VERTICAL_1", "VERTICAL_2", "VERTICAL_3") },
            { new Vector2(11, 40), new SingleLocation("QI35") },
            { new Vector2(12, 40), new MultipleLocation("RE401", "CO25") },
            { new Vector2(13, 40), new SingleLocation("RB25") },
            { new Vector2(15, 41), new SingleLocation("BS13") },
            { new Vector2(16, 41), new SingleLocation("RE01") },
            // Echoes
            { new Vector2(30, 26), new SingleLocation("RB202") },
            { new Vector2(31, 33), new SingleLocation("RB108") },
            // Mourning
            { new Vector2(43, 25), new SingleLocation("PR202") },
            { new Vector2(61, 27), new MultipleLocation("BossTrigger[5000]", "QI202") },
            { new Vector2(81, 27), new SingleLocation("RB201") },
            // Resting Place
            { new Vector2(41, 30), new SingleLocation("QI203") },
        };
}
