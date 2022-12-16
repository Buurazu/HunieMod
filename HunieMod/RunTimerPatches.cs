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
    public class RunTimerPatches
    {
        public static Stopwatch initialTimerDelay = new Stopwatch();
        public static Stopwatch revertDiffDelay = new Stopwatch();
        public static Stopwatch savePBDelay = new Stopwatch();
        public static float simTime = 0;
        public static float dateTime = 0;
        public static int munieEarned = 0;
        public static long startTime = 0;
        public static bool isBonusRound = false;

        public static bool preventAllUpdate = false;

        [HarmonyPrefix]
        [HarmonyPatch(typeof(PuzzleManager), "OnJackpotRolledUp")]
        public static void CheckMunieEarnedPre(ref int __state)
        {
            __state = GameManager.System.Player.money;
        }
        [HarmonyPostfix]
        [HarmonyPatch(typeof(PuzzleManager), "OnJackpotRolledUp")]
        public static void CheckMunieEarnedPost(int __state)
        {
            int munieThisDate = GameManager.System.Player.money - __state;
            munieEarned += munieThisDate;
            
            BasePatches.Logger.LogMessage("Munie Earned: " + munieThisDate + ", Total Munie Earned: " + munieEarned);
            long tickDiff = DateTime.UtcNow.Ticks - startTime;
            string formatted = String.Format("Time: " + RunTimer.convert(TimeSpan.FromTicks(tickDiff)) + ", Munie Per Minute: {0:0.##}", (munieEarned / TimeSpan.FromTicks(tickDiff).TotalMinutes));
            BasePatches.Logger.LogMessage(formatted);
        }

        public static void Update()
        {
            if (!BaseHunieModPlugin.InGameTimer.Value || BaseHunieModPlugin.run == null) return;
            RunTimer run = BaseHunieModPlugin.run;

            //add to sim/date time
            if (GameManager.System.GameState == GameState.SIM)
                simTime += Time.deltaTime;
            else if (GameManager.System.GameState == GameState.PUZZLE)
                dateTime += Time.deltaTime;

            int goalMillis = 1000;
            //lower milliseconds before stats show in bonus rounds, because it fades away too quick
            if (isBonusRound) goalMillis = 500;

            if (initialTimerDelay.IsRunning && initialTimerDelay.ElapsedMilliseconds > goalMillis)
            {
                initialTimerDelay.Reset();
                preventAllUpdate = true;
            }

            if (savePBDelay.IsRunning && savePBDelay.ElapsedMilliseconds > 5000)
            {
                savePBDelay.Reset();
                run.save();
            }
            
            if (revertDiffDelay.IsRunning && revertDiffDelay.ElapsedMilliseconds > 6000)
            {
                revertDiffDelay.Reset();
                preventAllUpdate = false;
                UIPuzzleStatus status = GameManager.Stage.uiPuzzle.puzzleStatus;
                int plevel = (int)AccessTools.Field(typeof(UIPuzzleStatus), "_passionLevel").GetValue(status);
                status.passionSubtitle.SetText("Passion Level");
                status.SetPassionLevel(plevel);
            }
            
        }

        public static void ChangePuzzleUIText()
        {
            
            RunTimer run = BaseHunieModPlugin.run;
            UIPuzzleStatus status = GameManager.Stage.uiPuzzle.puzzleStatus;
            status.affectionLabel.SetText("^C" + RunTimer.colors[(int)run.splitColor] + "FF" + run.splitText);

            if (run.prevColor != RunTimer.SplitColors.WHITE)
            {
                status.passionSubtitle.SetText("This Split/Gold");
                string passionText = "^C" + RunTimer.colors[(int)run.prevColor] + "FF" + run.prevText + "^C" + RunTimer.colors[(int)run.goldColor] + "FF";
                if (run.goldText != "")
                {
                    if (run.prevText.Length <= 6) passionText += " ";
                    passionText += "[" + run.goldText + "]";
                }
                status.passionLabel.SetText(passionText);
            }
            else if (run.goldText != "")
            {
                string passionText = "^C" + RunTimer.colors[(int)run.goldColor] + "FF" + run.goldText;
                status.passionLabel.SetText(passionText);
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(PuzzleGame), "OnUpdate")]
        public static void DisplayOurInfo(PuzzleGame __instance, ref bool ____victory)
        {
            if (preventAllUpdate && (____victory || BaseHunieModPlugin.lastChosenCategory == RunTimer.INTRO))
            {
                ChangePuzzleUIText();
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(PuzzleManager), "ShowPuzzleJackpotReward")]
        public static void PreventBlipOfPassionZero()
        {
            if (preventAllUpdate)
            {
                ChangePuzzleUIText();
            }
        }


        [HarmonyPrefix]
        [HarmonyPatch(typeof(LocationManager), "OnLocationArrival")]
        public static void StopPreventingAffectionUpdate()
        {
            preventAllUpdate = false;

            UIPuzzleStatus status = GameManager.Stage.uiPuzzle.puzzleStatus;
            status.passionSubtitle.SetText("Passion Level");
            status.SetPassionLevel(0);
        }

        public static void UpdateFiles()
        {
            //TitleScreen ts = (TitleScreen)AccessTools.Field(typeof(UITitle), "_titleScreen").GetValue(GameManager.Stage.uiTitle);
            //LoadScreen ls = (LoadScreen)AccessTools.Field(typeof(TitleScreen), "_loadScreen").GetValue(ts);
            //foreach (LoadScreenSaveFile l in ls.saveFiles) l.Refresh();
            BasePatches.currentCategory.SetText(RunTimer.categories[BaseHunieModPlugin.lastChosenCategory]);
            BasePatches.currentDifficulty.SetText(RunTimer.difficulties[BaseHunieModPlugin.lastChosenDifficulty]);
            BasePatches.PBtext.SetText("PB: " + RunTimer.GetPB(BaseHunieModPlugin.lastChosenCategory, BaseHunieModPlugin.lastChosenDifficulty));
            BasePatches.SOBtext.SetText("SoB: " + RunTimer.GetGolds(BaseHunieModPlugin.lastChosenCategory, BaseHunieModPlugin.lastChosenDifficulty));
        }

        //always start a new timer, but only make it a category/difficulty when not cheating
        public static void StartNewRun(int saveFileIndex, bool cont = false)
        {
            if (BaseHunieModPlugin.run != null)
            {
                BaseHunieModPlugin.run.reset();
                BaseHunieModPlugin.run = null;
            }

            if (!BaseHunieModPlugin.cheatsEnabled && !cont)
            {
                BaseHunieModPlugin.run = new RunTimer(saveFileIndex, BaseHunieModPlugin.lastChosenCategory, BaseHunieModPlugin.lastChosenDifficulty);
            }
            else
            {
                BaseHunieModPlugin.run = new RunTimer();
            }

            simTime = 0; dateTime = 0; munieEarned = 0; startTime = DateTime.UtcNow.Ticks;
        }
        
        [HarmonyPostfix]
        [HarmonyPatch(typeof(LoadScreenSaveFile), "Refresh")]
        public static void ChangeFile1Text(LoadScreenSaveFile __instance, ref int ____saveFileIndex, ref SaveFile ____saveFile)
        {
            if (____saveFile.started || !BaseHunieModPlugin.InGameTimer.Value) return;

            __instance.titleLabel.SetText("Start New Run");

            /*__instance.noDataContainer.gameObj.SetActive(false);
            __instance.dataContainer.gameObj.SetActive(true);

            __instance.dataDateLabel.SetText(RunTimer.categories[BaseHunieModPlugin.lastChosenCategory]);
            __instance.dataLocationLabel.SetText(RunTimer.difficulties[BaseHunieModPlugin.lastChosenDifficulty]);

            __instance.dataTimeLabel.SetText(RunTimer.GetPB(BaseHunieModPlugin.lastChosenCategory, BaseHunieModPlugin.lastChosenDifficulty));
            __instance.dataGirlLabel.SetText(RunTimer.GetGolds(BaseHunieModPlugin.lastChosenCategory, BaseHunieModPlugin.lastChosenDifficulty));
            */
        }
        
        
        [HarmonyPostfix]
        [HarmonyPatch(typeof(LoadScreen), "OnStartGameMale")]
        public static void MaleStart(ref int saveFileIndex)
        {
            StartNewRun(saveFileIndex);
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(LoadScreen), "OnStartGameFemale")]
        public static void FemaleStart(ref int saveFileIndex)
        {
            StartNewRun(saveFileIndex);
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(LoadScreen), "OnContinueGame")]
        public static void Continuing(ref int saveFileIndex)
        {
            StartNewRun(saveFileIndex, true);
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(SettingsCellApp), "OnPanelOptionSelected")]
        public static void AutoDifficulty(ref SettingsSwitchPanel settingsSwitchPanel, ref SettingsSwitchPanel ____settingsPanelDifficulty)
        {
            if (settingsSwitchPanel == ____settingsPanelDifficulty && BaseHunieModPlugin.lastChosenDifficulty == 0 && BaseHunieModPlugin.run != null && BaseHunieModPlugin.run.category != "")
            {
                BaseHunieModPlugin.run.chosenDifficulty = (int)GameManager.System.Player.settingsDifficulty + 1;
                BaseHunieModPlugin.run.refresh();
            }
        }

    }
}
