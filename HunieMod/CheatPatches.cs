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
            CheatPatches.AddItem("Pearl Necklace"); CheatPatches.AddItem("Stuffed Penguin"); CheatPatches.AddItem("Stuffed Whale");
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

        [HarmonyPostfix]
        [HarmonyPatch(typeof(GirlPlayerData), "ReadGirlSaveData")]
        public static void MakeUsMet(GirlPlayerData __instance)
        {
            if (__instance.metStatus < GirlMetStatus.UNKNOWN) __instance.metStatus = GirlMetStatus.UNKNOWN;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(GameManager), "SaveGame")]
        public static bool SaveDisabler() {
            if (BaseHunieModPlugin.savingDisabled) return false;
            else return true;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(LocationManager), "OnLocationSettled")]
        public static void SkipTutorialOnArrival()
        {
            if (GameManager.System.Player.tutorialStep < 2)
            {
                GameManager.System.Player.tutorialStep = 2;
                GameManager.System.Player.money = 1000;
                CheatPatches.AddItem("Stuffed Bear",GameManager.System.Player.dateGifts);
                AddGreatGiftsToInventory();

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
            GameManager.Stage.altGirl.ShowGirl(GameManager.Stage.altGirl.definition);
        }

    }
}
