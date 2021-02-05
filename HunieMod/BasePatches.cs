using System;
using System.Collections.Generic;
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
