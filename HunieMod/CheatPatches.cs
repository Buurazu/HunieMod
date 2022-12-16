using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using BepInEx.Logging;
using HarmonyLib;
using HarmonyLib.Tools;
using UnityEngine;

namespace HunieMod
{
    // Token: 0x02000002 RID: 2
    public class CheatPatches
    {

        public static bool awooga;
        /*The pieces in tokens are ordered alphabetically
                    Broken Heart, Flirtation, Joy, Passion, Romance, Sentiment, Sexuality, Talent */
        public static PuzzleTokenDefinition[] tokens = GameManager.Data.PuzzleTokens.GetAll();
        //Talent, Flirtation, Romance, Sexuality, Passion, Broken Heart, Joy, Sentiment
        public static PuzzleTokenDefinition[] orderedTokens =
            { tokens[7], tokens[1], tokens[4], tokens[6], tokens[3], tokens[0], tokens[2], tokens[5]};
        public static string[] tokenNames =
            { "Talent", "Flirtation", "Romance", "Sexuality", "Passion", "Broken Heart", "Joy", "Sentiment" };

        public static PuzzleTokenDefinition[] savedBoard = new PuzzleTokenDefinition[56];
        public static int savedAffection, savedPassion, savedMoves, savedSentiment;
        public static Dictionary<PuzzleTokenDefinition, int> savedWeights = new Dictionary<PuzzleTokenDefinition, int>();

        public static void Update()
        {
            if (!BaseHunieModPlugin.cheatsEnabled) return;
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
                if (BaseHunieModPlugin.savingDisabled)
                {
                    BaseHunieModPlugin.savingDisabled = false;
                    GameUtil.ShowNotification(CellNotificationType.MESSAGE, "Saving has been enabled");
                }
                else
                {
                    BaseHunieModPlugin.savingDisabled = true;
                    GameUtil.ShowNotification(CellNotificationType.MESSAGE, "Saving has been disabled");
                }
            }

            if (Input.GetKeyDown(KeyCode.F3))
            {
                if (GameManager.System.GameState == GameState.PUZZLE)
                {
                    if (GameManager.System.Puzzle.Game.isBonusRound)
                    {
                        GameManager.System.Puzzle.Game.SetResourceValue(PuzzleGameResourceType.AFFECTION, 0, false);
                        BaseHunieModPlugin.run.runTimer = DateTime.UtcNow.Ticks;
                    }
                    AccessTools.Field(typeof(PuzzleGame), "_goalAffection")?.SetValue(GameManager.System.Puzzle.Game, 10000);
                    GameManager.System.Puzzle.Game.AddResourceValue(PuzzleGameResourceType.AFFECTION, 0, false);
                    GameUtil.ShowNotification(CellNotificationType.MESSAGE, "Goal set to 10,000!");
                }
            }

            if (Input.GetKeyDown(KeyCode.F5))
            {
                if (GameManager.System.GameState == GameState.PUZZLE)
                {
                    //refresh the date!!!
                    /*
                    if (GameManager.System.Puzzle.Game.puzzleGameState != PuzzleGameState.WAITING)
                    {
                        GameUtil.ShowNotification(CellNotificationType.MESSAGE, "Can only refresh puzzles when you could make a move");
                    }
                    else
                    */
                    {
                        GameUtil.ShowNotification(CellNotificationType.MESSAGE, "Restarting the date!");
                        GameManager.Stage.uiPuzzle.puzzleStatus.UpdatePuzzleEffects(null);
                        AccessTools.Method(typeof(PuzzleManager), "HidePuzzleGrid").Invoke(GameManager.System.Puzzle, null);
                        GameManager.System.Puzzle.TravelToPuzzleLocation(GameManager.System.Location.currentLocation, GameManager.System.Location.currentGirl);
                    }
                }
                else if (GameManager.System.GameState == GameState.SIM)
                {
                    if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))
                    {
                        DeleteGiftInventory();
                    }
                    GameManager.System.Player.RollNewDay();
                    GameManager.Stage.cellPhone.RefreshActiveCellApp();
                    GameUtil.ShowNotification(CellNotificationType.MESSAGE, "Refreshing the store!");
                }
            }

            if (Input.GetKeyDown(KeyCode.F6))
            {
                GameUtil.ShowNotification(CellNotificationType.MESSAGE, "Deleting gifts from inventory!");
                DeleteGiftInventory();
            }

            if (Input.GetKeyDown(KeyCode.F7))
            {
                foreach (GirlPlayerData girlData in GameManager.System.Player.girls)
                {
                    if (girlData.metStatus < GirlMetStatus.UNKNOWN) girlData.metStatus = GirlMetStatus.UNKNOWN;
                }
                GameUtil.ShowNotification(CellNotificationType.MESSAGE, "All girls available to meet!");
            }

            if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))
            {
                //Passion/Sentiment/Moves increase/decrease
                if (GameManager.System.GameState == GameState.PUZZLE)
                {
                    int mult = 1;
                    if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)) mult = -1;

                    if (Input.GetKeyDown(KeyCode.P))
                        GameManager.System.Puzzle.Game.SetResourceValue(PuzzleGameResourceType.PASSION,
                            GameManager.System.Puzzle.GetPassionLevelCost(GameManager.System.Puzzle.Game.currentPassionLevel + mult), false);
                    if (Input.GetKeyDown(KeyCode.S)) GameManager.System.Puzzle.Game.AddResourceValue(PuzzleGameResourceType.SENTIMENT, mult, false);
                    if (Input.GetKeyDown(KeyCode.M)) GameManager.System.Puzzle.Game.AddResourceValue(PuzzleGameResourceType.MOVES, mult, false);


                }

                if (Input.GetKeyDown(KeyCode.M))
                {
                    if (GameManager.System.GameState == GameState.SIM)
                    {
                        InputPatches.mashCheat = !InputPatches.mashCheat;
                        if (InputPatches.mashCheat)
                            GameUtil.ShowNotification(CellNotificationType.MESSAGE, "MASH POWER ACTIVATED!!!!!");
                        else
                            GameUtil.ShowNotification(CellNotificationType.MESSAGE, "Mash power disabled");
                    }
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

                if (Input.GetKeyDown(KeyCode.R))
                {
                    GirlPlayerData girlData = GameManager.System.Player.GetGirlData(GameManager.System.Location.currentGirl);


                    if (Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt) || Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
                    {
                        if (girlData.relationshipLevel > 1)
                        {
                            int newLevel = girlData.relationshipLevel - 1;
                            AccessTools.Field(typeof(GirlPlayerData), "_relationshipLevel")?.SetValue(girlData, newLevel);
                            GameUtil.ShowNotification(CellNotificationType.MESSAGE, "Relationship Leveled Down to " + girlData.relationshipLevel + "!");
                            GameManager.Stage.uiGirl.ShowCurrentGirlStats();
                        }
                    }
                    else
                    {
                        if (girlData.relationshipLevel != 5)
                        {
                            int newLevel = girlData.relationshipLevel + 1;
                            AccessTools.Field(typeof(GirlPlayerData), "_relationshipLevel")?.SetValue(girlData, newLevel);
                            GameUtil.ShowNotification(CellNotificationType.MESSAGE, "Relationship Leveled Up to " + girlData.relationshipLevel + "!");
                            GameManager.Stage.uiGirl.ShowCurrentGirlStats();
                        }
                    }
                }

                if (Input.GetKeyDown(KeyCode.C))
                {
                    GameUtil.ShowNotification(CellNotificationType.MESSAGE, "Copying the board");
                    Dictionary<string, PuzzleGridPosition> theBoard = (Dictionary<string, PuzzleGridPosition>)AccessTools.Field(typeof(PuzzleGame), "_gridPositions").GetValue(GameManager.System.Puzzle.Game);
                    UIPuzzleGrid ui = (UIPuzzleGrid)AccessTools.Field(typeof(PuzzleGame), "_puzzleGrid").GetValue(GameManager.System.Puzzle.Game);

                    Dictionary<PuzzleTokenDefinition, int> weights = (Dictionary<PuzzleTokenDefinition, int>)AccessTools.Field(typeof(PuzzleGame), "_tokenWeights").GetValue(GameManager.System.Puzzle.Game);
                    foreach (PuzzleTokenDefinition ptd in tokens)
                    {
                        savedWeights[ptd] = weights[ptd];
                    }

                    for (int m = 6; m >= 0; m--)
                    {
                        for (int n = 7; n >= 0; n--)
                        {
                            PuzzleGridPosition blank = new PuzzleGridPosition(m, n, ui);
                            PuzzleGridPosition pgp = theBoard[blank.GetKey(0, 0)];
                            PuzzleToken pt = (PuzzleToken)AccessTools.Field(typeof(PuzzleGridPosition), "_token").GetValue(pgp);

                            savedBoard[m * 8 + n] = pt.definition;
                        }
                    }
                    savedAffection = GameManager.System.Puzzle.Game.GetResourceValue(PuzzleGameResourceType.AFFECTION);
                    savedPassion = GameManager.System.Puzzle.Game.GetResourceValue(PuzzleGameResourceType.PASSION);
                    savedMoves = GameManager.System.Puzzle.Game.GetResourceValue(PuzzleGameResourceType.MOVES);
                    savedSentiment = GameManager.System.Puzzle.Game.GetResourceValue(PuzzleGameResourceType.SENTIMENT);
                }

                if (Input.GetKeyDown(KeyCode.V))
                {
                    GameUtil.ShowNotification(CellNotificationType.MESSAGE, "Pasting copied board");
                    Dictionary<string, PuzzleGridPosition> theBoard = (Dictionary<string, PuzzleGridPosition>)AccessTools.Field(typeof(PuzzleGame), "_gridPositions").GetValue(GameManager.System.Puzzle.Game);
                    UIPuzzleGrid ui = (UIPuzzleGrid)AccessTools.Field(typeof(PuzzleGame), "_puzzleGrid").GetValue(GameManager.System.Puzzle.Game);
                    Dictionary<PuzzleTokenDefinition, int> weights = (Dictionary<PuzzleTokenDefinition, int>)AccessTools.Field(typeof(PuzzleGame), "_tokenWeights").GetValue(GameManager.System.Puzzle.Game);

                    foreach (PuzzleTokenDefinition ptd in tokens)
                    {
                        weights[ptd] = savedWeights[ptd];
                    }

                    for (int m = 6; m >= 0; m--)
                    {
                        for (int n = 7; n >= 0; n--)
                        {
                            PuzzleGridPosition blank = new PuzzleGridPosition(m, n, ui);
                            PuzzleGridPosition pgp = theBoard[blank.GetKey(0, 0)];
                            PuzzleToken pt = (PuzzleToken)AccessTools.Field(typeof(PuzzleGridPosition), "_token").GetValue(pgp);

                            pt.definition = savedBoard[m * 8 + n];
                            pt.level = 1;
                            pt.sprite.SetSprite(GameManager.Stage.uiPuzzle.puzzleGrid.puzzleTokenSpriteCollection, pt.definition.levels[pt.level - 1].GetSpriteName(false, false));
                            pgp.SetToken(pt);
                        }
                    }
                    GameManager.System.Puzzle.Game.SetResourceValue(PuzzleGameResourceType.AFFECTION, savedAffection);
                    GameManager.System.Puzzle.Game.SetResourceValue(PuzzleGameResourceType.PASSION, savedPassion);
                    GameManager.System.Puzzle.Game.SetResourceValue(PuzzleGameResourceType.MOVES, savedMoves);
                    GameManager.System.Puzzle.Game.SetResourceValue(PuzzleGameResourceType.SENTIMENT, savedSentiment);

                }

                if (Input.GetKeyDown(KeyCode.T))
                {
                    //PUZZLE TEST
                    /*The pieces in tokens are ordered alphabetically
                    Broken Heart, Flirtation, Joy, Passion, Romance, Sentiment, Sexuality, Talent */


                    GameUtil.ShowNotification(CellNotificationType.MESSAGE, "PUZZLE TEST");

                    Dictionary<string, PuzzleGridPosition> theBoard = (Dictionary<string, PuzzleGridPosition>)AccessTools.Field(typeof(PuzzleGame), "_gridPositions").GetValue(GameManager.System.Puzzle.Game);
                    UIPuzzleGrid ui = (UIPuzzleGrid)AccessTools.Field(typeof(PuzzleGame), "_puzzleGrid").GetValue(GameManager.System.Puzzle.Game);

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
                    List<int[]> tutorialBoards = new List<int[]>();
                    //flirt + 2?
                    tutorialBoards.Add(new int[] { 3, 2, 1, 2, 8, 3, 2, 1, 1, 6, 1, 6, 3, 5, 4, 1, 2, 1, 8, 5, 1, 6, 3, 5, 8, 1, 3, 3, 2, 3, 6, 6, 4, 5, 1, 4, 2, 1, 1, 4, 2, 1, 4, 6, 3, 7, 2, 5, 5, 5, 4, 1, 8, 2, 4, 8 });

                    int[] badboard = {
                    1, 4, 6, 7, 1, 4, 6, 7,
                    4, 6, 7, 1, 4, 0, 7, 1,
                    6, 7, 1, 4, 6, 6, 1, 4,
                    1, 4, 6, 7, 1, 6, 6, 7,
                    4, 6, 7, 6, 6, 7, 6, 1,
                    6, 7, 1, 0, 0, 6, 0, 0,
                    1, 4, 6, 0, 0, 6, 0, 0
                };
                    bool applied = false;
                    string loggedBoard = "";
                    for (int m = 6; m >= 0; m--)
                    {
                        for (int n = 7; n >= 0; n--)
                        {
                            PuzzleGridPosition blank = new PuzzleGridPosition(m, n, ui);
                            PuzzleGridPosition pgp = theBoard[blank.GetKey(0, 0)];
                            PuzzleToken pt = (PuzzleToken)AccessTools.Field(typeof(PuzzleGridPosition), "_token").GetValue(pgp);
                            
                            loggedBoard += pt.definition.id + ",";
                            pt.definition = orderedTokens[tutorialBoards[0][(m * 8) + n]];
                            //pt.definition = tokens[(n + m) % 2 + 1];
                            pt.level = 1;
                            if ((n + m) % 2 + 1 == 1 && !applied)
                            {
                                pt.level = 2;
                                applied = true;
                            }
                            pt.sprite.SetSprite(GameManager.Stage.uiPuzzle.puzzleGrid.puzzleTokenSpriteCollection, pt.definition.levels[pt.level - 1].GetSpriteName(false, false));
                            pgp.SetToken(pt);
                        }
                    }
                    BasePatches.Logger.LogMessage(loggedBoard);


                }

                if (Input.GetKeyDown(KeyCode.O))
                {
                    if (Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt) || Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
                    {
                        foreach (GirlPlayerData girlData in GameManager.System.Player.girls)
                        {
                            girlData.hairstyle = BaseHunieModPlugin.hairstylePreferences[girlData.GetGirlDefinition().firstName].Value;
                            girlData.outfit = BaseHunieModPlugin.outfitPreferences[girlData.GetGirlDefinition().firstName].Value;
                        }
                        RefreshGirlOutfit(GameManager.Stage.girl);
                        RefreshGirlOutfit(GameManager.Stage.altGirl);
                        
                        GameUtil.ShowNotification(CellNotificationType.MESSAGE, "Hairstyle and outfit preferences loaded!");
                    }
                    else
                    {
                        foreach (GirlPlayerData girlData in GameManager.System.Player.girls)
                        {
                            BaseHunieModPlugin.hairstylePreferences[girlData.GetGirlDefinition().firstName].Value = girlData.hairstyle;
                            BaseHunieModPlugin.outfitPreferences[girlData.GetGirlDefinition().firstName].Value = girlData.outfit;
                        }
                        SaveUtils.Save();
                        GameUtil.ShowNotification(CellNotificationType.MESSAGE, "Hairstyle and outfit preferences saved!");
                    }
                }

                if (Input.GetMouseButtonDown(1))
                {
                    if (GameManager.System.GameState == GameState.PUZZLE && highlightedToken != null)
                        ChangeTokenType(ref highlightedToken, GameManager.System.Puzzle.Game);
                }
            }
        }

        public static void DeleteGiftInventory()
        {
            List<UIGirlItemSlot> slots = (List<UIGirlItemSlot>)AccessTools.Field(typeof(UIGirl), "_itemSlots").GetValue(GameManager.Stage.uiGirl);
            foreach (UIGirlItemSlot slot in slots)
            {
                if (slot.itemDefinition == null) continue;
                if (slot.itemDefinition.type == ItemType.GIFT || slot.itemDefinition.type == ItemType.FOOD || slot.itemDefinition.type == ItemType.DRINK)
                    slot.ConsumeSlotItem();
            }

            InventoryItemPlayerData[] inventory = (InventoryItemPlayerData[])AccessTools.Field(typeof(PlayerManager), "_inventory").GetValue(GameManager.System.Player);
            foreach (InventoryItemPlayerData slot in inventory)
            {
                if (slot.itemDefinition == null) continue;
                if (slot.presentDefinition != null || slot.itemDefinition.type == ItemType.GIFT || slot.itemDefinition.type == ItemType.FOOD || slot.itemDefinition.type == ItemType.DRINK)
                    slot.itemDefinition = null;
            }
            GameManager.Stage.cellPhone.RefreshActiveCellApp();
        }

        public static void RefreshGirlOutfit(Girl girl)
        {
            if (girl.definition == null) return;
            girl.ChangeStyle(girl.definition.hairstyles[BaseHunieModPlugin.hairstylePreferences[girl.definition.firstName].Value].artIndex, true);
            girl.ChangeStyle(girl.definition.outfits[BaseHunieModPlugin.outfitPreferences[girl.definition.firstName].Value].artIndex, true);
        }

        public static void AddItem(string theItem, InventoryItemPlayerData[] target = null)
        {
            if (target == null) target = GameManager.System.Player.inventory;
            if (!GameManager.System.Player.HasItem(GameManager.Data.Items.Get(BaseHunieModPlugin.ItemNameList[theItem])))
                GameManager.System.Player.AddItem(GameManager.Data.Items.Get(BaseHunieModPlugin.ItemNameList[theItem]), target, false, false);
        }
        public static void AddItem(int theItem, InventoryItemPlayerData[] target = null)
        {
            if (target == null) target = GameManager.System.Player.inventory;
            if (!GameManager.System.Player.HasItem(GameManager.Data.Items.Get(theItem)))
                GameManager.System.Player.AddItem(GameManager.Data.Items.Get(theItem), target, false, false);
        }

        public static void AddGreatGiftsToInventory()
        {
            //perfumes
            CheatPatches.AddItem(151); CheatPatches.AddItem(152); CheatPatches.AddItem(153); CheatPatches.AddItem(154);
            //flowers
            CheatPatches.AddItem(139); CheatPatches.AddItem(140); CheatPatches.AddItem(141);
            CheatPatches.AddItem(142); CheatPatches.AddItem(143); CheatPatches.AddItem(144);

            CheatPatches.AddItem("Suede Ankle Booties"); CheatPatches.AddItem("Leopard Print Pumps"); CheatPatches.AddItem("Shiny Lipstick");
            CheatPatches.AddItem("Heart Necklace"); CheatPatches.AddItem("Pearl Necklace");
            CheatPatches.AddItem("Stuffed Penguin"); CheatPatches.AddItem("Stuffed Whale");
            GameUtil.ShowNotification(CellNotificationType.MESSAGE, "Fantastic date gifts added to inventory");
        }

        //I use this function to test meeting Venus
        /*
        [HarmonyPrefix]
        [HarmonyPatch(typeof(LocationManager), "CheckForSecretGirlUnlock")]
        public static bool TestingVenus(ref GirlDefinition __result)
        {
            __result = GameManager.Stage.uiGirl.goddessGirlDef;
            return false;
        }
        */

        //I had all girls available to meet in cheat mode, but I moved it to a hotkey because it sucked for HunieBee mouse routing
        /*
        [HarmonyPostfix]
        [HarmonyPatch(typeof(GirlPlayerData), "ReadGirlSaveData")]
        public static void MakeUsMet(GirlPlayerData __instance)
        {
            if (__instance.metStatus < GirlMetStatus.UNKNOWN) __instance.metStatus = GirlMetStatus.UNKNOWN;
        }
        */

        public static int lastSplitAffection = 0;
        [HarmonyPostfix]
        [HarmonyPatch(typeof(PuzzleGame), "OnUpdate")]
        public static void BonusRoundSplits(PuzzleGame __instance, ref int ____currentAffection, ref int ____goalAffection, ref bool ____isBonusRound)
        {
            if (!____isBonusRound || BaseHunieModPlugin.run == null || ____goalAffection < 10000) return;
            if (RunTimerPatches.preventAllUpdate) RunTimerPatches.ChangePuzzleUIText();
            if (____currentAffection == 0)
            {
                lastSplitAffection = 0;
                BaseHunieModPlugin.run.runTimer = DateTime.UtcNow.Ticks;
            }

            //if (____currentAffection - lastSplitAffection >= 500)
            if (__instance.currentDisplayAffection - lastSplitAffection >= 500)
            {
                lastSplitAffection = ____currentAffection;
                //lastSplitAffection += 500;
                BaseHunieModPlugin.run.split();
                RunTimerPatches.preventAllUpdate = true;
            }
            long tickDiff = DateTime.UtcNow.Ticks - BaseHunieModPlugin.run.runTimer;
            if (tickDiff / TimeSpan.TicksPerSecond > 2)
            {
                RunTimerPatches.preventAllUpdate = false;
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(GameManager), "SaveGame")]
        public static bool SaveDisabler() {
            if (BaseHunieModPlugin.savingDisabled) return false;
            else return true;
        }

        static bool unlockedEverything = false;
        [HarmonyPostfix]
        [HarmonyPatch(typeof(LocationManager), "OnLocationSettled")]
        public static void SkipTutorialOnArrival(LocationManager __instance)
        {
            if (GameManager.System.Player.tutorialStep < 2 && __instance.currentLocation.fullName == "Bar & Lounge" && BaseHunieModPlugin.CheatSpeedEnabled.Value)
            {
                GameManager.System.Player.tutorialStep = 2;
                GameManager.System.Player.money = 1000;
                CheatPatches.AddItem("Stuffed Bear",GameManager.System.Player.dateGifts);
                AddGreatGiftsToInventory();

            }
            if (!unlockedEverything)
            {
                unlockedEverything = true;
                foreach (GirlPlayerData girlData in GameManager.System.Player.girls)
                {
                    for (int i = 0; i <= 4; i++)
                    {
                        girlData.UnlockOutfit(i);
                        girlData.UnlockHairstyle(i);
                    }
                }
            }
        }

        //haha funny nude patch
        [HarmonyPrefix]
        [HarmonyPatch(typeof(Girl), "AddGirlPiece")]
        public static bool NudeTime(GirlPiece girlPiece)
        {
            if (girlPiece.type == GirlPieceType.OUTFIT && CheatPatches.awooga)
                return false;
            else
                return true;
        }
        [HarmonyPrefix]
        [HarmonyPatch(typeof(Girl), "AddGirlPieceArtToContainer")]
        public static bool NudeTime2(GirlPieceArt girlPieceArt, Girl __instance)
        {
            if ((girlPieceArt == __instance.definition.braPiece || girlPieceArt == __instance.definition.pantiesPiece) && CheatPatches.awooga)
                return false;
            else
                return true;
        }

        public static void RefreshGirls()
        {
            GameManager.Stage.girl.ShowGirl(GameManager.Stage.girl.definition);
            if (GameManager.Stage.altGirl.definition != null)
                GameManager.Stage.altGirl.ShowGirl(GameManager.Stage.altGirl.definition);
        }

        public static bool skipThisSetProcess = false;
        public static PuzzleToken highlightedToken;
        [HarmonyPostfix]
        [HarmonyPatch(typeof(PuzzleToken), "HighlightToken")]
        public static void SaveHighlightedToken(PuzzleToken __instance)
        {
            highlightedToken = __instance;
        }
        [HarmonyPostfix]
        [HarmonyPatch(typeof(PuzzleToken), "UnhighlightToken")]
        public static void DeleteHighlightedToken(PuzzleToken __instance)
        {
            if (__instance == highlightedToken)
                highlightedToken = null;
        }
        [HarmonyPrefix]
        [HarmonyPatch(typeof(UITop), "OnHuniebeeButtonPress")]
        public static bool PreventHuniebeeOnTokenChange()
        {
            if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl) && highlightedToken != null)
                return false;
            return true;
        }
        [HarmonyPrefix]
        [HarmonyPatch(typeof(PuzzleGame), "StartTokenMove")]
        public static bool ChangeTokenType(ref PuzzleToken token, PuzzleGame __instance)
        {
            if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))
            {
                // make power token
                if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
                {
                    token.IncLevel(1);
                    return false;
                }
                PuzzleGroup temp = new PuzzleGroup(new List<PuzzleGridPosition>() { token.gridPosition });
                for (int i = 0; i < tokenNames.Length; i++)
                {
                    if (tokenNames[i] + " Token" == token.definition.name) {
                        if (Input.GetMouseButton(0))
                        {
                            i++;
                            if (i == tokenNames.Length) i = 0;
                        }
                        else if (Input.GetMouseButton(1))
                        {
                            i--;
                            if (i < 0) i = tokenNames.Length - 1;
                        }
                        int newLevel = 1;
                        if (i < 4) newLevel = token.level;
                        List<PuzzleTokenDefinition> newDef = new List<PuzzleTokenDefinition>() { orderedTokens[i] };

                        skipThisSetProcess = true;
                        __instance.SwitchPuzzleGroupTokensWith(temp, newDef, newLevel, true);
                        

                        return false;
                    }
                }
            }
            return true;
        }
        [HarmonyPrefix]
        [HarmonyPatch(typeof(PuzzleGame), "ProcessMatchSet")]
        public static bool PreventMatchingAfterTypeSwitch()
        {
            if (skipThisSetProcess) return false;
            return true;
        }
        [HarmonyPrefix]
        [HarmonyPatch(typeof(PuzzleGame), "ProcessMatch")]
        public static bool PreventMatchingAfterTypeSwitch2()
        {
            if (skipThisSetProcess) return false;
            return true;
        }
        [HarmonyPostfix]
        [HarmonyPatch(typeof(PuzzleGame), "OnUpdate")]
        public static void PreventMatchingAfterTypeSwitch3()
        {
            skipThisSetProcess = false;
        }

    }
}
