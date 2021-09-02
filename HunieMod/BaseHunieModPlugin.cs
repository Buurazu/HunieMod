using System;
using System.IO;
using System.Linq;
using BepInEx;
using UnityEngine;
using HarmonyLib;
using BepInEx.Configuration;
using System.Collections.Generic;
using System.Net;
using System.Diagnostics;

namespace HunieMod
{
    /// <summary>
    /// The base plugin type that adds HuniePop-specific functionality over the default <see cref="BaseUnityPlugin"/>.
    /// </summary>
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    public class BaseHunieModPlugin : BaseUnityPlugin
    {
        /// <summary>
        /// The version of this plugin.
        /// </summary>
        public const string PluginVersion = "1.8";

        public static Dictionary<string, int> ItemNameList = new Dictionary<string, int>();

        public static ConfigEntry<String> MouseKeys { get; private set; }
        public static ConfigEntry<String> ControllerKeys { get; private set; }
        public static ConfigEntry<KeyboardShortcut> ResetKey { get; private set; }
        public static ConfigEntry<KeyboardShortcut> ResetKey2 { get; private set; }
        public static ConfigEntry<Boolean> CensorshipEnabled { get; private set; }
        public static ConfigEntry<Boolean> CheatHotkeyEnabled { get; private set; }
        public static ConfigEntry<Boolean> MouseWheelEnabled { get; private set; }
        public static ConfigEntry<Boolean> AxisesEnabled { get; private set; }
        public static ConfigEntry<Boolean> InputModsEnabled { get; private set; }
        public static ConfigEntry<Boolean> InGameTimer { get; private set; }
        public static ConfigEntry<int> SplitRules { get; private set; }
        public static ConfigEntry<Boolean> VsyncEnabled { get; private set; }
        public static ConfigEntry<Boolean> CapAt144 { get; private set; }
        public static ConfigEntry<Boolean> CustomCGs { get; private set; }

        //hasReturned is used to display "This is for practice purposes" after a return to main menu, until you start a new file
        public static bool hasReturned = false;
        public static bool cheatsEnabled = false;
        public static bool savingDisabled = false;

        public const int AXISES = 13;

        public const int JAN23 = 0;
        public const int VALENTINES = 1;

        public static bool newVersionAvailable = false;

        public static RunTimer run;

        public static int lastChosenCategory = 0;
        public static int lastChosenDifficulty = 0;

        private void Awake()
        {
            ResetKey = Config.Bind(
                "Settings", nameof(ResetKey),
                new KeyboardShortcut(KeyCode.F4),
                "The hotkey to use for going back to the title (set to None to disable)");
            ResetKey2 = Config.Bind(
                "Settings", nameof(ResetKey2),
                new KeyboardShortcut(KeyCode.Escape),
                "Alternate hotkey to use for going back to the title (set to None to disable)");

            InputModsEnabled = Config.Bind(
                "Settings", nameof(InputModsEnabled),
                true,
                "Enable or disable all fake clicks (overrides the below settings)");
            MouseWheelEnabled = Config.Bind(
                "Settings", nameof(MouseWheelEnabled),
                true,
                "Enable or disable the mouse wheel being treated as a click");
            AxisesEnabled = Config.Bind(
                "Settings", nameof(AxisesEnabled),
                true,
                "Enable or disable controller axises being treated as a click");
            MouseKeys = Config.Bind(
                "Settings", nameof(MouseKeys),
                "W, A, S, D, Q, E, UpArrow, DownArrow, LeftArrow, RightArrow",
                "The keys that will be treated as a click (set to None for no keyboard clicks)");
            ControllerKeys = Config.Bind(
                "Settings", nameof(ControllerKeys),
                "JoystickButton0, JoystickButton1, JoystickButton2, JoystickButton3",
                "The controller buttons that will be treated as a click (set to None for no controller clicks)");

            CensorshipEnabled = Config.Bind(
                "Settings", nameof(CensorshipEnabled),
                true,
                "Enable or disable the censorship mods");

            CustomCGs = Config.Bind(
                "Settings", nameof(CustomCGs),
                false,
                "Enable or disable loading custom CGs from the exe folder (i.e. Aiko.png) (requires the censorship enabled) (loading files could lag the game)");

            VsyncEnabled = Config.Bind(
                "Settings", nameof(VsyncEnabled),
                true,
                "Enable or disable Vsync. The FPS cap below will only take effect with it disabled");
            CapAt144 = Config.Bind(
                "Settings", nameof(CapAt144),
                true,
                "Cap the game at 144 FPS. If false, it will cap at 60 FPS instead. 144 FPS could help mash speed, but the higher framerate could mean bonus round affection drains faster (especially on Hard)");
            
            InGameTimer = Config.Bind(
                "Settings", nameof(InGameTimer),
                true,
                "Enable or disable the built-in timer (shows your time on the affection meter after each date, read the readme for more info)");
            SplitRules = Config.Bind(
                "Settings", nameof(SplitRules),
                0,
                "0 = Split on every date/bonus, 1 = Split only after dates, 2 = Split only after bonus rounds\n(You may want to delete your run comparison/golds after changing this. Get Laids are excluded from this option)");

            CheatHotkeyEnabled = Config.Bind(
                "Settings", nameof(CheatHotkeyEnabled),
                true,
                "Enable or disable the cheat hotkey (C on main menu)");

        }

        void Start()
        {
            if (!VsyncEnabled.Value)
            {
                QualitySettings.vSyncCount = 0;
                if (CapAt144.Value)
                    Application.targetFrameRate = 144;
                else
                    Application.targetFrameRate = 60;
            }

            Harmony.CreateAndPatchAll(typeof(BasePatches), null);
            //initiate the variable used for autosplitting
            BasePatches.InitSearchForMe();

            //Create the splits files for the first time if they don't exist
            if (!System.IO.Directory.Exists("splits"))
            {
                System.IO.Directory.CreateDirectory("splits");
                System.IO.Directory.CreateDirectory("splits/data");
            }
            RunTimer.ConvertOldSplits();

            //Check for a new update
            WebClient client = new WebClient();
            try
            {
                string reply = client.DownloadString("https://pastebin.com/raw/md3qeuCk");

                if (reply != PluginVersion)
                    newVersionAvailable = true;
            }
            catch (Exception e) { Logger.LogDebug("Couldn't read the update pastebin! " + e.ToString()); }

            //Create the item names dictionary for easier rewarding of specific items
            foreach (ItemDefinition item in HunieMod.Definitions.Items)
            {
                ItemNameList.Add(item.name, item.id);
            }

            if (CensorshipEnabled.Value) Harmony.CreateAndPatchAll(typeof(CensorshipPatches), null);
            if (InputModsEnabled.Value) Harmony.CreateAndPatchAll(typeof(InputPatches), null);
            if (InGameTimer.Value) Harmony.CreateAndPatchAll(typeof(RunTimerPatches), null);

            string both = MouseKeys.Value + "," + ControllerKeys.Value;
            string[] keys = both.Split(',');
            string validKeycodes = "Mouse button bound to keys/buttons: ";
            for (int i = 0; i < keys.Length; i++)
            {
                keys[i] = keys[i].Trim();
                KeyCode kc = KeyCode.None;
                try
                {
                    kc = (KeyCode)System.Enum.Parse(typeof(KeyCode), keys[i]);
                }
                catch { Logger.LogMessage(keys[i] + " is not a valid keycode name!"); }
                if (kc != KeyCode.None)
                {
                    InputPatches.mouseKeyboardKeys.Add(kc);
                    validKeycodes += keys[i] + ", ";
                }
            }
            Logger.LogMessage(validKeycodes);
        }

        public static int GameVersion()
        {
            //Determine the version of the game running by checking if the Kyu Plushie exists
            if (ItemNameList.ContainsKey("Kyu Plushie") == false) return JAN23;
            else return VALENTINES;
        }

        void PlayDialogAudio(int i, int j, int k = -1, int l = -1)
        {
            if (k >= 0) GameManager.System.Audio.Play(AudioCategory.VOICE, HunieMod.Definitions.DialogTriggers[i].lineSets[j].lines[k].dialogLine[l].audioDefinition, false, 1f, false);
            else GameManager.System.Audio.Play(AudioCategory.VOICE, HunieMod.Definitions.DialogScenes[i].steps[j].sceneLine.dialogLine.audioDefinition, false, 1f, false);
        }

        void PlayCheatLine(int line = -1)
        {
            int version = GameVersion(); //0 = jan23, 1 = valentines
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
                    if (version == JAN23) PlayDialogAudio(208, 1); else PlayDialogAudio(209, 1);
                    break;
                case 1: //Nikki: Dude, seriously right now?
                    if (version == JAN23) PlayDialogAudio(10, 1, 0, 2); else PlayDialogAudio(11, 1, 0, 2);
                    break;
                case 2: //Audrey: Ugh, gaaay!
                    if (version == JAN23) PlayDialogAudio(15, 5, 0, 3); else PlayDialogAudio(16, 5, 0, 3);
                    break;
                case 3: //Jessie: Really now, I didn't take you for one of those types
                    if (version == JAN23) PlayDialogAudio(15, 5, 0, 6); else PlayDialogAudio(16, 5, 0, 6);
                    break;
                case 4: //Celeste: Typical earthling
                    if (version == JAN23) PlayDialogAudio(17, 0, 2, 10); else PlayDialogAudio(18, 0, 2, 10);
                    break;
                case 5: //Kyu: Really? I know I taught you better than that
                    PlayDialogAudio(2, 1, 1, 8);
                    break;
                case 6: //Beli: That's terrible!
                    if (version == JAN23) PlayDialogAudio(15, 2, 0, 7); else PlayDialogAudio(16, 2, 0, 7);
                    break;
                case 7: //Venus: You can't possibly be serious
                    PlayDialogAudio(5, 3, 1, 11);
                    break;
                case 8: //Momo: Master, why?
                    if (version == JAN23) PlayDialogAudio(10, 1, 1, 9); else PlayDialogAudio(11, 1, 1, 9);
                    break;
                case 9: //Lola: What's the matter with you?
                    if (version == JAN23) PlayDialogAudio(10, 1, 2, 4); else PlayDialogAudio(11, 1, 2, 4);
                    break;
                case 10: //Aiko: Could you please not do that
                    if (version == JAN23) PlayDialogAudio(10, 1, 1, 1); else PlayDialogAudio(11, 1, 1, 1);
                    break;
                case 11: //Kyanna: Professional help
                    PlayDialogAudio(4, 2, 1, 2);
                    break;
                default:
                    break;
            }
        }

        private void OnApplicationQuit()
        {
            //save golds on the way out
            if (run != null)
                run.reset();
        }

        private void Update() // Another Unity method
        {
            /*bool cellButtonDisabled = (bool)AccessTools.Field(typeof(UITop), "_cellButtonDisabled")?.GetValue(GameManager.Stage.uiTop);
            Logger.LogMessage("Cellphone unlocked: " + GameManager.System.Player.cellphoneUnlocked);
            Logger.LogMessage("Interactive: " + GameManager.Stage.uiTop.buttonHuniebee.interactive);
            Logger.LogMessage("Is enabled: " + GameManager.Stage.uiTop.buttonHuniebee.button.IsEnabled());
            Logger.LogMessage("_disabled: " + cellButtonDisabled);*/
            //InputPatches.mouseWasDown = false; InputPatches.mouseWasClicked = false;
            RunTimerPatches.Update();

            //Test if we should send the "Venus unlocked" signal
            //All Panties routes would have met Momo or Celeste by now
            if (GameManager.System.GameState == GameState.SIM
                && GameManager.System.Player.GetGirlData(GameManager.Stage.uiGirl.alienGirlDef).metStatus != GirlMetStatus.MET
                && GameManager.System.Player.GetGirlData(GameManager.Stage.uiGirl.catGirlDef).metStatus != GirlMetStatus.MET
                && !BaseHunieModPlugin.cheatsEnabled && GameManager.Stage.girl.definition.firstName == "Venus"
                && GameManager.System.Player.GetGirlData(GameManager.Stage.girl.definition).metStatus != GirlMetStatus.MET
                && GameManager.Stage.girl.girlPieceContainers.localX < 520)
            {
                BasePatches.searchForMe = 500;
                if (run != null && run.goal == 69)
                {
                    run.split();
                    string newSplit = "Venus Unlocked\n      " + run.splitText + "\n";
                    run.push(newSplit);
                    run.save();
                }
            }

            if (GameManager.System.GameState == GameState.TITLE && BasePatches.titleScreenInteractive)
            {
                bool updateText = false;
                if (Input.GetKeyDown(KeyCode.DownArrow))
                {
                    lastChosenDifficulty++; if (lastChosenDifficulty >= RunTimer.difficulties.Length) lastChosenDifficulty = 0;
                    updateText = true;
                }
                if (Input.GetKeyDown(KeyCode.UpArrow))
                {
                    lastChosenDifficulty--; if (lastChosenDifficulty < 0) lastChosenDifficulty = RunTimer.difficulties.Length-1;
                    updateText = true;
                }
                if (Input.GetKeyDown(KeyCode.RightArrow))
                {
                    lastChosenCategory++; if (lastChosenCategory >= RunTimer.categories.Length) lastChosenCategory = 0;
                    updateText = true;
                }
                if (Input.GetKeyDown(KeyCode.LeftArrow))
                {
                    lastChosenCategory--; if (lastChosenCategory < 0) lastChosenCategory = RunTimer.categories.Length - 1;
                    updateText = true;
                }

                if (updateText)
                {
                    RunTimerPatches.UpdateFiles();
                }

                if (CheatHotkeyEnabled.Value && cheatsEnabled == false && Input.GetKeyDown(KeyCode.C))
                {
                    GameManager.System.Audio.Play(AudioCategory.SOUND, GameManager.Stage.uiPuzzle.puzzleGrid.failureSound, false, 2f, false);
                    GameManager.System.Audio.Play(AudioCategory.SOUND, GameManager.Stage.uiPuzzle.puzzleGrid.badMoveSound, false, 2f, false);
                    PlayCheatLine();
                    Harmony.CreateAndPatchAll(typeof(CheatPatches), null);
                    cheatsEnabled = true;
                }
            }

            if (ResetKey.Value.IsDown() || ResetKey2.Value.IsDown())
            {
                if (GameManager.System.GameState == GameState.TITLE)
                {
                    //GameUtil.QuitGame();
                }
                else
                {
                    if (GameUtil.EndGameSession(false, false, false))
                    {
                        hasReturned = true;
                        BasePatches.searchForMe = -111;
                        if (run != null)
                        {
                            run.reset();
                            run = null;
                        }
                    }
                }
            }

            //display the splits folder on Ctrl+S
            if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))
            {
                //display the splits folder on Ctrl+S
                if (Input.GetKeyDown(KeyCode.S))
                {
                    if (GameManager.System.GameState == GameState.TITLE)
                        System.Diagnostics.Process.Start(Directory.GetCurrentDirectory() + "/splits");
                    /*
                    else if (run != null)
                    {
                        run.save();
                        GameUtil.ShowNotification(CellNotificationType.MESSAGE, "Run saved!");
                    }*/

                }
                //reset run on Ctrl+R
                if (Input.GetKeyDown(KeyCode.R) && run != null)
                {
                    run.reset(true);
                    GameUtil.ShowNotification(CellNotificationType.MESSAGE, "Run reset!");
                }
                //quit run on Ctrl+Q
                if (Input.GetKeyDown(KeyCode.Q) && run != null)
                {
                    run.reset(false);
                    GameUtil.ShowNotification(CellNotificationType.MESSAGE, "Run quit!");
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

                if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))
                {
                    if (Input.GetKeyDown(KeyCode.M))
                    {
                        InputPatches.mashCheat = !InputPatches.mashCheat;
                        if (InputPatches.mashCheat)
                            GameUtil.ShowNotification(CellNotificationType.MESSAGE, "MASH POWER ACTIVATED!!!!!");
                        else
                            GameUtil.ShowNotification(CellNotificationType.MESSAGE, "Mash power disabled");

                    }

                    if (Input.GetKeyDown(KeyCode.N))
                    {
                        CheatPatches.awooga = !CheatPatches.awooga;
                        if (CheatPatches.awooga)
                            GameUtil.ShowNotification(CellNotificationType.MESSAGE, "AWOOOOOGA");
                        else
                            GameUtil.ShowNotification(CellNotificationType.MESSAGE, "Nude cheat disabled");
                        CheatPatches.RefreshGirls();
                    }

                    if (Input.GetKeyDown(KeyCode.T))
                    {
                        //PUZZLE TEST
                        /*The pieces in tokens are ordered alphabetically
                        Broken Heart, Flirtation, Joy, Passion, Romance, Sentiment, Sexuality, Talent */
                        GameUtil.ShowNotification(CellNotificationType.MESSAGE, "PUZZLE TEST");
                        PuzzleTokenDefinition[] tokens = GameManager.Data.PuzzleTokens.GetAll();

                        Dictionary<string, PuzzleGridPosition> theBoard = (Dictionary<string, PuzzleGridPosition>)AccessTools.Field(typeof(PuzzleGame), "_gridPositions").GetValue(Game.Puzzle.Game);
                        UIPuzzleGrid ui = (UIPuzzleGrid)AccessTools.Field(typeof(PuzzleGame), "_puzzleGrid").GetValue(Game.Puzzle.Game);

                        /*
                        int[] badboard = {
                        1, 4, 6, 7, 1, 4, 6, 7,
                        4, 6, 7, 1, 4, 6, 7, 1,
                        6, 7, 1, 4, 6, 7, 1, 4,
                        1, 4, 6, 7, 1, 4, 6, 7,
                        4, 6, 7, 1, 4, 6, 7, 1,
                        6, 7, 1, 4, 6, 7, 1, 4,
                        1, 4, 6, 7, 1, 4, 6, 7
                    };
                        */

                        int[] badboard = {
                        1, 4, 6, 7, 1, 4, 6, 7,
                        4, 6, 7, 1, 4, 0, 7, 1,
                        6, 7, 1, 4, 6, 6, 1, 4,
                        1, 4, 6, 7, 1, 6, 6, 7,
                        4, 6, 7, 6, 6, 7, 6, 1,
                        6, 7, 1, 0, 0, 6, 0, 0,
                        1, 4, 6, 0, 0, 6, 0, 0
                    };

                        for (int m = 6; m >= 0; m--)
                        {
                            for (int n = 7; n >= 0; n--)
                            {
                                PuzzleGridPosition blank = new PuzzleGridPosition(m, n, ui);
                                PuzzleGridPosition pgp = theBoard[blank.GetKey(0, 0)];
                                PuzzleToken pt = (PuzzleToken)AccessTools.Field(typeof(PuzzleGridPosition), "_token").GetValue(pgp);
                                pt.definition = tokens[badboard[(m * 8) + n]];
                                pt.level = 1;
                                pt.sprite.SetSprite(GameManager.Stage.uiPuzzle.puzzleGrid.puzzleTokenSpriteCollection, pt.definition.levels[pt.level - 1].GetSpriteName(false, false));
                                pgp.SetToken(pt);
                            }
                        }

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
