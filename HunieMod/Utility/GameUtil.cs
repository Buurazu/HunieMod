﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using BepInEx;
using GM = GameManager;
using HarmonyLib;
using UnityEngine;
using System.IO;

namespace HunieMod
{
    /// <summary>
    /// A class with general game-related utilities.
    /// </summary>
    public static class GameUtil
    {
        /// <summary>
        /// Ends the current game session and shows the titlescreen.
        /// </summary>
        /// <remarks>Only works when <see cref="GM.GameState"/> is <see cref="GameState.PUZZLE"/> or <see cref="GameState.SIM"/> and the tutorial has been completed.</remarks>
        /// <param name="saveGame">When <c>true</c>, save the current game session to the active save file.</param>
        /// <param name="revertDates">When <c>true</c> and <paramref name="saveGame"/> is also <c>true</c>, will set <see cref="GirlPlayerData.dayDated"/> to false when on a puzzle/date.</param>
        /// <param name="triggerValediction">When <c>true</c>, trigger a valediction dialog (goodbye) on the main girl.</param>
        /// <returns><c>True</c> when the session was ended, <c>false</c> when ending the session was blocked due to the game's current state.</returns>
        public static bool EndGameSession(bool saveGame = true, bool revertDates = true, bool triggerValediction = true)
        {
            
            /*if (!GM.System.Player.tutorialComplete || GetAvailableGirls(availableOnly: false, excludeCurrentGirl: false).Count == 0)
            {
                ShowNotification(CellNotificationType.MESSAGE, "Cannot leave the tutorial");
                //return false;
            }*/

            //base HunieMod could just use Input.GetMouseButtonDown(0) I think
            if (Input.GetMouseButtonDown(0) || InputPatches.mouseDown)
            {
                ShowNotification(CellNotificationType.MESSAGE, "Cannot leave with the mouse button clicked");
                return false;
            }

            if (GM.System == null || GM.System.GameState == GameState.TITLE || GM.System.GameState == GameState.LOADING || !GM.System.Location.IsLocationSettled()
                || (GM.System.Location.currentLocation.type == LocationType.NORMAL && !GM.Stage.uiWindows.IsDefaultWindowActive(true) && GM.System.Player.tutorialComplete))
            {
                ShowNotification(CellNotificationType.MESSAGE, "Cannot leave unless you could use the Girl Finder");
                return false;
            }
            if (GM.Stage.cellPhone.IsOpen())
            {
                ShowNotification(CellNotificationType.MESSAGE, "Cannot leave with the HunieBee open");
                return false;
            }

            if (GM.System.GameState == GameState.PUZZLE)
            {
                if (GM.System.Puzzle.Game.puzzleGameState != PuzzleGameState.WAITING)
                {
                    ShowNotification(CellNotificationType.MESSAGE, "Can only leave puzzles when you could make a move");
                    return false;
                }
                if (saveGame)
                {
                    GM.System.Player.GetGirlData(GM.System.Location.currentGirl).dayDated = !revertDates;
                    var returnTo = (LocationDefinition)AccessTools.Field(typeof(PuzzleManager), "_returnToLocation")?.GetValue(GM.System.Puzzle);
                    if (returnTo != null)
                        GM.System.Player.currentLocation = returnTo;
                }
                HidePuzzleGame(true);
            }
            else
            {
                GM.Stage.uiGirl.stats.localY = UIGirl.GIRL_STATS_HIDDEN_Y_POS;
            }

            GM.Stage.uiTop.buttonHuniebee.button.Disable();
            AccessTools.Field(typeof(UITop), "_cellButtonDisabled").SetValue(GM.Stage.uiTop, true);

            //bool cellButtonDisabled = (bool)AccessTools.Field(typeof(UITop), "_cellButtonDisabled")?.GetValue(GameManager.Stage.uiTop);

            GM.Stage.SetPausable(false);
            if (GM.Stage.cellPhone.IsOpen())
                ShowCellPhone(false, true);

            ClearActiveDialogScene();
            GM.Stage.altGirl.girlPieceContainers.localX = -GameCamera.SCREEN_DEFAULT_WIDTH_HALF;
            GM.Stage.altGirl.ClearGirl();
            GM.Stage.background.StopBackgroundMusic();
            GM.Stage.background.locationBackgrounds.gameObj.transform.localScale = Vector3.one * 0.95f;
            GM.Stage.uiWindows.HideActiveWindow();

            if (triggerValediction)
                StaticCoroutine.Do(TriggerValedictionDialog());

            if (saveGame)
                GM.System.SaveGame();

            //remove blinking message icon
            if (GM.System.Player.messages.Count > 0)
            {
                GM.System.Player.messages[0].viewed = true;
            }
            GM.Stage.uiTop.RefreshMessageAlert();
            //reset cell phone defaults
            GameManager.Stage.cellPhone.cellMemory = new Dictionary<string, int>();
            GameManager.Stage.cellPhone.SetCellApp(GameManager.Stage.cellPhone.defaultCellApp);

            GM.System.Location.currentGirl = null;
            GM.System.Location.currentLocation = null;
            AccessTools.Field(typeof(GM), "_saveFile").SetValue(GM.System, null);
            AccessTools.Method(typeof(LocationManager), "Awake").Invoke(GM.System.Location, null);

            GM.System.GameState = GameState.TITLE;
            GM.Stage.uiTitle.ShowTitleScreen();
            GM.Stage.uiTitle.SaveFileSelectedEvent += OnSaveFileSelected;

            //fuck it try this
            //GameManager.System.Init(GameManager.System.Hook);

            return true;
        }

        private static IEnumerator TriggerValedictionDialog()
        {
            var line = GM.System.Girl.GetDialogTriggerLine(GM.Stage.uiGirl.valedictionDialogTrigger, (int)GM.System.Clock.DayTime(-1));
            GM.Stage.girl.ReadDialogLine(line, passive: false, hideSpeechBubble: true);
            yield return new WaitForSeconds(line.GetAudio().clip.length - 0.5f);
            GM.Stage.girl.ClearDialog();
            yield break;
        }

        private static void OnSaveFileSelected(int saveFileIndex)
        {
            GM.Stage.uiTitle.SaveFileSelectedEvent -= OnSaveFileSelected;
            LoadSaveFile(saveFileIndex);
        }

        /// <summary>
        /// Stops and removes any active dialog scene and any girl dialog lines (including alt.) that may be active.
        /// </summary>
        /// <remarks>
        /// Note that a dialog scene is much more than just spoken dialog and pretty much encompasses all game logic.
        /// When calling this method at the wrong time, the game/save can break irreversibly due to getting stuck in a scene/step that cannot advance.
        /// </remarks>
        public static void ClearActiveDialogScene()
        {
            var list = (List<DialogSceneStepsProgress>)AccessTools.Field(typeof(DialogManager), "_activeDialogSceneSteps").GetValue(GM.System.Dialog);
            list.Clear();
            AccessTools.Field(typeof(DialogManager), "_activeDialogScene").SetValue(GM.System.Dialog, null);
            GM.Stage.girl.ClearDialog();
            GM.Stage.altGirl.ClearDialog();
        }

        /// <summary>
        /// Gets a list of girls in the game with several optional filters.
        /// </summary>
        /// <param name="metOnly">When <c>true</c>, only include girls for which <see cref="GirlPlayerData.metStatus"/> is <see cref="GirlMetStatus.MET"/>.</param>
        /// <param name="availableOnly">When <c>true</c>, only include girls that are currently available for meeting.</param>
        /// <param name="excludeCurrentGirl">When <c>true</c>, exclude the current active girl.</param>
        /// <returns>A list of <see cref="GirlDefinition"/> matching the specified filters. An empty list is returned when there are no matches.</returns>
        public static List<GirlDefinition> GetAvailableGirls(bool metOnly = true, bool availableOnly = true, bool excludeCurrentGirl = true)
        {
            var allGirls = GM.Data.Girls.GetAll();
            var girls = new List<GirlDefinition>();
            var clock = GM.System.Clock;
            allGirls.ForEach(girl =>
            {
                if (!excludeCurrentGirl || GM.System.Location.currentGirl != girl)
                {
                    var girlData = GM.System.Player.GetGirlData(girl);
                    var girlLocation = girl.IsAtLocationAtTime(clock.Weekday(clock.TotalMinutesElapsed(ClockManager.MINUTES_PER_DAYTIME), true),
                                                               clock.DayTime(clock.TotalMinutesElapsed(ClockManager.MINUTES_PER_DAYTIME)));

                    if ((!metOnly || girlData.metStatus == GirlMetStatus.MET) && (!availableOnly || girlLocation != null))
                        girls.Add(girl);
                }
            });
            return girls;
        }

        /// <summary>
        /// Gets a random girl that matches the specified filters.
        /// </summary>
        /// <param name="metOnly">When <c>true</c>, only include girls for which <see cref="GirlPlayerData.metStatus"/> is <see cref="GirlMetStatus.MET"/>.</param>
        /// <param name="availableOnly">When <c>true</c>, only include girls that are currently available for meeting.</param>
        /// <param name="excludeCurrentGirl">When <c>true</c>, exclude the current active girl.</param>
        /// <returns>A random <see cref="GirlDefinition"/> matching the specified filters, or <c>null</c> when no match was found.</returns>
        public static GirlDefinition GetRandomAvailableGirl(bool metOnly = true, bool availableOnly = true, bool excludeCurrentGirl = true)
        {
            var girls = GetAvailableGirls(metOnly, availableOnly, excludeCurrentGirl);
            return girls.Count > 0 ? girls[UnityEngine.Random.Range(0, girls.Count)] : null;
        }

        /// <summary>
        /// Gets a random position on the visible game screen.
        /// </summary>
        /// <returns>A random position on the screen.</returns>
        public static Vector2 GetRandomScreenPosition()
        {
            var cam = GM.System.gameCamera.mainCamera;
            var x = UnityEngine.Random.Range(cam.ScreenToWorldPoint(Vector3.zero).x, cam.ScreenToWorldPoint(new Vector2(GameCamera.SCREEN_DEFAULT_WIDTH, 0)).x);
            var y = UnityEngine.Random.Range(cam.ScreenToWorldPoint(Vector3.zero).y, cam.ScreenToWorldPoint(new Vector2(0, GameCamera.SCREEN_DEFAULT_HEIGHT)).y);
            return new Vector2(x, y);
        }

        /// <summary>
        /// Hides the visual puzzle grid and the puzzle status at the bottom of the screen. Optionally destroys the active <see cref="PuzzleGame"/> to stop the puzzle logic.
        /// </summary>
        /// <param name="destroy">When <c>true</c>, destroy the active <see cref="PuzzleGame"/> that handles puzzle logic.</param>
        /// <param name="hidePuzzleStatus">When <c>true</c>, hide the puzzle status at the bottom of the screen.</param>
        public static void HidePuzzleGame(bool destroy = false, bool hidePuzzleStatus = true)
        {
            if (destroy)
            {
                GM.System.Puzzle.Game.Destroy();
                GM.Stage.uiPuzzle.puzzleStatus.UpdatePuzzleEffects(null);
                AccessTools.Field(typeof(PuzzleManager), "_activePuzzleGame").SetValue(GM.System.Puzzle, null);
                AccessTools.Field(typeof(Girl), "DialogLineBeginEvent").SetValue(GM.Stage.girl, null, BindingFlags.Public, null, null);
                AccessTools.Field(typeof(Girl), "DialogLineReadEvent").SetValue(GM.Stage.girl, null, BindingFlags.Public, null, null);
            }
            GM.Stage.uiPuzzle.puzzleGrid.SetLocalScale(0.9f, 0f);
            GM.Stage.uiPuzzle.puzzleGrid.gridBackground.SetAlpha(0f, 0f);
            GM.Stage.uiPuzzle.puzzleGrid.gridBorder.SetAlpha(0f, 0f);
            GM.Stage.uiPuzzle.puzzleGrid.tokenContainer.SetChildAlpha(1f, 0f);
            GM.Stage.uiPuzzle.puzzleGrid.notifier.SetAlpha(0f, 0f);
            GM.Stage.uiPuzzle.puzzleGrid.notifier.SetLocalScale(1f, 0f);
            GM.Stage.uiPuzzle.puzzleGrid.notifierBurst.SetAlpha(0f, 0f);
            GM.Stage.uiPuzzle.puzzleGrid.notifierBurst.SetLocalScale(1f, 0f);
            GM.Stage.uiPuzzle.puzzleGrid.gameObj.SetActive(false);

            if (hidePuzzleStatus)
                GM.Stage.uiPuzzle.puzzleStatus.localY = UIGirl.GIRL_STATS_HIDDEN_Y_POS;
        }

        /// <summary>
        /// Loads the <see cref="SaveFile"/> of the specified index and begins a game session with it.
        /// </summary>
        /// <param name="index">The index of the save file to load and start a game with.</param>
        /// <param name="gender">The gender to use when starting a new game when the specified <paramref name="index"/> is an empty save file.</param>
        public static void LoadSaveFile(int index, SettingsGender gender = SettingsGender.MALE)
        {
            if (index < 0 || index >= SaveUtils.SAVE_FILE_COUNT)
                throw new ArgumentOutOfRangeException(nameof(index), $"Min: 0  Max: {SaveUtils.SAVE_FILE_COUNT - 1}");

            if (GM.System != null)
            {
                SaveFile saveFile = SaveUtils.GetSaveFile(index);
                if (saveFile != null)
                {
                    if (GM.Stage != null)
                    {
                        // Remove all subscriptions from event
                        AccessTools.Field(typeof(UITitle), "SaveFileSelectedEvent").SetValue(GM.Stage.uiTitle, null, BindingFlags.Public, null, null);

                        GM.Stage.uiTitle.HideTitleScreen();
                        GM.Stage.SetPausable(true);

                        GM.Stage.uiTop.buttonHuniebee.interactive = true;
                    }

                    if (!saveFile.started)
                    {
                        BaseHunieModPlugin.hasReturned = false;
                        saveFile.settingsGender = (int)gender;
                    }

                    // Set the active save file
                    AccessTools.Field(typeof(GM), "_saveFile").SetValue(GM.System, saveFile);

                    // Start game session
                    AccessTools.Method(typeof(GM), "BeginGameSession").Invoke(GM.System, null);
                }
            }
        }

        /// <summary>
        /// Exits the game process.
        /// </summary>
        /// <param name="killProcess">When <c>true</c>, instantly kill the game's process. Otherwise, let the application itself handle quitting.</param>
        public static void QuitGame(bool killProcess = true)
        {
            if (killProcess)
                Process.GetCurrentProcess()?.Kill();
            else
                Application.Quit();
        }

        /// <summary>
        /// Resets the <see cref="SaveFile"/> of the specified index, equal to the Erase option on the title screen.
        /// </summary>
        /// <param name="index">The zero-based index of the save file to reset. Refer to <see cref="SaveUtils.SAVE_FILE_COUNT"/> for the highest index.</param>
        /// <param name="force">
        /// When <c>true</c>, reset the save file regardless of it being the active <see cref="GM.SaveFile"/> or not.
        /// This is a DANGEROUS option and has the potential to corrupt your save file.
        /// </param>
        /// <returns><c>True</c> when the save file was reset, <c>false</c> when it was not found or when it is the active save file and <paramref name="force"/> is <c>false</c>.</returns>
        public static bool ResetSaveFile(int index, bool force = false)
        {
            if (index < 0 || index >= SaveUtils.SAVE_FILE_COUNT)
                throw new ArgumentOutOfRangeException(nameof(index), $"Minimum: 0  Maximum: {SaveUtils.SAVE_FILE_COUNT - 1}");

            if (GM.System != null)
            {
                var saveFile = SaveUtils.GetSaveFile(index);
                if (saveFile != null && (force || saveFile != GM.System.SaveFile))
                {
                    saveFile.ResetFile();
                    SaveUtils.Save();
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Instantly show/hide the HunieBee and pause/unpause the game.
        /// </summary>
        /// <param name="show">When <c>true</c>, shows the cell phone and pauses the game, otherwise hides the cell phone and unpauses the game.</param>
        /// <param name="silent">When <c>true</c>, don't play any sound effects.</param>
        public static void ShowCellPhone(bool show = true, bool silent = false)
        {
            if (show)
            {
                GM.System.Pause();
                GM.Stage.cellPhone.Open();
            }
            else
            {
                GM.System.Unpause();
                GM.Stage.cellPhone.Close();
            }

            // What it normally does. This however can interfere with the normal opening/closing of the cellphone and
            // could make uiTop fall behind the background and thus become invisible
            //GM.Stage.uiTop.ShiftSelf(show ? 5 : -5);

            // Set a pre-defined index instead
            GM.Stage.uiTop.SetOwnChildIndex(show ? 7 : 2);

            GM.Stage.cellPhone.localX = show ? UICellPhone.OPEN_X_POSITION : UICellPhone.CLOSED_X_POSITION;
            GM.Stage.cellPhone.interactive = show;
            GM.Stage.uiTop.pauseOverlay.spriteAlpha = show ? 0.5f : 0f;

            if (!silent)
            {
                GM.System.Audio.Play(
                    AudioCategory.SOUND,
                    show ? GM.Stage.uiPhotoGallery.closeSound : GM.Stage.uiPhotoGallery.flipSound,
                    false,
                    1.4f);
            }
        }

        public static Texture2D ImageFileToTexture(string filename, bool isEmbeddedResource = false)
        {
            byte[] imageData;

            if (isEmbeddedResource)
            {
                Assembly assembly = Assembly.GetExecutingAssembly();
                //Assembly assembly = typeof(SpriteUtil).Assembly; // Replace with the assembly that contains your images, if you want to use embedded ones...

                using (Stream stream = assembly.GetManifestResourceStream(filename))
                {
                    if (stream == null)
                    {
                        //Logger.LogInfo($"{nameof(SpriteUtil)}.{nameof(ImageFileToTexture)}: Resource does not exist: {filename}");
                        return null;
                    }
                    imageData = new byte[stream.Length];
                    stream.Read(imageData, 0, (int)stream.Length);
                }
            }
            else
            {
                if (!File.Exists(filename))
                {
                    //Logger.LogInfo($"{nameof(SpriteUtil)}.{nameof(ImageFileToTexture)}: File does not exist: {filename}");
                    return null;
                }
                using (FileStream stream = File.Open(filename, FileMode.Open, FileAccess.Read))
                {
                    imageData = new byte[stream.Length];
                    stream.Read(imageData, 0, (int)stream.Length);
                }
            }

            Texture2D texture = new Texture2D(2, 2, TextureFormat.ARGB32, false);

            if (imageData?.Length > 0)
            {
                texture.LoadImage(imageData);
            }

            return texture;
        }

        public static SpriteObject ImageFileToSprite(string filename, string spriteName, bool isEmbeddedResource = false)
        {
            Texture2D texture = ImageFileToTexture(filename, isEmbeddedResource);
            if (texture == null)
            {
                return null;
            }

            GameObject spriteGameObject = tk2dSprite.CreateFromTexture(
                texture,
                tk2dSpriteCollectionSize.ForResolution(GameCamera.SCREEN_DEFAULT_HEIGHT_HALF, GameCamera.SCREEN_DEFAULT_WIDTH, GameCamera.SCREEN_DEFAULT_HEIGHT),
                new Rect(0f, 0f, texture.width, texture.height),
                Vector2.zero);

            tk2dSprite sprite = spriteGameObject.GetComponent<tk2dSprite>();
            sprite.CurrentSprite.name = spriteName;

            return DisplayUtils.CreateSpriteObject(sprite.Collection, spriteName);
        }

        /// <summary>
        /// Show a notification at the top of the screen, regardless of game state.
        /// </summary>
        /// <param name="type">The type of notification to show.</param>
        /// <param name="text">The text of the notification.</param>
        public static void ShowNotification(CellNotificationType type, string text)
        {
            if (text.IsNullOrWhiteSpace())
                return;

            var notification = new CellNotification(type, text);
            AccessTools.Method(typeof(UICellNotifications), "ShowNotification")?.Invoke(GM.Stage.cellNotifications, new object[] { notification });
        }
    }
}
