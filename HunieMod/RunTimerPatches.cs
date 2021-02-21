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
        public static bool isBonusRound = false;

        public static bool preventAllUpdate = false;

        public static void Update()
        {
            
            if (!BaseHunieModPlugin.InGameTimer.Value || BaseHunieModPlugin.run == null) return;
            RunTimer run = BaseHunieModPlugin.run;

            //checking criterias for non-Wing categories
            if (run.goal == 100 && GameManager.System.SaveFile != null && GameManager.System.SaveFile.GetPercentComplete() == 100)
            {
                run.split();
                string newSplit = "100%\n      " + run.splitText + "\n";
                run.push(newSplit);
                run.save();
                //run.goal should no longer be 100% now, so this will only execute once
            }

            if (initialTimerDelay.IsRunning && initialTimerDelay.ElapsedMilliseconds > 1000)
            {
                initialTimerDelay.Reset();
                preventAllUpdate = true;
            }
            //there's no point to reverting our change early, because the passion level gets drained to zero by then anyway
            /*
            if (revertDiffDelay.IsRunning && revertDiffDelay.ElapsedMilliseconds > 6000)
            {
                revertDiffDelay.Reset();
                preventAllUpdate = false;
                UIPuzzleStatus status = GameManager.Stage.uiPuzzle.puzzleStatus;
                int plevel = (int)AccessTools.Field(typeof(UIPuzzleStatus), "_passionLevel").GetValue(status);
                status.passionSubtitle.SetText("Passion Level");
                status.SetPassionLevel(plevel);
            }
            */
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(PuzzleGame), "OnUpdate")]
        public static void DisplayOurInfo(PuzzleGame __instance, ref int ____goalAffection, ref bool ____victory, ref bool ____isBonusRound)
        {
            UIPuzzleStatus status = GameManager.Stage.uiPuzzle.puzzleStatus;
            RunTimer run = BaseHunieModPlugin.run;
            if (preventAllUpdate)
            {
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
        }


        [HarmonyPrefix]
        [HarmonyPatch(typeof(LocationManager), "OnLocationArrival")]
        public static void StopPreventingAffectionUpdate()
        {
            preventAllUpdate = false;

            UIPuzzleStatus status = GameManager.Stage.uiPuzzle.puzzleStatus;
            int plevel = (int)AccessTools.Field(typeof(UIPuzzleStatus), "_passionLevel").GetValue(status);
            status.passionSubtitle.SetText("Passion Level");

        }

        public static void UpdateFiles()
        {
            TitleScreen ts = (TitleScreen)AccessTools.Field(typeof(UITitle), "_titleScreen").GetValue(GameManager.Stage.uiTitle);
            LoadScreen ls = (LoadScreen)AccessTools.Field(typeof(TitleScreen), "_loadScreen").GetValue(ts);
            foreach (LoadScreenSaveFile l in ls.saveFiles) l.Refresh();
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
                int catChoice = BaseHunieModPlugin.lastChosenCategory;
                BaseHunieModPlugin.run = new RunTimer(saveFileIndex, catChoice, BaseHunieModPlugin.lastChosenDifficulty);
            }
            else
            {
                BaseHunieModPlugin.run = new RunTimer();
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(LoadScreenSaveFile), "Refresh")]
        public static void ChangeFile1Text(LoadScreenSaveFile __instance, ref int ____saveFileIndex, ref SaveFile ____saveFile)
        {
            if (____saveFile.started || !BaseHunieModPlugin.InGameTimer.Value) return;

            __instance.noDataContainer.gameObj.SetActive(false);
            __instance.dataContainer.gameObj.SetActive(true);

            __instance.titleLabel.SetText("Start New Run");
            __instance.dataDateLabel.SetText(RunTimer.categories[BaseHunieModPlugin.lastChosenCategory]);
            __instance.dataLocationLabel.SetText(RunTimer.difficulties[BaseHunieModPlugin.lastChosenDifficulty]);

            __instance.dataTimeLabel.SetText(RunTimer.GetPB(BaseHunieModPlugin.lastChosenCategory, BaseHunieModPlugin.lastChosenDifficulty));
            __instance.dataGirlLabel.SetText(RunTimer.GetGolds(BaseHunieModPlugin.lastChosenCategory, BaseHunieModPlugin.lastChosenDifficulty));

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
            if (settingsSwitchPanel == ____settingsPanelDifficulty && BaseHunieModPlugin.lastChosenDifficulty == 0 && BaseHunieModPlugin.run != null)
            {
                BaseHunieModPlugin.run.category = RunTimer.categories[BaseHunieModPlugin.lastChosenCategory] + " " + RunTimer.difficulties[(int)GameManager.System.Player.settingsDifficulty+1];
                BaseHunieModPlugin.run.refresh();
            }
        }
    }
}
