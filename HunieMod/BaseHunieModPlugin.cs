using System;
using System.IO;
using System.Linq;
using BepInEx;
using UnityEngine;
using HarmonyLib;
using BepInEx.Configuration;
using System.Collections.Generic;

namespace HunieMod
{
    /// <summary>
    /// The base plugin type that adds HuniePop-specific functionality over the default <see cref="BaseUnityPlugin"/>.
    /// </summary>
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    public class BaseHunieModPlugin : BaseUnityPlugin
    {
        public static Dictionary<string, int> ItemList = new Dictionary<string, int>() {
            { "Coffee", 1 },
{"Orange Juice", 2},
{"Bagel", 3},
{"Muffin", 4},
{"Omelette", 5},
{"Pancakes", 6},
{"Cookies", 7},
{"Cupcakes", 8},
{"Sundae", 9},
{"Pumpkin Pie", 10},
{"Fruit Tart Pie", 11},
{"Wedding Cake", 12},
{"Orange", 13},
{"Lemon", 14},
{"Mango", 15},
{"Pinapple", 16},
{"Coconut", 17},
{"Watermelon", 18},
{"Carrot", 19},
{"Cucumber", 20},
{"Tomatos", 21},
{"Bell Peppers", 22},
{"Eggplant", 23},
{"Cabbage", 24},
{"Heart Candies", 25},
{"Jelly Beans", 26},
{"Bubble Gum", 27},
{"Lollipop", 28},
{"Cotton Candy", 29},
{"Chocolate", 30},
{"Soda", 31},
{"Popcorn", 32},
{"French Fries", 33},
{"Corndog", 34},
{"Hamburger", 35},
{"Pizza", 36},
{"Beer", 37},
{"Sake", 38},
{"Wine", 39},
{"Champagne", 40},
{"Pina Colada", 41},
{"Daiquiri", 42},
{"Mojito", 43},
{"Lime Margarita", 44},
{"Martini", 45},
{"Cocktail", 46},
{"Lemon Drop", 47},
{"Whisky", 48},
{"Decorative Pens", 49},
{"Glossy Notebook", 50},
{"Graduation Cap", 51},
{"Textbooks", 52},
{"Girly Backpack", 53},
{"Laptop Pro", 54},
{"Old Fashioned Yoyo", 55},
{"Puzzle Cube", 56},
{"Sudoku Books", 57},
{"Dart Board", 58},
{"Board Game", 59},
{"Chess Set", 60},
{"Water Bottle", 61},
{"Cardio Weights", 62},
{"Skipping Rope", 63},
{"Kettle Bell", 64},
{"Boxing Gloves", 65},
{"Punching Bag", 66},
{"Baby Binky", 67},
{"Bead Bracelet", 68},
{"Glow Sticks", 69},
{"Rainbow Wig", 70},
{"Fuzzy Boots", 71},
{"Fairy Wings", 72},
{"Tennis Balls", 73},
{"Tennis Racket", 74},
{"Flying Disc", 75},
{"Basket Ball", 76},
{"Volley Ball", 77},
{"Soccer Ball", 78},
{"Sketching Pencils", 79},
{"Paint Brushes", 80},
{"Drawing Mannequin", 81},
{"Sketch Pad", 82},
{"Paint Palette", 83},
{"Canvas & Easel", 84},
{"Baking Utensils", 85},
{"Measuring Cup", 86},
{"Rolling Pin", 87},
{"Oven Timer", 88},
{"Mixing Bowl", 89},
{"Oven Mitts", 90},
{"Yoga Belt", 91},
{"Yoga Blocks", 92},
{"Yoga Bag", 93},
{"Yoga Mat", 94},
{"Yoga Ball", 95},
{"Yoga Outfit", 96},
{"Tango Rose", 97},
{"Sweatbands", 98},
{"Leg Warmers", 99},
{"Dancing Fan", 100},
{"Pink Tutu", 101},
{"Stripper Pole", 102},
{"Synthetic Seaweed", 103},
{"Synthetic Coral", 104},
{"Tank Gravel", 105},
{"Bag of Goldfish", 106},
{"Fishy Castle", 107},
{"Fish Tank", 108},
{"Swimmers Cap", 109},
{"Goggles", 110},
{"Snorkel", 111},
{"Flippers", 112},
{"Lifesaver", 113},
{"Diving Tank", 114},
{"Flower Seeds", 115},
{"Garden Shovel", 116},
{"Flower Pots", 117},
{"Watering Can", 118},
{"Garden Gnome", 119},
{"Wooden Birdhouse", 120},
{"Hoop Earrings", 121},
{"Gold Earrings", 122},
{"Heart Necklace", 123},
{"Pearl Necklace", 124},
{"Silver Ring", 125},
{"Lovely Ring", 126},
{"Nail Polish", 127},
{"Shiny Lipstick", 128},
{"Hair Brush", 129},
{"Makeup Kit", 130},
{"Eyelash Curler", 131},
{"Compact Mirror", 132},
{"Peep Toe Heels", 133},
{"Cork Wedge Sandals", 134},
{"Vintage Platforms", 135},
{"Leopard Print Pumps", 136},
{"Pink Mary Janes", 137},
{"Suede Ankle Booties", 138},
{"Blue Orchid", 139},
{"White Pansy", 140},
{"Orange Cosmos", 141},
{"Red Tulip", 142},
{"Pink Lily", 143},
{"Sunflower", 144},
{"Stuffed Bear", 145},
{"Stuffed Cat", 146},
{"Stuffed Sheep", 147},
{"Stuffed Monkey", 148},
{"Stuffed Penguin", 149},
{"Stuffed Whale", 150},
{"Sea Breeze Perfume", 151},
{"Green Tea Perfume", 152},
{"Peach Perfume", 153},
{"Cinnamon Perfume", 154},
{"Rose Perfume", 155},
{"Lilac Perfume", 156},
{"Flute", 157},
{"Drums", 158},
{"Trumpet", 159},
{"Banjo", 160},
{"Violin", 161},
{"Keyboard", 162},
{"Sun Lotion", 163},
{"Stylish Shades", 164},
{"Flip Flops", 165},
{"Beach Ball", 166},
{"Big Beach Towel", 167},
{"Surfboard", 168},
{"Buttery Croissant", 169},
{"Fresh Baguette", 170},
{"Fancy Cheese", 171},
{"French Beret", 172},
{"Accordion", 173},
{"Wine Bottle", 174},
{"Hot Wax Candles", 175},
{"Silk Blindfold", 176},
{"Spiked Choker", 177},
{"Fuzzy Handcuffs", 178},
{"Leather Whip", 179},
{"Ball Gag", 180},
{"Ear Muffs", 181},
{"Warm Mittens", 182},
{"Snow Globe", 183},
{"Ice Skates", 184},
{"Snow Sled", 185},
{"Snowboard", 186},
{"Bandages", 187},
{"Stethoscope", 188},
{"Medical Clipboard", 189},
{"Medicine Bottle", 190},
{"First-Aid Kit", 191},
{"Nurse Uniform", 192},
{"Double Hair Bow", 193},
{"Glitter Bottles", 194},
{"Twirly Baton", 195},
{"Megaphone", 196},
{"Pom-poms", 197},
{"Cheerleading Uniform", 198},
{"Chopsticks", 199},
{"Riceballs", 200},
{"Bonsai Tree", 201},
{"Wooden Sandals", 202},
{"Kimono", 203},
{"Samurai Helmet", 204},
{"Maracas", 205},
{"Sombrero", 206},
{"Poncho", 207},
{"Luchador Mask", 208},
{"Pinata", 209},
{"Vinuela", 210},
{"Cigarette Pack", 211},
{"Lighter", 212},
{"Glass Pipe", 213},
{"Glass Bong", 214},
{"Blotter Tabs", 215},
{"Happy Pills", 216},
{"Wing Pin", 217},
{"Compass", 218},
{"Pilot's Cap", 219},
{"Travel Suitcase", 220},
{"Rolling Luggage", 221},
{"High Def Camera", 222},
{"Retro Controller", 223},
{"Arcade Joystick", 224},
{"Zappy Gun", 225},
{"Gamer Glove", 226},
{"Handheld Game", 227},
{"Arcade Cabinet", 228},
{"Mistletoe", 229},
{"Gingerbread Man", 230},
{"Round Ornament", 231},
{"Ribbon Wreath", 232},
{"Fuzzy Stocking", 233},
{"Jolly Old Cap", 234},
{"Acorns", 235},
{"Maple Leaf", 236},
{"Pinecone", 237},
{"Mushrooms", 238},
{"Seashell", 239},
{"Four Leaf Clover", 240},
{"Endurance Ring", 241},
{"Pocket Vibe", 242},
{"Fairy's Tail", 243},
{"Bliss Beads", 244},
{"Magic Wand", 245},
{"Royal Scepter", 246},
{"Ball of Yarn", 247},
{"Lattice Ball", 248},
{"Squeaky Mouse", 249},
{"Feather Pole", 250},
{"Laser Pointer", 251},
{"Scratch Post", 252},
{"Model Rocket", 253},
{"Miniature UFO", 254},
{"Armillary Sphere", 255},
{"Telescope", 256},
{"Space Helmet", 257},
{"Moonrock", 258},
{"Sapphire", 259},
{"Ruby", 260},
{"Emerald", 261},
{"Topaz", 262},
{"Amethyst", 263},
{"Diamond", 264},
{"Snake Flute", 265},
{"Jeweled Turban", 266},
{"Feather Fan", 267},
{"Scarab Pendant", 268},
{"Antique Vase", 269},
{"Golden Cat Statue", 270},
{"Poker Chips", 271},
{"Lucky Dice", 272},
{"Playing Cards", 273},
{"Dealer's Cap", 274},
{"Roulette Wheel", 275},
{"Slot Machine", 276},
{"Tiffany's Panties", 277},
{"Aiko's Panties", 278},
{"Kyanna's Panties", 279},
{"Audrey's Panties", 280},
{"Lola's Panties", 281},
{"Nikki's Panties", 282},
{"Jessie's Panties", 283},
{"Beli's Panties", 284},
{"Kyu's Panties", 285},
{"Momo's Panties", 286},
{"Celeste's Panties", 287},
{"Venus' Panties", 288},
{"Tissue Box", 289},
{"Dirty Magazine", 290},
{"Weird Thing", 291},
{"Love Potion", 292},
{"Shiny Gift Box", 293},
{"Fine Gift Box", 294},
{"Fancy Gift Box", 295},
{"Regal Gift Box", 296},
{"Lovely Gift Box", 297},
{"Lucky Gift Box", 298},
{"Kyu Plushie", 299}};

        public static ConfigEntry<KeyboardShortcut> ResetKey { get; private set; }
        public static ConfigEntry<KeyboardShortcut> ResetKey2 { get; private set; }
        public static ConfigEntry<Boolean> CensorshipEnabled { get; private set; }
        public static ConfigEntry<Boolean> ReturnToMenuEnabled { get; private set; }
        public static ConfigEntry<Boolean> CheatHotkeyEnabled { get; private set; }
        public static ConfigEntry<Boolean> MouseWheelEnabled { get; private set; }
        //public static ConfigEntry<Boolean> SaveOnReturn { get; private set; }

        public static bool hasReturned = false;
        public static bool cheatsEnabled = false;
        public static bool savingDisabled = false;

        public const int AXISES = 13;

        public static int testint = 0;

        private void Awake()
        {
            CensorshipEnabled = Config.Bind(
                "Settings", nameof(CensorshipEnabled),
                true,
                "Enable or disable the censorship mods");
            ReturnToMenuEnabled = Config.Bind(
                "Settings", nameof(ReturnToMenuEnabled),
                true,
                "Enable or disable the return to main menu feature");
            CheatHotkeyEnabled = Config.Bind(
                "Settings", nameof(CheatHotkeyEnabled),
                true,
                "Enable or disable the cheat hotkey (C on main menu)");
            MouseWheelEnabled = Config.Bind(
                "Settings", nameof(MouseWheelEnabled),
                true,
                "Enable or disable the mouse wheel being treated as a click");

            ResetKey = Config.Bind(
                "Settings", nameof(ResetKey),
                new KeyboardShortcut(KeyCode.F4),
                "The key to use for going back to the title or quitting the game");
            ResetKey2 = Config.Bind(
                "Settings", nameof(ResetKey2),
                new KeyboardShortcut(KeyCode.Escape),
                "Alternate key to use");

        }

        void Start()
        {
            // Logger is part of BaseUnityPlugin and will be available in this instance
            //Logger.LogDebug(Input.GetJoystickNames()[0]);

            if (CensorshipEnabled.Value)
            {
                Harmony.CreateAndPatchAll(typeof(CensorshipPatches), null);
            }
            Harmony.CreateAndPatchAll(typeof(SpeedrunPatches), null);
        }

        int Version()
        {
            if (GameManager.Data.Items.Get(299) == null)
            {
                return 0;
            }
            else return 1;
        }

        void PlayDialogAudio(int i, int j)
        {
            GameManager.System.Audio.Play(AudioCategory.VOICE, HunieMod.Definitions.DialogScenes[i].steps[j].sceneLine.dialogLine.audioDefinition, false, 1f, false);
        }
        void PlayDialogAudio(int i, int j, int k, int l)
        {
            GameManager.System.Audio.Play(AudioCategory.VOICE, HunieMod.Definitions.DialogTriggers[i].lineSets[j].lines[k].dialogLine[l].audioDefinition, false, 1f, false);
        }

        void PlayCheatLine(int line = -1)
        {
            int version = Version(); //0 = jan23, 1 = valentines
            int r;
            if (line == -1)
            {
                System.Random rand = new System.Random();
                r = rand.Next(12);
            }
            else r = line;
            switch (r)
            {
                case 0: //Tiffany: Have you ever cheated? Be honest
                    if (version == 0) PlayDialogAudio(208, 1); else PlayDialogAudio(209, 1);
                    break;
                case 1: //Nikki: Dude, seriously right now?
                    if (version == 0) PlayDialogAudio(10, 1, 0, 2); else PlayDialogAudio(11, 1, 0, 2);
                    break;
                case 2: //Audrey: Ugh, gaaay!
                    if (version == 0) PlayDialogAudio(15, 5, 0, 3); else PlayDialogAudio(16, 5, 0, 3);
                    break;
                case 3: //Jessie: Really now, I didn't take you for one of those types
                    if (version == 0) PlayDialogAudio(15, 5, 0, 6); else PlayDialogAudio(16, 5, 0, 6);
                    break;
                case 4: //Celeste: Typical earthling
                    if (version == 0) PlayDialogAudio(17, 0, 2, 10); else PlayDialogAudio(18, 0, 2, 10);
                    break;
                case 5: //Kyu: Really? I know I taught you better than that
                    PlayDialogAudio(2, 1, 1, 8);
                    break;
                case 6: //Beli: That's terrible!
                    if (version == 0) PlayDialogAudio(15, 2, 0, 7); else PlayDialogAudio(16, 2, 0, 7);
                    break;
                case 7: //Venus: You can't possibly be serious
                    PlayDialogAudio(5, 3, 1, 11);
                    break;
                case 8: //Momo: Master, why?
                    if (version == 0) PlayDialogAudio(10, 1, 1, 9); else PlayDialogAudio(11, 1, 1, 9);
                    break;
                case 9: //Lola: What's the matter with you?
                    if (version == 0) PlayDialogAudio(10, 1, 2, 4); else PlayDialogAudio(11, 1, 2, 4);
                    break;
                case 10: //Aiko: Could you please not do that
                    if (version == 0) PlayDialogAudio(10, 1, 1, 1); else PlayDialogAudio(11, 1, 1, 1);
                    break;
                case 11: //Kyanna: Professional help
                    PlayDialogAudio(4, 2, 1, 2);
                    break;
                default:
                    break;
            }
        }

        private void Update() // Another Unity method
        {
            //Logger.LogDebug(Input.GetAxis("Mouse ScrollWheel"));
            /*string axises = "";
            for (int i = 0; i <= AXISES; i++)
            {
                axises += Input.GetAxis("Axis " + i) + ",";
            }
            axises += Input.GetAxis("Mouse ScrollWheel");
            Logger.LogDebug(axises);*/

            if (GameManager.System.GameState == GameState.TITLE)
            {

                if (CheatHotkeyEnabled.Value && cheatsEnabled == false && Input.GetKeyDown(KeyCode.C))
                {
                    GameManager.System.Audio.Play(AudioCategory.SOUND, GameManager.Stage.uiPuzzle.puzzleGrid.failureSound, false, 2f, false);
                    GameManager.System.Audio.Play(AudioCategory.SOUND, GameManager.Stage.uiPuzzle.puzzleGrid.badMoveSound, false, 2f, false);
                    PlayCheatLine();
                    Harmony.CreateAndPatchAll(typeof(CheatPatches), null);
                    cheatsEnabled = true;
                }
            }
            if (cheatsEnabled)
            {
                if (Input.GetKeyDown(KeyCode.F1))
                {
                    if (GameManager.System.GameState == GameState.PUZZLE)
                    {
                        if (GameManager.System.Puzzle.Game.puzzleGameState == PuzzleGameState.WAITING)
                        {
                            GameManager.System.Puzzle.Game.SetResourceValue(PuzzleGameResourceType.AFFECTION, 999999, false);
                            GameUtil.ShowNotification(CellNotificationType.MESSAGE, "Puzzle cleared!");
                        }
                    }
                    else if (GameManager.System.GameState == GameState.SIM)
                    {
                        CheatPatches.AddGreatGiftsToInventory();
                        GameManager.System.Player.money = 69420;
                        GameManager.System.Player.hunie = 69420;
                    }
                }

                if (Input.GetKeyDown(KeyCode.F2))
                {
                    if (savingDisabled)
                    {
                        savingDisabled = false;
                        GameUtil.ShowNotification(CellNotificationType.MESSAGE, "Saving has been enabled");
                    }
                    else
                    {
                        savingDisabled = true;
                        GameUtil.ShowNotification(CellNotificationType.MESSAGE, "Saving has been disabled");
                    }
                }

                //i've made this the "output things of my choosing" function
                if (Input.GetKeyDown(KeyCode.N))
                {
                    /*string bigTable = "";
                    foreach (GirlDefinition g in HunieMod.Definitions.Girls)
                    {
                        bigTable += g.firstName;
                        if (g.drinksAnytime) bigTable += " (D)";
                        bigTable += ":\n";
                        for (int i = 0; i < g.schedule.Length; i++)
                        {
                            for (int j = 0; j < g.schedule[i].daytimes.Length; j++)
                            {
                                LocationDefinition loc = g.schedule[i].daytimes[j].location;
                                string locname = "";
                                if (loc == null)
                                {
                                    if (g.leavesTown) locname = "OUT OF TOWN";
                                    else locname = "ASLEEP";
                                }
                                else locname = loc.fullName;
                                if (!g.drinksAnytime && (locname == "Lusties Nightclub" || locname == "Bar & Lounge")) locname += " (D)";
                                string formattedPhase = "";
                                switch(j)
                                {
                                    case 0: formattedPhase = "MORNING:   "; break;
                                    case 1: formattedPhase = "AFTERNOON: "; break;
                                    case 2: formattedPhase = "EVENING:   "; break;
                                    case 3: formattedPhase = "NIGHT:     "; break;
                                }
                                bigTable += (GameClockWeekday)i + " " + formattedPhase + locname + "\n";
                            }
                        }
                        bigTable += "\n";
                    }
                    Logger.LogDebug(bigTable);*/
                    //PlayCheatLine(testint);
                    //testint++;
                    /*
                    string bigTable = "";
                    for (int i = 0; i < HunieMod.Definitions.DialogScenes.Count; i++)
                    {
                        for (int j = 0; j < HunieMod.Definitions.DialogScenes[i].steps.Count; j++)
                        {
                            bigTable += i + "," + j + ": " + HunieMod.Definitions.DialogScenes[i].steps[j].sceneLine.dialogLine.text + "\n";
                        }
                    }
                    Logger.LogDebug(bigTable);

                    bigTable = "";
                    for (int i = 0; i < HunieMod.Definitions.DialogTriggers.Count; i++)
                    {
                        for (int j = 0; j < HunieMod.Definitions.DialogTriggers[i].lineSets.Count; j++)
                        {
                            for (int k = 0; k < HunieMod.Definitions.DialogTriggers[i].lineSets[j].lines.Count; k++)
                            {
                                for (int l = 0; l < HunieMod.Definitions.DialogTriggers[i].lineSets[j].lines[k].dialogLine.Length; l++)
                                {
                                    bigTable += i + "," + j + "," + k + "," + l + ": " + HunieMod.Definitions.DialogTriggers[i].lineSets[j].lines[k].dialogLine[l].text + "\n";
                                }
                            }
                            
                        }
                    }
                    Logger.LogDebug(bigTable);*/

                    /*
                    Logger.LogDebug("test");
                    string bigTable = "";
                    for (int i = 0; i < 10000; i++)
                    {
                        ItemDefinition test = GameManager.Data.Items.Get(i);
                        if (test != null) bigTable += "{\"" + GameManager.Data.Items.Get(i).name + "\", " + i + "},\n";
                    }
                    Logger.LogDebug(bigTable);*/
                }
            }
            if (ReturnToMenuEnabled.Value)
            {
                if (ResetKey.Value.IsDown() || ResetKey2.Value.IsDown())
                {
                    if (GameManager.System.GameState == GameState.TITLE)
                    {
                        //GameUtil.QuitGame();
                    }
                    else
                    {
                        if (GameUtil.EndGameSession(false, false, false)) hasReturned = true; // Back to Titlescreen
                    }
                }
            }
        }
        /// <summary>
        /// The identifier of this plugin.
        /// </summary>
        public const string PluginGUID = "com.lounger.huniemod";

        /// <summary>
        /// The name of this plugin.
        /// </summary>
        public const string PluginName = "HunieMod (Speedrun Version)";

        /// <summary>
        /// The version of this plugin.
        /// </summary>
        public const string PluginVersion = "1.2";

        /// <summary>
        /// The directory where this plugin resides.
        /// </summary>
        public static readonly string PluginBaseDir = Path.GetDirectoryName(typeof(BaseHunieModPlugin).Assembly.Location);

        #region Game core

        /// <summary>
        /// General game manager containing all other managers and general game settings.
        /// </summary>
        public static GameManager Game => GameManager.System;

        /// <summary>
        /// The game's main camera.
        /// </summary>
        public static Camera MainCam => GameManager.System.gameCamera.mainCamera;

        /// <summary>
        /// The root container on which all visible elements are placed.
        /// </summary>
        public static Stage GameStage => GameManager.Stage;

        /// <summary>
        /// Manages location traveling, arrivals and departures.
        /// </summary>
        public static LocationManager Location => Game.Location;

        /// <summary>
        /// The main player's settings, stats and active game variables.
        /// </summary>
        public static PlayerManager Player => Game.Player;

        /// <summary>
        /// Manages going to puzzle locations and puzzle game logic, whereas <see cref="PuzzleManager.Game"/>
        /// manages the more visual aspects of the puzzle game.
        /// </summary>
        public static PuzzleManager Puzzle => Game.Puzzle;

        #endregion

        #region Locations

        /// <summary>
        /// The definition of the location that is currently active.
        /// </summary>
        public static LocationDefinition CurrentLocationDef => Location?.currentLocation;

        /// <summary>
        /// The ID of the location that is currently active.
        /// </summary>
        public static LocationId? CurrentLocation => (LocationId?)CurrentLocationDef?.id;

        #endregion

        #region Girls

        /// <summary>
        /// The definition of the girl that is currently active.
        /// </summary>
        public static GirlDefinition CurrentGirlDef => Location?.currentGirl;

        /// <summary>
        /// The visual object of the main girl currently on the stage.
        /// </summary>
        public static Girl CurrentStageGirlObject => GameStage.girl;

        /// <summary>
        /// The visual object of the alt. girl currently on the stage.
        /// </summary>
        public static Girl CurrentStageAltGirlObject => GameStage.altGirl;

        /// <summary>
        /// The ID of the girl that is currently active.
        /// </summary>
        public static GirlId? CurrentGirl => (GirlId?)CurrentGirlDef?.id;

        /// <summary>
        /// The ID of the main girl currently on the stage.
        /// </summary>
        public static GirlId? CurrentStageGirl => (GirlId?)CurrentStageGirlObject?.definition.id;

        /// <summary>
        /// The ID of the alt. girl currently on the stage.
        /// </summary>
        public static GirlId? CurrentStageAltGirl => (GirlId?)CurrentStageAltGirlObject?.definition.id;

        #endregion

        #region Events

        private static EventManager events;

        /// <summary>
        /// Event helper that wraps certain key game events.
        /// </summary>
        protected static EventManager Events => events = events ?? new EventManager();

        /// <summary>
        /// Event helper that wraps certain key game events.
        /// </summary>
        protected class EventManager
        {
            /// <summary>
            /// Fires after <see cref="GameManager.Pause"/> has frozen all game elements but the cellphone.
            /// </summary>
            public event GameManager.GameManagerDelegate GamePause
            {
                add { Game.GamePauseEvent += value; }
                remove { Game.GamePauseEvent -= value; }
            }

            /// <summary>
            /// Fires after <see cref="GameManager.Unpause"/> has unfrozen all game elements.
            /// </summary>
            public event GameManager.GameManagerDelegate GameUnpause
            {
                add { Game.GameUnpauseEvent += value; }
                remove { Game.GameUnpauseEvent -= value; }
            }

            /// <summary>
            /// Fires after LocationManager.OnLocationArrival() has initialized the arrival sequence and before the location is "settled".
            /// </summary>
            public event LocationManager.LocationDelegate LocationArrive
            {
                add { Location.LocationArriveEvent += value; }
                remove { Location.LocationArriveEvent -= value; }
            }

            /// <summary>
            /// Fires after LocationManager.OnLocationDeparture() has set up the new location and transition screen.
            /// Shortly before LocationManager.ArriveLocation() fires.
            /// </summary>
            public event LocationManager.LocationDelegate LocationDepart
            {
                add { Location.LocationDepartEvent += value; }
                remove { Location.LocationDepartEvent -= value; }
            }

            /// <summary>
            /// Fires when <see cref="Stage.OnStart"/> has setup all it's child elements.
            /// </summary>
            public event Stage.StageDelegate StageStarted
            {
                add { GameStage.StageStartedEvent += value; }
                remove { GameStage.StageStartedEvent -= value; }
            }
        }

        #endregion
    }
}
