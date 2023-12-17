using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using BepInEx.Logging;
using HarmonyLib;
using HarmonyLib.Tools;
using UnityEngine;
using Holoville;
using Holoville.HOTween;

namespace HunieMod
{
    // Token: 0x02000002 RID: 2
    public class BasePatches
    {
        public static SpriteObject updateSprite;

        public static DisplayObject ourContainer;
        public static LabelObject currentCategory;
        public static LabelObject currentDifficulty;
        public static LabelObject PBtext;
        public static LabelObject SOBtext;
        public static LabelObject seedText;

        public static BepInEx.Logging.ManualLogSource Logger = BepInEx.Logging.Logger.CreateLogSource("BasePatches");

        public static bool splitThisDate = false;
        public static bool replacingText = false;
        public static int startingCompletedGirls = 0;
        public static int startingRelationship = 0;
        public static bool dateIsProgress = false;

        public static bool titleScreenInteractive = true;

        [HarmonyPrefix]
        [HarmonyPatch(typeof(LocationManager), "OnLocationArrival")]
        public static void PostReturnNotification()
        {
            titleScreenInteractive = true; //reset title screen interactive here
            if (BaseHunieModPlugin.cheatsEnabled) GameUtil.ShowNotification(CellNotificationType.MESSAGE, "CHEATS ARE ENABLED");
            else if (BaseHunieModPlugin.hasReturned) GameUtil.ShowNotification(CellNotificationType.MESSAGE, "This is for practice purposes only");
        }

        public static void CheckFor100Complete()
        {
            RunTimer run = BaseHunieModPlugin.run;
            if (run == null) return;
            //checking for 100% completion
            if (run.goal == 100)
            {
                //SteamUtils.CheckPercentCompleteAchievement() recreation
                int num = 0;
                if (GameManager.System.Player.girls != null && GameManager.System.Player.girls.Count > 0)
                {
                    for (int i = 0; i < GameManager.System.Player.girls.Count; i++)
                    {
                        GirlPlayerData girlPlayerData = GameManager.System.Player.girls[i];
                        if (girlPlayerData.gotPanties && girlPlayerData.DetailKnownCount() == 12 && girlPlayerData.ItemCollectionCount() == 24 && girlPlayerData.UnlockedOutfitsCount() == 5)
                        {
                            num++;
                        }
                        else return;
                    }
                }
                if (num == 12)
                {
                    GameUtil.ShowNotification(CellNotificationType.MESSAGE, "100% complete! Time: " + RunTimer.convert(TimeSpan.FromTicks(DateTime.UtcNow.Ticks - run.runTimer)));
                    run.split();
                    string newSplit = "100%\n      " + run.splitText + "\n";
                    run.push(newSplit);
                    run.save();
                    //run.goal should no longer be 100% now, so this will only execute once
                }
            }
        }
        [HarmonyPostfix] [HarmonyPatch(typeof(GirlPlayerData), "KnowDetail")] public static void KnowDetail100() { CheckFor100Complete(); }
        [HarmonyPostfix] [HarmonyPatch(typeof(GirlPlayerData), "UnlockOutfit")] public static void UnlockOutfit100() { CheckFor100Complete(); }
        [HarmonyPostfix] [HarmonyPatch(typeof(GirlPlayerData), "AddPhotoEarned")] public static void AddPhoto100() { CheckFor100Complete(); }
        [HarmonyPostfix] [HarmonyPatch(typeof(GirlPlayerData), "AddItemToUniqueGifts")] public static void AddUnique100() { CheckFor100Complete(); }
        [HarmonyPostfix] [HarmonyPatch(typeof(GirlPlayerData), "AddItemToCollection")] public static void AddCollection100() { CheckFor100Complete(); }

        //handle starting Goon% and Intro%
        public static bool startingGooning = false;
        [HarmonyPrefix]
        [HarmonyPatch(typeof(LocationManager), "TravelTo")]
        public static bool StartGooning()
        {
            if (startingGooning) return true;
            RunTimer run = BaseHunieModPlugin.run;
            LocationDefinition bedroom = Definitions.GetLocation(HunieMod.LocationId.BedroomBonus);

            LocationDefinition introLocation = GameManager.Data.Locations.Get(BaseHunieModPlugin.IntroLocation.Value);
            if (run != null && !run.finishedRun) {
                if (run.chosenCategory == RunTimer.GOON)
                {
                    GameManager.System.Player.settingsDifficulty = (SettingsDifficulty)(run.chosenDifficulty - 1);
                    GameManager.System.Player.tutorialComplete = true;
                    GameManager.System.Player.cellphoneUnlocked = true;
                    AccessTools.Field(typeof(UITop), "_cellButtonDisabled").SetValue(GameManager.Stage.uiTop, false);
                    GameManager.Stage.uiTop.buttonHuniebee.button.Enable();
                    GameManager.Stage.uiTop.buttonHuniebee.SetAlpha(1);

                    //randomly choose goon target
                    GirlDefinition goonGirl = Definitions.GetGirl(GirlId.Kyu);
                    if (BaseHunieModPlugin.goonChoice != -1) goonGirl = Definitions.Girls[BaseHunieModPlugin.goonChoice];
                    else
                    {
                        List<GirlDefinition> girls = new List<GirlDefinition>();
                        for (int i = 0; i < Definitions.Girls.Count; i++)
                        {
                            GirlDefinition g = Definitions.Girls[i];
                            if (BaseHunieModPlugin.goonPreferences[g.firstName].Value) girls.Add(g);
                        }
                        if (girls.Count > 0)
                        {
                            ListUtils.Shuffle<GirlDefinition>(girls);
                            goonGirl = girls[0];
                        }
                    }
                    foreach (GirlPlayerData girlData in GameManager.System.Player.girls)
                    {
                        girlData.metStatus = GirlMetStatus.MET;
                        girlData.dayDated = true;
                    }
                    startingGooning = true;
                    GameManager.System.Puzzle.TravelToPuzzleLocation(bedroom, goonGirl);
                    startingGooning = false;
                    return false;
                }
                else if (run.chosenCategory == RunTimer.INTRO)
                {
                    GameManager.System.Location.currentLocation = Definitions.GetLocation(LocationId.Bedroom);
                    GameManager.System.Player.tutorialStep = 1;
                    startingGooning = true;
                    GameManager.System.Puzzle.TravelToPuzzleLocation(introLocation, Definitions.GetGirl(GirlId.Kyu));
                    startingGooning = false;
                    return false;
                }
            }
            return true;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(PuzzleGame), "OnUpdate")]
        public static void AutosplitHelp(PuzzleGame __instance, ref int ____goalAffection, ref bool ____victory, ref bool ____isBonusRound)
        {
            RunTimer run = BaseHunieModPlugin.run;
            //allow splits to happen with cheats/has returned, as long as we don't auto-start with those on it's fine
            //if (BaseHunieModPlugin.cheatsEnabled || BaseHunieModPlugin.hasReturned) return;
            if (__instance.currentDisplayAffection == 0)
            {
                if (run != null) {
                    if (run.finalRunDisplay == "" && BaseHunieModPlugin.swimsuitsChosen >= 8)
                    {
                        Logger.LogMessage("Insane amount of swimsuits detected");
                        run.push("WARNING! This runner is extremely horny!\n\n");
                    }
                    if ((run.chosenCategory == RunTimer.INTRO || run.chosenCategory == RunTimer.GOON) && !run.finishedRun && ____goalAffection < 10000)
                    {
                        ____goalAffection = 10000;
                        GameManager.System.Puzzle.Game.AddResourceValue(PuzzleGameResourceType.AFFECTION, 0, false);
                    }
                }
                startingCompletedGirls = GameManager.System.Player.GetTotalMaxRelationships();
                startingRelationship = GameManager.System.Player.GetGirlData(GameManager.Stage.girl.definition).relationshipLevel;
                dateIsProgress = false;
                // The date will cause progress if our relationship is under 5, or it's a panties-grabbing bonus round
                if (startingRelationship < 5 || (____isBonusRound && !GameManager.System.Player.GetGirlData(GameManager.Stage.girl.definition).gotPanties))
                    dateIsProgress = true;
                    //Logger.LogMessage(GameManager.System.Player.GetTotalMaxRelationships() + "," + GameManager.System.Player.GetTotalGirlsRelationshipLevel());
                //auto category detection
                if (run != null && !BaseHunieModPlugin.cheatsEnabled && BaseHunieModPlugin.lastChosenCategory == RunTimer.ANYCATEGORY && GameManager.System.Player.tutorialComplete)
                {
                    if (run.switchedCategory == false && GameManager.System.Player.GetTotalGirlsRelationshipLevel() == 0)
                    {
                        string girlName = GameManager.Stage.girl.definition.firstName;
                        if (girlName == "Aiko")
                            run.chosenCategory = RunTimer.GETLAID;
                        else if (girlName == "Audrey")
                            run.chosenCategory = RunTimer.GETLAIDKYU;
                        else
                            run.chosenCategory = RunTimer.ALLMAINGIRLS;
                        run.refresh();
                        run.switchedCategory = true;
                    }
                    // switch to Get Laid Kyu after a Get Laid run finished and we date Kyu
                    else if (run.chosenCategory == RunTimer.GETLAID && run.category == "" && GameManager.Stage.girl.definition.firstName == "Kyu")
                    {
                        run.chosenCategory = RunTimer.GETLAIDKYU;
                        run.finishedRun = false;
                        //run.chosenDifficulty = (int)GameManager.System.Player.settingsDifficulty + 1;
                        run.refresh();
                    }
                    // switch to All Main Girls after an Aiko start if you initiate a non-Aiko date
                    else if (run.chosenCategory == RunTimer.GETLAID && GameManager.Stage.girl.definition.firstName != "Aiko")
                    {
                        run.chosenCategory = RunTimer.ALLMAINGIRLS;
                        run.refresh();
                    }
                    // switch to All Panties if you've initiated a date with a secret girl
                    else if (run.chosenCategory == RunTimer.ALLMAINGIRLS && GameManager.Stage.girl.definition.secretGirl)
                    {
                        run.chosenCategory = RunTimer.ALLPANTIES;
                        run.refresh();
                    }
                }
            }
            if (run != null && (run.chosenCategory == RunTimer.INTRO || run.chosenCategory == RunTimer.GOON))
            {
                bool didSplit = false;
                bool finalSplit = false;
                if (run.splits.Count == 9 && __instance.currentDisplayAffection == ____goalAffection && ____victory)
                {
                    finalSplit = run.split(true);
                }
                else if (run.splits.Count < 9 && __instance.currentDisplayAffection >= 1000 * (run.splits.Count+1))
                {
                    didSplit = run.split(false);
                }
                if (finalSplit || didSplit)
                {
                    if (finalSplit)
                    {
                        RunTimerPatches.initialTimerDelay.Start();
                    }
                    else
                    {
                        RunTimerPatches.preventAllUpdate = true;
                        RunTimerPatches.revertDiffDelay.Start();
                    }
                    
                    RunTimerPatches.isBonusRound = false;

                    string newSplit = run.splits.Count + ",000 Points";
                    newSplit += "\n      " + run.splitText + "\n";
                    run.push(newSplit);

                    if (finalSplit)
                        RunTimerPatches.savePBDelay.Start();
                }
            }
            else if (__instance.currentDisplayAffection == ____goalAffection && (____victory || ____isBonusRound))
            {
                //if a timer is running, split
                if (!splitThisDate && run != null)
                {
                    bool didSplit = false;
                    //don't split for dates in postgame
                    if (dateIsProgress || run.category == "")
                    {
                        //check our rules
                        if (run.goal <= 2 || BaseHunieModPlugin.SplitRules.Value <= 0) didSplit = run.split(____isBonusRound);
                        else if (BaseHunieModPlugin.SplitRules.Value == 1 && !____isBonusRound) didSplit = run.split(____isBonusRound);
                        else if (BaseHunieModPlugin.SplitRules.Value == 2 && ____isBonusRound) didSplit = run.split(____isBonusRound);
                        //check for final split regardless of option
                        //technically someone could just repeat sex with a girl to trigger this split. if they do that, too bad
                        else if (____isBonusRound && (run.goal == startingCompletedGirls + 1))
                            didSplit = run.split(____isBonusRound);
                    }
                    if (didSplit)
                    {
                        RunTimerPatches.initialTimerDelay.Start();
                        //if (!____isBonusRound) RunTimerPatches.revertDiffDelay.Start();
                        RunTimerPatches.isBonusRound = ____isBonusRound;
                        int dateNum = startingRelationship;

                        string newSplit = GameManager.Stage.girl.definition.firstName + " #" + dateNum;
                        if (GameManager.Stage.girl.definition.firstName == "Kyu" && startingCompletedGirls == 0) newSplit = "Tutorial";
                        else if (____isBonusRound) newSplit = GameManager.Stage.girl.definition.firstName + " Bonus";

                        newSplit += "\n      " + run.splitText + "\n";
                        run.push(newSplit);

                        if ((____isBonusRound && run.goal == startingCompletedGirls + 1) || run.goal == 10000)
                        {
                            //run.save();
                            RunTimerPatches.savePBDelay.Start();
                        }

                        string simTime = ((int)RunTimerPatches.simTime / 60) + ":" + ((int)RunTimerPatches.simTime % 60).ToString("D2");
                        string dateTime = ((int)RunTimerPatches.dateTime / 60) + ":" + ((int)RunTimerPatches.dateTime % 60).ToString("D2");
                        Logger.LogMessage("Sim time after " + newSplit + ": " + simTime);
                        Logger.LogMessage("Date time after " + newSplit + ": " + dateTime);
                    }
                }

                splitThisDate = true;
            }
            else
            {
                replacingText = false;
                splitThisDate = false;
            }
        }

        /*
        [HarmonyPrefix]
        [HarmonyPatch(typeof(DialogManager), "DialogSceneStep")]
        public static void DialogUpdate(DialogManager __instance, List<DialogSceneStepsProgress> ____activeDialogSceneSteps)
        {
            DialogSceneStepsProgress activeDialogSceneSteps = ____activeDialogSceneSteps[____activeDialogSceneSteps.Count - 1];
            Logger.LogDebug("pre-dialog scene step! " + ____activeDialogSceneSteps.Count + " steps, " + activeDialogSceneSteps.stepIndex.ToString());

            //Logger.LogDebug("dialog update: " + ____proceedToNextStep.ToString() + "," + ____activeDialogScene.ToString());
        }
        [HarmonyPostfix]
        [HarmonyPatch(typeof(DialogManager), "DialogSceneStep")]
        public static void DialogUpdate2(DialogManager __instance, List<DialogSceneStepsProgress> ____activeDialogSceneSteps)
        {
            DialogSceneStepsProgress activeDialogSceneSteps = ____activeDialogSceneSteps[____activeDialogSceneSteps.Count - 1];
            Logger.LogDebug("post-dialog scene step! " + ____activeDialogSceneSteps.Count + " steps, " + activeDialogSceneSteps.stepIndex.ToString());

            //Logger.LogDebug("dialog update: " + ____proceedToNextStep.ToString() + "," + ____activeDialogScene.ToString());
        }
        [HarmonyPrefix]
        [HarmonyPatch(typeof(DialogManager), "PlayDialogScene")]
        public static void DialogUpdate4(DialogManager __instance, List<DialogSceneStepsProgress> ____activeDialogSceneSteps)
        { Logger.LogDebug("playdialogscene!"); }
        [HarmonyPrefix]
        [HarmonyPatch(typeof(DialogManager), "OnGirlDialogLineRead")]
        public static void DialogUpdate5(DialogManager __instance, List<DialogSceneStepsProgress> ____activeDialogSceneSteps)
        { Logger.LogDebug("OnGirlDialogLineRead!"); }
        [HarmonyPrefix]
        [HarmonyPatch(typeof(DialogManager), "OnResponseOptionSelected")]
        public static void DialogUpdate6(DialogManager __instance, List<DialogSceneStepsProgress> ____activeDialogSceneSteps)
        { Logger.LogDebug("OnResponseOptionSelected!"); }


        [HarmonyPostfix]
        [HarmonyPatch(typeof(DialogManager), "Update")]
        public static void DialogUpdate3(DialogManager __instance, DialogSceneDefinition ____activeDialogScene, List<DialogSceneStepsProgress> ____activeDialogSceneSteps,
            Sequence ____dialogSceneSequence, Timer ____waitTimer)
        {
            return;
            string thelog = "active scene: ";
            if (____activeDialogScene != null)
            {
                thelog += ____activeDialogScene.ToString() + " with steps " + ____activeDialogScene.steps.Count.ToString();
            }
            thelog += ", active scene steps count: ";
            if (____activeDialogSceneSteps != null)
            {
                thelog += ____activeDialogSceneSteps.Count;
            }
            thelog += ", sequence elapsed: ";
            if (____dialogSceneSequence != null)
            {
                thelog += ____dialogSceneSequence.elapsed.ToString();
            }
            thelog += ", waittimer: ";
            if (____waitTimer != null)
            {
                thelog += ____waitTimer.duration.ToString();
            }
            Logger.LogDebug(thelog);
            //DialogSceneStepsProgress activeDialogSceneSteps = ____activeDialogSceneSteps[____activeDialogSceneSteps.Count - 1];
            //Logger.LogDebug("post-dialog scene step! " + ____activeDialogSceneSteps.Count + " steps, " + activeDialogSceneSteps.stepIndex.ToString());

            //Logger.LogDebug("dialog update: " + ____proceedToNextStep.ToString() + "," + ____activeDialogScene.ToString());
        }
        */
        /*
        [HarmonyPostfix]
        [HarmonyPatch(typeof(DialogManager), "Update")]
        public static void DialogUpdate2(DialogManager __instance, DialogSceneDefinition ____activeDialogScene, List<DialogSceneStepsProgress> ____activeDialogSceneSteps,
            Sequence ____dialogSceneSequence, Timer ____waitTimer)
        {
            TalkWindow test = GameManager.Stage.uiWindows.GetActiveWindow() as TalkWindow;

            //object test2 = AccessTools.Field(typeof(Girl), "DialogLineReadEvent").GetValue(GameManager.Stage.girl);
            object test2 = AccessTools.Field(typeof(Girl), "_currentDialogLine").GetValue(GameManager.Stage.girl);
            test2 = AccessTools.Field(typeof(UITop), "CellPhoneClosedEvent").GetValue(GameManager.Stage.uiTop);
            test2 = AccessTools.Field(typeof(PuzzleGame), "PuzzleGameReadyEvent").GetValue(GameManager.System.Puzzle.Game);
            //
            if (test2 != null)
            {
                Logger.LogDebug(test2.ToString());
            }
            else
            {
                Logger.LogDebug("null");
            }
            if (test != null)
            {
                Logger.LogDebug(test.ToString());
            }
            else Logger.LogDebug("we are in update withi no active window");
            //DialogSceneStepsProgress activeDialogSceneSteps = ____activeDialogSceneSteps[____activeDialogSceneSteps.Count - 1];
            //Logger.LogDebug("post-dialog scene step! " + ____activeDialogSceneSteps.Count + " steps, " + activeDialogSceneSteps.stepIndex.ToString());

            //Logger.LogDebug("dialog update: " + ____proceedToNextStep.ToString() + "," + ____activeDialogScene.ToString());
        }
        [HarmonyPostfix]
        [HarmonyPatch(typeof(DialogManager), "OnResponseOptionSelected")]
        public static void DialogUpdate3(DialogManager __instance, DialogSceneDefinition ____activeDialogScene, List<DialogSceneStepsProgress> ____activeDialogSceneSteps,
            Sequence ____dialogSceneSequence, Timer ____waitTimer)
        {
            
            Logger.LogDebug("we are in onresponseoptionselected");
            //DialogSceneStepsProgress activeDialogSceneSteps = ____activeDialogSceneSteps[____activeDialogSceneSteps.Count - 1];
            //Logger.LogDebug("post-dialog scene step! " + ____activeDialogSceneSteps.Count + " steps, " + activeDialogSceneSteps.stepIndex.ToString());

            //Logger.LogDebug("dialog update: " + ____proceedToNextStep.ToString() + "," + ____activeDialogScene.ToString());
        }
        */

        /*
        [HarmonyPrefix]
        [HarmonyPatch(typeof(PuzzleGame), "OnUpdate")]
        public static void TestingDrain(PuzzleGame __instance, bool ____isBonusRound, float ____bonusRoundDrainTimestamp, float ____bonusRoundDrainDelay, int ____currentAffection)
        {
            //Logger.LogDebug("drain delay is: " + ____bonusRoundDrainDelay.ToString());
            float diff = GameManager.System.Lifetime(true) - ____bonusRoundDrainTimestamp;
            //Logger.LogDebug(diff.ToString());
            //return;
            if (____isBonusRound && GameManager.System.Lifetime(true) - ____bonusRoundDrainTimestamp >= ____bonusRoundDrainDelay &&
                __instance.puzzleGameState != PuzzleGameState.FINISHED && __instance.puzzleGameState != PuzzleGameState.COMPLETE)
            {
                Logger.LogDebug(diff.ToString() + " , " + ____currentAffection.ToString());
            }
        }
        */

        [HarmonyPostfix]
        [HarmonyPatch(typeof(PuzzleGame), "Begin")]
        public static void Version1Drain(PuzzleGame __instance, ref float ____bonusRoundDrainDelay)
        {
            int version = BaseHunieModPlugin.GameVersion(); //0 = jan23, 1 = valentines
            if (version == 1 || BaseHunieModPlugin.V1Drain.Value == false) return;

            ____bonusRoundDrainDelay = 0.05f - 0.0012f * (float)GameManager.System.Player.GetTotalMaxRelationships();
            if (GameManager.System.Player.settingsDifficulty == SettingsDifficulty.EASY)
            {
                ____bonusRoundDrainDelay += 0.01f;
            }
            else if (GameManager.System.Player.settingsDifficulty == SettingsDifficulty.HARD)
            {

                ____bonusRoundDrainDelay -= 0.01f;

            }
        }

        public static void ThingsToDoWhenGameStarts()
        {
            titleScreenInteractive = false;
            if (BaseHunieModPlugin.seedMode)
            {
                if (BaseHunieModPlugin.seedString != "") RNGPatches.InitializeRNG(int.Parse(BaseHunieModPlugin.seedString));
                else RNGPatches.InitializeRNG(int.Parse(BaseHunieModPlugin.defaultSeed));
            }
            else
            {
                RNGPatches.WipeRNG();
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(LoadScreen), "OnStartGameMale")]
        public static void MaleStart()
        {
            ThingsToDoWhenGameStarts();
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(LoadScreen), "OnStartGameFemale")]
        public static void FemaleStart()
        {
            ThingsToDoWhenGameStarts();
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(LoadScreen), "OnContinueGame")]
        public static void ContinueGame(ref int saveFileIndex)
        {
            ThingsToDoWhenGameStarts();
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(LoadScreen), "OnCreditsButtonPressed")]
        public static bool UpdateTime()
        {
            if (!BaseHunieModPlugin.newVersionAvailable) return true;

            if (BaseHunieModPlugin.GameVersion() == BaseHunieModPlugin.JAN23)
                System.Diagnostics.Process.Start("https://drive.google.com/u/0/uc?id=1qjLj9fB86nIhd5KERbwDrkZE16-4ItHM&export=download");
            else
                System.Diagnostics.Process.Start("https://drive.google.com/u/0/uc?id=1p7uv5mO-fYAO2wUgp_G3x-EWbzbLBtk8&export=download");
            System.Diagnostics.Process.Start(Directory.GetCurrentDirectory());
            GameUtil.QuitGame(false);

            return false;
        }

        private static void DecreaseDiff(ButtonObject buttonObject)
        {
            BaseHunieModPlugin.lastChosenDifficulty--;
            if (BaseHunieModPlugin.lastChosenDifficulty < 0) BaseHunieModPlugin.lastChosenDifficulty = RunTimer.difficulties.Length - 1;
            RunTimerPatches.UpdateFiles();
        }
        private static void IncreaseDiff(ButtonObject buttonObject)
        {
            BaseHunieModPlugin.lastChosenDifficulty++;
            if (BaseHunieModPlugin.lastChosenDifficulty >= RunTimer.difficulties.Length) BaseHunieModPlugin.lastChosenDifficulty = 0;
            RunTimerPatches.UpdateFiles();
        }
        private static void DecreaseCat(ButtonObject buttonObject)
        {
            BaseHunieModPlugin.lastChosenCategory--;
            if (BaseHunieModPlugin.lastChosenCategory < 0) BaseHunieModPlugin.lastChosenCategory = RunTimer.categories.Length - 1;
            RunTimerPatches.UpdateFiles();
        }
        private static void IncreaseCat(ButtonObject buttonObject)
        {
            BaseHunieModPlugin.lastChosenCategory++;
            if (BaseHunieModPlugin.lastChosenCategory >= RunTimer.categories.Length) BaseHunieModPlugin.lastChosenCategory = 0;
            RunTimerPatches.UpdateFiles();
        }
        private static void InitializeOurThing(SpriteObject obj, DisplayObject container, float X, float Y)
        {
            obj.localX = X;
            obj.localY = Y;
            obj.SetAlpha(0);
            container.AddChild(obj);
        }
        private static void InitializeOurThing(LabelObject obj, DisplayObject container, float X, float Y)
        {
            obj.localX = X;
            obj.localY = Y;
            obj.label.color = new Color(1, 1, 1, 0);
            obj.label.anchor = TextAnchor.MiddleCenter;
            container.AddChild(obj);
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(LoadScreen), "Init")]
        public static void UpdateButtonAndVersionInfo(LoadScreen __instance, ref SpriteObject ____creditsButton, ref UICellApp ____settingsCellApp)
        {
            //add mod version and settings info to the corner (or just edit the text if on Valentine's)
            int version = BaseHunieModPlugin.GameVersion(); //0 = jan23, 1 = valentines
            string[] v = { "Jan. 23", "Valentine's" };
            Logger.LogMessage("Game Version " + v[version]);
            string newText1 = "Speedrun Mod " + BaseHunieModPlugin.PluginVersion + " (" + v[version] + ")";
            string newText2 = "";
            if (BaseHunieModPlugin.VsyncEnabled.Value)
                newText2 += "Vsync On (" + Screen.currentResolution.refreshRate + ")";
            else
                newText2 += BaseHunieModPlugin.FramerateCap.Value + " FPS Lock";

            if (BaseHunieModPlugin.V1Drain.Value && version == 0)
                newText2 += ", V1.0 Drain";

            ourContainer = UnityEngine.Object.Instantiate(__instance.buttonContainer) as DisplayObject;
            ourContainer.RemoveAllChildren();
            __instance.buttonContainer.AddChild(ourContainer);

            if (version == 0)
            {
                //create our own label in the bottom-left
                LabelObject labelObject = UnityEngine.Object.Instantiate(__instance.saveFiles[0].dataLocationLabel) as LabelObject;
                labelObject.active = true;
                labelObject.label.color = new Color(142 / 255f, 123 / 255f, 152 / 255f, 0f);
                labelObject.label.scale = new Vector3(0.67f, 0.67f);
                //labelObject.label.font.lineHeight = 27;
                labelObject.label.maxChars = 999;

                labelObject.SetText(newText1 + "\n" + newText2);
                labelObject.localX = 10f;
                labelObject.localY = 28f;
                ourContainer.AddChild(labelObject);
                //__instance.buttonContainer.AddChild(labelObject);
                //labelObject.ShiftSelfToTop();
                
            }
            else
            {
                LabelObject ____noteVersion = (__instance.GetChildByName("LoadScreenNotes").GetChildByName("LoadScreenNoteVersion") as LabelObject);
                LabelObject ____noteCensor = (__instance.GetChildByName("LoadScreenNotes").GetChildByName("LoadScreenNoteCensor") as LabelObject);
                ____noteVersion.SetText(newText1);
                ____noteCensor.SetText(newText2);
            }

            if (BaseHunieModPlugin.InGameTimer.Value)
            {
                //add run timer stuff
                //if run setting is on
                SettingsCellApp settingsApp = (SettingsCellApp)____settingsCellApp;
                SettingsMeter meter = ((List<SettingsMeter>)AccessTools.Field(typeof(SettingsCellApp), "_settingsMeters").GetValue(settingsApp))[0];
                SpriteObject leftArrow = UnityEngine.Object.Instantiate(
                    (SpriteObject)AccessTools.Field(typeof(SettingsMeter), "_leftArrow").GetValue(meter)) as SpriteObject;
                SpriteObject rightArrow = UnityEngine.Object.Instantiate(
                    (SpriteObject)AccessTools.Field(typeof(SettingsMeter), "_rightArrow").GetValue(meter)) as SpriteObject;
                SpriteObject leftArrow2 = UnityEngine.Object.Instantiate(
                    (SpriteObject)AccessTools.Field(typeof(SettingsMeter), "_leftArrow").GetValue(meter)) as SpriteObject;
                SpriteObject rightArrow2 = UnityEngine.Object.Instantiate(
                    (SpriteObject)AccessTools.Field(typeof(SettingsMeter), "_rightArrow").GetValue(meter)) as SpriteObject;
                currentDifficulty = UnityEngine.Object.Instantiate(__instance.saveFiles[0].dataLocationLabel) as LabelObject;
                currentCategory = UnityEngine.Object.Instantiate(__instance.saveFiles[0].dataLocationLabel) as LabelObject;
                PBtext = UnityEngine.Object.Instantiate(__instance.saveFiles[0].dataLocationLabel) as LabelObject;
                SOBtext = UnityEngine.Object.Instantiate(__instance.saveFiles[0].dataLocationLabel) as LabelObject;
                seedText = UnityEngine.Object.Instantiate(__instance.saveFiles[0].dataLocationLabel) as LabelObject;
                int ypos = 155; int yoffset = 20;
                int xpos = 600; int offset = 85; int diff = 120;
                //centered vertically, large difference horizontally
                /*InitializeOurThing(leftArrow, __instance.buttonContainer, xpos - offset - diff, ypos);
                InitializeOurThing(currentCategory, __instance.buttonContainer, xpos - offset - 5, ypos);
                InitializeOurThing(rightArrow, __instance.buttonContainer, xpos - offset + diff, ypos);
                InitializeOurThing(leftArrow2, __instance.buttonContainer, xpos + offset - diff, ypos);
                InitializeOurThing(currentDifficulty, __instance.buttonContainer, xpos + offset - 5, ypos);
                InitializeOurThing(rightArrow2, __instance.buttonContainer, xpos + offset + diff, ypos);*/
                //centered horizontally, stacked on top of each other
                InitializeOurThing(leftArrow, ourContainer, xpos - diff, ypos + yoffset);
                InitializeOurThing(currentCategory, ourContainer, xpos - 5, ypos + yoffset);
                InitializeOurThing(rightArrow, ourContainer, xpos + diff, ypos + yoffset);
                InitializeOurThing(leftArrow2, ourContainer, xpos - diff, ypos - yoffset);
                InitializeOurThing(currentDifficulty, ourContainer, xpos - 5, ypos - yoffset);
                InitializeOurThing(rightArrow2, ourContainer, xpos + diff, ypos - yoffset);
                InitializeOurThing(PBtext, ourContainer, xpos - offset - 5, ypos - yoffset * 3);
                InitializeOurThing(SOBtext, ourContainer, xpos + offset - 5, ypos - yoffset * 3);
                InitializeOurThing(seedText, ourContainer, xpos + offset * 4 - 5, ypos);
                seedText.SetText("");
                SOBtext.label.color = new Color(221 / 256f, 175 / 256f, 76 / 256f, 0);
                currentDifficulty.SetText(RunTimer.difficulties[BaseHunieModPlugin.lastChosenDifficulty]);
                currentCategory.SetText(RunTimer.categories[BaseHunieModPlugin.lastChosenCategory]);
                PBtext.SetText("PB: " + RunTimer.GetPB(BaseHunieModPlugin.lastChosenCategory, BaseHunieModPlugin.lastChosenDifficulty));
                SOBtext.SetText("SoB: " + RunTimer.GetGolds(BaseHunieModPlugin.lastChosenCategory, BaseHunieModPlugin.lastChosenDifficulty));
                leftArrow.button.ButtonPressedEvent += DecreaseCat;
                rightArrow.button.ButtonPressedEvent += IncreaseCat;
                leftArrow2.button.ButtonPressedEvent += DecreaseDiff;
                rightArrow2.button.ButtonPressedEvent += IncreaseDiff;
            }

            //replace Credits button with Update button
            if (!BaseHunieModPlugin.newVersionAvailable) return;
            SpriteObject spr = GameUtil.ImageFileToSprite("update.png", "updatesprite");

            if (spr != null)
            {
                ____creditsButton.SetLightness(0f);
                ____creditsButton.AddChild(spr);

                updateSprite = ____creditsButton.GetChildren(true)[____creditsButton.GetChildren().Length - 1] as SpriteObject;
                updateSprite.SetLocalPosition(-83, 24);
                updateSprite.SetOwnChildIndex(3);
                updateSprite.spriteAlpha = 0f;
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(LoadScreen), "Refresh")]
        public static void UpdateOurLabelPos(LoadScreen __instance, ref SpriteObject ____creditsButton, ref UICellApp ____settingsCellApp)
        {
            if (ourContainer != null)
            {
                float xoffset = 513f - __instance.buttonContainer.localX;
                ourContainer.localX = xoffset;
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(AudioManager), "Play", typeof(AudioCategory), typeof(AudioDefinition), typeof(bool), typeof(float), typeof(bool))]
        public static void ReplaceAnySFX(AudioDefinition audioDefinition, float volume)
        {
            if (audioDefinition == null || audioDefinition.clip == null) return;
            AudioClip newSFX;
            if (BaseHunieModPlugin.customSFX.TryGetValue(audioDefinition.clip.name, out newSFX))
            {
                AudioSource audioSource = GameManager.System.gameCamera.gameObject.AddComponent("AudioSource") as AudioSource;
                audioSource.clip = newSFX;
                audioSource.volume *= GameManager.System.settingsSoundVol / 10f;
                audioSource.Play();
            }
        }
        
        [HarmonyPostfix]
        [HarmonyPatch(typeof(UIPhotoGallery), "ShowPhotoGallery")]
        public static void PlayFunnyCG4SFX(GirlDefinition initialPhotoGirl, bool singlePhotoMode)
        {
            if (singlePhotoMode)
            {
                AudioClip newSFX;
                if (BaseHunieModPlugin.climaxSFX.TryGetValue(initialPhotoGirl.firstName.ToLower(), out newSFX))
                {
                    AudioSource audioSource = GameManager.System.gameCamera.gameObject.AddComponent("AudioSource") as AudioSource;
                    audioSource.clip = newSFX;
                    audioSource.volume *= GameManager.System.settingsSoundVol / 10f;
                    audioSource.Play();
                }
            }
        }

        static int tempHS = 0;
        static int tempOF = 0;
        static bool inShowGirl = false;

        [HarmonyPrefix]
        [HarmonyPatch(typeof(Girl), "ShowGirl")]
        public static void SetOutfitOptions(GirlDefinition girlDefinition)
        {
            if (BaseHunieModPlugin.cheatsEnabled) return;
            inShowGirl = true;
            GirlPlayerData girlData = GameManager.System.Player.GetGirlData(girlDefinition);
            tempHS = girlData.hairstyle;
            tempOF = girlData.outfit;
            if (girlData.hairstyle == 0)
            {
                girlData.hairstyle = BaseHunieModPlugin.hairstylePreferences[girlDefinition.firstName].Value;
            }
            if (girlData.outfit == 0)
            {
                girlData.outfit = BaseHunieModPlugin.outfitPreferences[girlDefinition.firstName].Value;
            }
        }
        [HarmonyPostfix]
        [HarmonyPatch(typeof(Girl), "ShowGirl")]
        public static void RevertOutfitOptions(GirlDefinition girlDefinition)
        {
            if (BaseHunieModPlugin.cheatsEnabled) return;
            inShowGirl = false;
            GirlPlayerData girlData = GameManager.System.Player.GetGirlData(girlDefinition);
            girlData.hairstyle = tempHS;
            girlData.outfit = tempOF;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(GirlPlayerData), "GetRandomUnlockedOutfit")]
        public static bool AllowAnyRandom(ref int __result)
        {
            if (!inShowGirl) return true;
            __result = UnityEngine.Random.Range(0, 4);
            return false;
        }
        [HarmonyPrefix]
        [HarmonyPatch(typeof(GirlPlayerData), "GetRandomUnlockedHairstyle")]
        public static bool AllowAnyRandom2(ref int __result)
        {
            if (!inShowGirl) return true;
            __result = UnityEngine.Random.Range(0, 4);
            return false;
        }

        //replace when girls are forced to be shown in their default styles
        [HarmonyPrefix]
        [HarmonyPatch(typeof(DialogManager), "ShowMainGirl")]
        public static void ShowMainGirlStyles(ref string styles)
        {
            if (styles == "8,13") styles = "";
        }
        [HarmonyPrefix]
        [HarmonyPatch(typeof(DialogManager), "ShowAltGirl")]
        public static void ShowAltGirlStyles(ref string styles)
        {
            if (styles == "8,13") styles = "";
        }

        /*
        [HarmonyPrefix]
        [HarmonyPatch(typeof(GirlManager), "TalkWithHer")]
        public static void TestingTalking()
        {

            Girl ____activeGirl = GameManager.Stage.girl;

            Logger.LogMessage("hi");
            GirlPlayerData girlData = GameManager.System.Player.GetGirlData(____activeGirl.definition);
            //List<int> collection = (List<int>)AccessTools.Field(typeof(GirlPlayerData), "_collection").GetValue(girlData);
            //collection.RemoveAt(5);
            bool[] details = (bool[])AccessTools.Field(typeof(GirlPlayerData), "_details").GetValue(girlData);
            details[1] = false;

            List<DialogSceneDefinition> list = new List<DialogSceneDefinition>();
            for (int i = 0; i < ____activeGirl.definition.talkQueries.Count; i++)
            {
                if (GameManager.System.GameLogic.GameConditionsMet(____activeGirl.definition.talkQueries[i].conditions, false))
                {
                    list.Add(____activeGirl.definition.talkQueries[i]);
                    list.Add(____activeGirl.definition.talkQueries[i]);
                    list.Add(____activeGirl.definition.talkQueries[i]);
                    if (girlData.gotPanties && GameManager.System.Player.endingSceneShown)
                    {
                        list.Add(____activeGirl.definition.talkQueries[i]);
                        list.Add(____activeGirl.definition.talkQueries[i]);
                        list.Add(____activeGirl.definition.talkQueries[i]);
                    }
                }
            }
            for (int j = 0; j < ____activeGirl.definition.talkQuizzes.Count; j++)
            {
                if (GameManager.System.GameLogic.GameConditionsMet(____activeGirl.definition.talkQuizzes[j].conditions, true) && !girlData.IsRecentQuiz(j))
                {
                    list.Add(____activeGirl.definition.talkQuizzes[j]);
                }
            }
            for (int k = 0; k < ____activeGirl.definition.talkQuestions.Count; k++)
            {
                if (GameManager.System.GameLogic.GameConditionsMet(____activeGirl.definition.talkQuestions[k].conditions, true) && !girlData.IsRecentQuestion(k))
                {
                    list.Add(____activeGirl.definition.talkQuestions[k]);
                }
            }

            Logger.LogMessage("hi 2");
            foreach (DialogSceneDefinition d in list)
            {
                //Logger.LogMessage(d.steps[0].messageDef.messageText);
                foreach (DialogSceneStep ds in d.steps)
                {
                    if (ds.responseOptions != null)
                    {
                        foreach (DialogSceneResponseOption dsr in ds.responseOptions)
                        {
                            Logger.LogMessage(dsr.text);
                        }
                    }
                }
                Logger.LogMessage(d.id.ToString());
            }

            foreach (GirlDefinition girl in HunieMod.Definitions.Girls)
            {
                Logger.LogMessage(girl.firstName);
                foreach (DialogSceneDefinition d in girl.talkQuestions)
                {
                    foreach (GameCondition gc in d.conditions)
                    {
                        Logger.LogMessage(gc.type.ToString());
                        if (gc.girlDefinition != null) Logger.LogMessage(gc.girlDefinition.firstName);
                        if (gc.locationDefinition != null) Logger.LogMessage(gc.locationDefinition.fullName);
                        if (gc.girlDetailType != null) Logger.LogMessage(gc.girlDetailType.ToString());
                        if (gc.relationshipLevel != null) Logger.LogMessage(gc.relationshipLevel.ToString());
                        if (gc.metStatus != null) Logger.LogMessage(gc.metStatus.ToString());

                        foreach (DialogSceneStep ds in d.steps)
                        {
                            if (ds.responseOptions != null)
                            {
                                foreach (DialogSceneResponseOption dsr in ds.responseOptions)
                                {
                                    Logger.LogMessage(dsr.text);
                                }
                            }
                        }
                        Logger.LogMessage(d.id.ToString());
                    }
                    //Logger.LogMessage(d.steps[0].messageDef.messageText);
                    
                }
            }
        }
        */

        /*[HarmonyPrefix]
        [HarmonyPatch(typeof(PuzzleGame), "OnUpdate")]
        public static void testingthing2(PuzzleGame __instance)
        {
            Logger.LogMessage(__instance.puzzleGameState.ToString());
        }
        */
        /*
        static bool logGetMatch = false;
        [HarmonyPrefix]
        [HarmonyPatch(typeof(PuzzleGame), "CreateToken")]
        public static void testingthing2(PuzzleGame __instance, int col)
        {
            Logger.LogMessage("testing creating token in column " + col);
            logGetMatch = true;
        }
        [HarmonyPostfix]
        [HarmonyPatch(typeof(PuzzleGame), "CreateToken")]
        public static void testingthing4(PuzzleGame __instance, int col)
        {
            Logger.LogMessage("finished testing");
            logGetMatch = false;
        }
        [HarmonyPostfix]
        [HarmonyPatch(typeof(PuzzleGame), "GetMatch")]
        public static void testingthing3(PuzzleGame __instance, PuzzleGridPosition gridPosition, PuzzleTokenDefinition tokenDefinitionOverride, PuzzleMatch __result)
        {
            if (!logGetMatch) return;
            PuzzleGridPosition pgp = gridPosition;
            Logger.LogMessage(pgp.row + "," + pgp.col);
            if (pgp.GetToken() != null && pgp.GetToken().definition != null) Logger.LogMessage(pgp.GetToken().definition.name);
            if (__result == null) Logger.LogMessage("result is null, so no match detected, this token can spawn!");
        }
        */

        /*
        [HarmonyPrefix]
        [HarmonyPatch(typeof(PuzzleGame), "EndTokenMove")]
        public static void testingthing(PuzzleMatchSet ____moveTokenMatchSet)
        {
            Logger.LogMessage("hmm");
            foreach (PuzzleMatch pm in ____moveTokenMatchSet.matches)
            {
                foreach (PuzzleGridPosition pgp in pm.gridPositions)
                {
                    Logger.LogMessage(pgp.row + "," + pgp.col + "," + pgp.GetToken().definition.name);
                }
            }
        }
        [HarmonyPrefix]
        [HarmonyPatch(typeof(PuzzleGame), "SwitchPuzzleGroupTokensWith")]
        public static void testingthing4(PuzzleGame __instance, PuzzleGroup puzzleGroup)
        {
            Logger.LogMessage("hi");
            for (int i = 0; i < puzzleGroup.gridPositions.Count; i++)
            {
                PuzzleGridPosition puzzleGridPosition = puzzleGroup.gridPositions[i];
                PuzzleToken token = puzzleGridPosition.GetToken(false);
                Logger.LogMessage("hi" +  i);
            }
        }
        */
    }
}
