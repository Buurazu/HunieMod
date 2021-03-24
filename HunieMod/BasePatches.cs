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
                            run.save();
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
