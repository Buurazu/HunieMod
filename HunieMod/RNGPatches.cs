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
    public class RNGPatches
    {

        public static System.Random curRandom = null; //temporarily set before a key function is done
        public static System.Random dateRandom = null; //advanced per date start
        public static System.Random curDateRandom = null; //handles the RNG mid-date
        public static System.Random powerTokenRandom = null; //handles power token RNG
        public static System.Random dateGiftRandom = null;
        public static System.Random storeRandom = null;
        public static System.Random talkRandom = null; //used for what kind of talk you get

        public static bool inPuzzleBegin = false;
        public static bool ignore = false;

        public static int ourSeed;
        public static int currentDateSeed, currentPowerSeed;

        public static void InitializeRNG(int seed)
        {
            ourSeed = seed;
            BasePatches.Logger.LogMessage("Seed: " + seed);
            curRandom = null;
            if (BaseHunieModPlugin.seedDates) dateRandom = new System.Random(seed);
            curDateRandom = null;
            powerTokenRandom = null;
            if (BaseHunieModPlugin.seedGifts) dateGiftRandom = new System.Random(seed - 1000);
            if (BaseHunieModPlugin.seedStore) storeRandom = new System.Random(seed - 2000);
            if (BaseHunieModPlugin.seedTalks) talkRandom = new System.Random(seed - 3000);
        }
        public static void WipeRNG()
        {
            ourSeed = 0;
            curRandom = null;
            dateRandom = null;
            curDateRandom = null;
            powerTokenRandom = null;
            dateGiftRandom = null;
            storeRandom = null;
            talkRandom = null;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(UnityEngine.Random), "Range", new Type[] { typeof(int), typeof(int) })]
        public static void SpoofRandomRange(int min, int max, ref int __result)
        {
            if (BaseHunieModPlugin.seedMode && curRandom != null && !ignore)
            {
                //BasePatches.Logger.LogMessage((new System.Diagnostics.StackTrace()).GetFrame(2).GetMethod().Name + " called RandomInt: " + min + "," + max + "," + __result);
                __result = curRandom.Next(min, max);
            }
        }
        [HarmonyPostfix]
        [HarmonyPatch(typeof(UnityEngine.Random), "Range", new Type[] { typeof(float), typeof(float) })]
        public static void SpoofRandomRangeFloat(float min, float max, ref float __result)
        {
            //NextDouble doesn't have nextdouble range, but, it defaults to between 0 and 1
            //That's fine for now, this should only be used for power token chance which is 0-1
            if (BaseHunieModPlugin.seedMode && curRandom != null && !ignore)
            {
                //BasePatches.Logger.LogMessage((new System.Diagnostics.StackTrace()).GetFrame(2).GetMethod().Name + " called RandomFloat: " + min + "," + max + "," + __result);
                __result = (float)curRandom.NextDouble();
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(PuzzleManager), "ShowPuzzleJackpotReward")]
        public static void SeededNotif()
        {
            if (BaseHunieModPlugin.seedMode)
                GameUtil.ShowNotification(CellNotificationType.MESSAGE, "Seeded Run: " + ourSeed);
        }

        
        [HarmonyPrefix]
        [HarmonyPatch(typeof(PuzzleGame), "Begin")]
        public static void PerDateRNG()
        {
            if (!BaseHunieModPlugin.seedMode || dateRandom == null) return;
            if (CheatPatches.refreshingPuzzle)
            {
                
            }
            else if (dateRandom != null)
            {
                currentDateSeed = dateRandom.Next();
                currentPowerSeed = dateRandom.Next();
            }
            CheatPatches.refreshingPuzzle = false;
            curDateRandom = new System.Random(currentDateSeed);
            powerTokenRandom = new System.Random(currentPowerSeed);
            inPuzzleBegin = true;
            curRandom = curDateRandom;
        }
        [HarmonyPostfix]
        [HarmonyPatch(typeof(PuzzleGame), "Begin")]
        public static void EndInitialRNG()
        {
            inPuzzleBegin = false;
            curRandom = null;
        }
        [HarmonyPrefix]
        [HarmonyPatch(typeof(PuzzleGame), "Destroy")]
        public static void EndRNG()
        {
            curDateRandom = null;
            powerTokenRandom = null;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(PuzzleGame), "CreateToken")]
        public static void TokenRNGPre()
        {
            curRandom = curDateRandom;
        }
        [HarmonyPostfix]
        [HarmonyPatch(typeof(PuzzleGame), "CreateToken")]
        public static void TokenRNGPost()
        {
            if (!inPuzzleBegin) curRandom = null;
        }
        [HarmonyPrefix]
        [HarmonyPatch(typeof(PuzzleGame), "ProcessMatch")]
        public static void PowerRNGPre()
        {
            curRandom = powerTokenRandom;
        }
        [HarmonyPostfix]
        [HarmonyPatch(typeof(PuzzleGame), "ProcessMatch")]
        public static void PowerRNGPost()
        {
            curRandom = null;
        }
        [HarmonyPrefix]
        [HarmonyPatch(typeof(PuzzleGame), "SwitchPuzzleGroupTokensWith")]
        public static void CompactRNGPre(PuzzleGroup puzzleGroup)
        {
            curRandom = curDateRandom;
        }
        [HarmonyPostfix]
        [HarmonyPatch(typeof(PuzzleGame), "SwitchPuzzleGroupTokensWith")]
        public static void CompactRNGPost()
        {
            curRandom = null;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(GirlManager),"GiveItem")]
        public static void ItemRNGPre()
        {
            curRandom = dateGiftRandom;
        }
        [HarmonyPostfix]
        [HarmonyPatch(typeof(GirlManager), "GiveItem")]
        public static void ItemRNGPost()
        {
            curRandom = null;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(GirlManager), "TalkWithHer")]
        public static void TalkPre()
        {
            curRandom = talkRandom;
        }
        [HarmonyPostfix]
        [HarmonyPatch(typeof(GirlManager), "TalkWithHer")]
        public static void TalkPost()
        {
            curRandom = null;
        }

        public static void ListUtilsShuffle<T>(List<T> list, System.Random random)
        {
			int i = list.Count;
			while (i > 1)
			{
				i--;
				int index = random.Next(i + 1);
				T value = list[index];
				list[index] = list[i];
				list[i] = value;
            }
        }


        //ListUtils.Shuffle isn't really patchable so I have to do this...
        [HarmonyPrefix]
        [HarmonyPatch(typeof(PlayerManager), "RollNewStoreList")]
        public static bool SeedStoreRNG(PlayerManager __instance, StoreItemPlayerData[] storeList, ItemType itemType)
        {
            if (!BaseHunieModPlugin.seedMode || storeRandom == null) return true;

            StoreItemPlayerData[] _storeUnique = (StoreItemPlayerData[])AccessTools.Field(typeof(PlayerManager), "_storeUnique").GetValue(__instance);
            StoreItemPlayerData[] _storeGifts = (StoreItemPlayerData[])AccessTools.Field(typeof(PlayerManager), "_storeGifts").GetValue(__instance);
            Dictionary<GirlDefinition, GirlPlayerData> _girls = (Dictionary<GirlDefinition, GirlPlayerData>)AccessTools.Field(typeof(PlayerManager), "_girls").GetValue(__instance);

            List<ItemDefinition> list;
            if (storeList != _storeUnique)
            {
                //ItemData.GetAllOfType reimplementation
                List<ItemDefinition> list2 = new List<ItemDefinition>();
                for (int i = 0; i < Definitions.Items.Count; i++)
                {
                    if (Definitions.Items[i].type == itemType && Definitions.Items[i].name != "Kyu Plushie")
                    {
                        list2.Add(Definitions.Items[i]);
                    }
                }
                list = list2;
                if (storeList == _storeGifts)
                {
                    int num = Mathf.Clamp(6 + Mathf.FloorToInt((float)(__instance.GetTotalGirlsRelationshipLevel() / 2)), 6, 18);
                    for (int i = 0; i < list.Count; i++)
                    {
                        if (list[i].itemFunctionValue > num)
                        {
                            list.RemoveAt(i);
                            i--;
                        }
                    }
                }
            }
            else
            {
                list = new List<ItemDefinition>();
                List<GirlDefinition> all = GameManager.Data.Girls.GetAll();
                for (int j = 0; j < all.Count; j++)
                {
                    if (!all[j].secretGirl || _girls[all[j]].metStatus == GirlMetStatus.MET)
                    {
                        for (int k = 0; k < all[j].uniqueGiftList.Count; k++)
                        {
                            if (!__instance.HasItem(all[j].uniqueGiftList[k]) && !_girls[all[j]].IsItemInUniqueGifts(all[j].uniqueGiftList[k]))
                            {
                                list.Add(all[j].uniqueGiftList[k]);
                                break;
                            }
                        }
                    }
                }
            }
            RNGPatches.ListUtilsShuffle<ItemDefinition>(list, storeRandom);
            if (list.Count > 12)
            {
                list.RemoveRange(12, list.Count - 12);
            }
            for (int l = 0; l < 12; l++)
            {
                if (list.Count > l)
                {
                    storeList[l].itemDefinition = list[l];
                    storeList[l].soldOut = false;
                }
                else
                {
                    storeList[l].itemDefinition = null;
                    storeList[l].soldOut = true;
                }
            }
            return false;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(GirlManager), "GetDialogTriggerLine")]
        [HarmonyPatch(typeof(EnergyTrail), "Init")]
        [HarmonyPatch(typeof(GirlPlayerData), "AddItemToCollection")]
        public static void IgnorePre()
        {
            ignore = true;
        }
        [HarmonyPostfix]
        [HarmonyPatch(typeof(GirlManager), "GetDialogTriggerLine")]
        [HarmonyPatch(typeof(EnergyTrail), "Init")]
        [HarmonyPatch(typeof(GirlPlayerData), "AddItemToCollection")]
        public static void IgnorePost()
        {
            ignore = false;
        }

    }
}
