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

    private readonly Dictionary<Vector2, IMapLocation> mapLocations = new();

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
        LoadLocationInfo();
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
    /// Loads the list of location info from json and assigns single/multiple type
    /// </summary>
    private void LoadLocationInfo()
    {
        if (!FileHandler.LoadDataAsJson("locations.json", out LocationInfo[] list))
            return;

        foreach (var info in list)
        {
            Vector2 location = new(info.X, info.Y);
            if (info.Locations.Length == 1)
                mapLocations.Add(location, new SingleLocation(info.Locations[0]));
            else
                mapLocations.Add(location, new MultipleLocation(info.Locations));
        }
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
                    ModLog.Error(cellPosition + " is not a cell that contains locations!");
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
        ModLog.Info("Creating marks holder");
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
        ModLog.Info("Creating location text");
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
        ModLog.Info("Calculating current inventory");
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

        DoorLocation startingDoor = allDoorLocations[settings.RealStartingLocation.Door];
        roomObjects["Initial"].AddRange(roomObjects[startingDoor.Room]); // Starting room is visible
        roomObjects["D02Z02S11"].AddRange(roomObjects["D01Z02S03"]); // Albero elevator room is also visible after graveyard elevator
        foreach (string obj in roomObjects["Initial"])
        {
            if (allDoorLocations.TryGetValue(obj, out DoorLocation door) && door.Direction != 5)
                unreachableDoors.Add(door); // Maybe instead check visibility flags
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
                    if (exitDoor == null)
                        exitDoor = allDoorLocations[enterDoor.OriginalDoor];

                    checkedDoors.Add(enterDoor);
                    checkedDoors.Add(exitDoor);
                    inventory.AddItem(enterDoor.Id);
                    inventory.AddItem(exitDoor.Id);
                    unreachableDoors.Remove(enterDoor);
                    unreachableDoors.Remove(exitDoor);

                    string newRoom = exitDoor.Room;
                    foreach (string obj in roomObjects[newRoom])
                    {
                        // If this door hasn't already been processed, make it visible
                        if (allDoorLocations.TryGetValue(obj, out DoorLocation newDoor) && newDoor.ShouldBeMadeVisible(settings, inventory))
                            currentDoors.Push(newDoor);
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
}
