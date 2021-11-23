﻿using System;
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

        public static int searchForMe;

        public static SpriteObject updateSprite;

        public static BepInEx.Logging.ManualLogSource Logger = BepInEx.Logging.Logger.CreateLogSource("BasePatches");

        public static bool splitThisDate = false;
        public static bool replacingText = false;
        public static int startingCompletedGirls = 0;
        public static int startingRelationship = 0;

        public static bool titleScreenInteractive = true;

        public static void InitSearchForMe()
        {
            searchForMe = 123456789;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(LocationManager), "OnLocationArrival")]
        public static void PostReturnNotification()
        {
            titleScreenInteractive = true; //reset title screen interactive here
            if (BaseHunieModPlugin.cheatsEnabled) GameUtil.ShowNotification(CellNotificationType.MESSAGE, "CHEATS ARE ENABLED");
            else if (BaseHunieModPlugin.hasReturned) GameUtil.ShowNotification(CellNotificationType.MESSAGE, "This is for practice purposes only");
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(PuzzleGame), "OnUpdate")]
        public static void AutosplitHelp(PuzzleGame __instance, ref int ____goalAffection, ref bool ____victory, ref bool ____isBonusRound)
        {
            //allow splits to happen with cheats/has returned, as long as we don't auto-start with those on it's fine
            //if (BaseHunieModPlugin.cheatsEnabled || BaseHunieModPlugin.hasReturned) return;
            if (__instance.currentDisplayAffection == 0)
            {
                startingCompletedGirls = GameManager.System.Player.GetTotalMaxRelationships();
                startingRelationship = GameManager.System.Player.GetGirlData(GameManager.Stage.girl.definition).relationshipLevel;
            }
            if (__instance.currentDisplayAffection == ____goalAffection && (____victory || ____isBonusRound))
            {
                //make the autosplitter more viable for 100%?
                if (startingCompletedGirls < 12)
                    searchForMe = 100;
                //if a timer is running, split
                if (!splitThisDate && BaseHunieModPlugin.run != null)
                {
                    bool didSplit = false;
                    RunTimer run = BaseHunieModPlugin.run;
                    //don't split for dates in postgame
                    if (startingCompletedGirls < 12)
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
                        if (!____isBonusRound) RunTimerPatches.revertDiffDelay.Start();
                        RunTimerPatches.isBonusRound = ____isBonusRound;
                        int dateNum = startingRelationship;
                        //if (____isBonusRound) dateNum++; I thought I need to increase this but it makes it #6

                        string newSplit = GameManager.Stage.girl.definition.firstName + " #" + dateNum;
                        if (GameManager.Stage.girl.definition.firstName == "Kyu" && startingCompletedGirls == 0) newSplit = "Tutorial";
                        newSplit += "\n      " + run.splitText + "\n";
                        run.push(newSplit);

                        if (____isBonusRound && (run.goal == startingCompletedGirls + 1))
                        {
                            //run.save();
                            RunTimerPatches.savePBDelay.Start();
                        }

                    }
                }


                splitThisDate = true;
            }
            else
            {
                searchForMe = 0;
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

        [HarmonyPostfix]
        [HarmonyPatch(typeof(LoadScreen), "OnStartGameMale")]
        public static void MaleStart()
        {
            titleScreenInteractive = false;
            if (!BaseHunieModPlugin.cheatsEnabled)
                searchForMe = 111;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(LoadScreen), "OnStartGameFemale")]
        public static void FemaleStart()
        {
            titleScreenInteractive = false;
            if (!BaseHunieModPlugin.cheatsEnabled)
                searchForMe = 111;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(LoadScreen), "OnContinueGame")]
        public static void ContinueGame(ref int saveFileIndex)
        {
            titleScreenInteractive = false;
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

        [HarmonyPostfix]
        [HarmonyPatch(typeof(LoadScreen), "Init")]
        public static void UpdateButton(LoadScreen __instance, ref SpriteObject ____creditsButton)
        {
            //add mod version and settings info to the corner (or just edit the text if on Valentine's)
            int version = BaseHunieModPlugin.GameVersion(); //0 = jan23, 1 = valentines
            string[] v = { "Jan. 23", "Valentines" };
            string newText1 = "Speedrun Mod " + BaseHunieModPlugin.PluginVersion + " (" + v[version] + ")";
            string newText2 = "";
            if (BaseHunieModPlugin.VsyncEnabled.Value)
                newText2 += "Vsync On (" + Screen.currentResolution.refreshRate + ")";
            else if (BaseHunieModPlugin.CapAt144.Value)
                newText2 += "144 FPS Lock";
            else
                newText2 += "60 FPS Lock";

            if (BaseHunieModPlugin.V1Drain.Value && version == 0)
                newText2 += ", V1.0 Drain";

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
                __instance.buttonContainer.AddChild(labelObject);
                labelObject.ShiftSelfToTop();
                labelObject.localX = -503f;
                labelObject.localY = -10f;
            }
            else
            {
                LabelObject ____noteVersion = (__instance.GetChildByName("LoadScreenNotes").GetChildByName("LoadScreenNoteVersion") as LabelObject);
                LabelObject ____noteCensor = (__instance.GetChildByName("LoadScreenNotes").GetChildByName("LoadScreenNoteCensor") as LabelObject);
                ____noteVersion.SetText(newText1);
                ____noteCensor.SetText(newText2);
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

    }
}
