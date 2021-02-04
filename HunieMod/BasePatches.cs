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
    public class BasePatches
    {

        public static int searchForMe;

        public static void InitSearchForMe()
        {
            searchForMe = 123456789;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(LocationManager), "OnLocationArrival")]
        public static void PostReturnNotification()
        {
            if (BaseHunieModPlugin.cheatsEnabled) GameUtil.ShowNotification(CellNotificationType.MESSAGE, "CHEATS ARE ENABLED");
            else if (BaseHunieModPlugin.hasReturned) GameUtil.ShowNotification(CellNotificationType.MESSAGE, "This is for practice purposes only");
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(PuzzleGame), "UpdateAffectionMeterDisplay")]
        public static void AutosplitHelp(PuzzleGame __instance, ref int ____goalAffection)
        {
            if (BaseHunieModPlugin.cheatsEnabled || BaseHunieModPlugin.hasReturned) return;
            if (__instance.currentDisplayAffection == ____goalAffection)
                searchForMe = 100;
            else searchForMe = 0;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(LoadScreen), "OnStartGameMale")]
        public static void MaleStart()
        {
            if (!BaseHunieModPlugin.cheatsEnabled)
                searchForMe = 111;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(LoadScreen), "OnStartGameFemale")]
        public static void FemaleStart()
        {
            if (!BaseHunieModPlugin.cheatsEnabled)
                searchForMe = 111;
        }

    }
}
