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
    public class CensorshipPatches
    {
        public static string[] GirlNames = new string[] { "Tiffany", "Aiko", "Kyanna", "Audrey", "Lola", "Nikki", "Jessie", "Beli", "Kyu", "Momo", "Celeste", "Venus" };
        public static string[] GirlNamesLower = new string[] { "tiffany", "aiko", "kyanna", "audrey", "lola", "nikki", "jessie", "beli", "kyu", "momo", "celeste", "venus" };

        //Replace all CGs with the girl's first CG
        [HarmonyPrefix]
        [HarmonyPatch(typeof(PhotoGallery), "RefreshBigPhoto")]
        public static void RefreshBigPhotoPrefix(PhotoGallery __instance, ref int ____activeThumbnailIndex, ref int __state)
        {
            __state = ____activeThumbnailIndex;
            if (____activeThumbnailIndex % 4 >= 1)
            {
                ____activeThumbnailIndex -= ____activeThumbnailIndex % 4;
            }
        }
        //Replace the sex CGs with user images if they exist
        [HarmonyPostfix]
        [HarmonyPatch(typeof(PhotoGallery), "RefreshBigPhoto")]
        public static void ShowFunnyCG4Image(PhotoGallery __instance, ref int ____activeThumbnailIndex, int __state)
        {
            ____activeThumbnailIndex = __state;
            __instance.bigPhoto.RemoveAllChildren();
            if (__state % 4 == 3)
            {
                //SpriteObject spr = GameUtil.ImageFileToSprite(GirlNames[__state / 4] + ".png", GirlNames[__state / 4]);
                SpriteObject spr;

                if (BaseHunieModPlugin.customCG4.TryGetValue(GirlNamesLower[__state / 4], out spr))
                {
                    SpriteObject spr2 = (SpriteObject)UnityEngine.Object.Instantiate(spr);
                    __instance.bigPhoto.AddChild(spr2);

                    SpriteObject updatedSprite = __instance.bigPhoto.GetChildren(true)[__instance.bigPhoto.GetChildren().Length - 1] as SpriteObject;
                    updatedSprite.SetLocalPosition(0, 0);
                    updatedSprite.SetOwnChildIndex(3);
                }
            }
        }

        //Censor the gallery thumbnails
        [HarmonyPrefix]
        [HarmonyPatch(typeof(PhotoGallery), "Init")]
        public static void InitGalleryPrefix(List<PhotoGalleryGirlPhoto> photos, bool singlePhoto)
        {
            //gallery thumbnails doesn't need to be censored if it's in post-sex single photo mode
            if (!singlePhoto)
            {
                for (int i = 0; i < photos.Count; i++)
                {
                    photos[i].girlPhoto = photos[i - i % 4].girlPhoto;
                }
            }
        }

        //Censor the Girl Info Photos thumbnails
        [HarmonyPostfix]
        [HarmonyPatch(typeof(GirlProfileCellApp), "Init")]
        public static void NoGirlPhotoTitties(GirlProfileCellApp __instance)
        {
            GirlDefinition gd;
            if (GameManager.Stage.cellPhone.cellMemory.ContainsKey("cell_memory_profile_girl"))
            {
                gd = GameManager.Data.Girls.Get(GameManager.Stage.cellPhone.cellMemory["cell_memory_profile_girl"]);
            }
            else
            {
                gd = GameManager.System.Location.currentGirl;
            }
            for (int n = 0; n < 4; n++)
            {
                GirlProfilePhoto girlProfilePhoto = __instance.tabPhotos.GetChildByName("GirlProfilePhoto" + n.ToString()) as GirlProfilePhoto;
                if (girlProfilePhoto.sprite.spriteId != 120)
                    girlProfilePhoto.sprite.SetSprite(gd.photos[0].smallSpriteName[0]);
            }
        }

        //define inappropriate outfits for each character
        //Girl IDs start at 1, be careful
        public static int[][] lewdOutfits =
        {
            //Tiffany
            new int[] { 3, 4 },
            //Aiko
            new int[] { 4 },
            //Kyanna
            new int[] { 4 },
            //Audrey
            new int[] { 4 },
            //Lola
            new int[] { 4 },
            //Nikki
            new int[] { 4 },
            //Jessie
            new int[] { 1, 4 },
            //Beli
            new int[] { 4 },
            //Kyu
            new int[] { 4 },
            //Momo
            new int[] { 4 },
            //Celeste
            new int[] { 1, 2, 3, 4 },
            //Venus
            new int[] { 1, 2, 3, 4 }
        };


        [HarmonyPrefix]
        [HarmonyPatch(typeof(Girl), "AddGirlPiece")]
        public static void CensorLewdOutfits(ref GirlPiece girlPiece, Girl __instance)
        {
            if (!BaseHunieModPlugin.OutfitCensorshipEnabled.Value) return;
            foreach (int i in lewdOutfits[__instance.definition.id - 1])
            {
                if ("outfit_" + (i+1) == girlPiece.art[0].spriteName)
                {
                    girlPiece = __instance.definition.pieces[13];
                    break;
                }
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(Girl), "ShowGirl")]
        public static void NikkiGlassesSetting(Girl __instance, GirlDefinition girlDefinition)
        {
            if (girlDefinition.firstName == "Nikki")
            {
                List<GirlPiece> piecesByType = girlDefinition.GetPiecesByType(GirlPieceType.EXTRA);
                if (BaseHunieModPlugin.NikkisGlasses.Value == 0)
                {
                    piecesByType[0].showChance = 0;
                }
                else if (BaseHunieModPlugin.NikkisGlasses.Value == 1) {
                    piecesByType[0].hideOnDates = false;
                    piecesByType[0].underwear = true;
                }
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(Girl), "ShowGirl")]
        public static void CensorBraPanties(Girl __instance, GirlDefinition girlDefinition)
        {
            if (BaseHunieModPlugin.BraPantiesCensorshipEnabled.Value && GameManager.System.Location.currentLocation.bonusRoundLocation)
                __instance.ChangeStyle(girlDefinition.outfits[0].artIndex, true);
        }

        // mute sex
        // 
        [HarmonyPrefix]
        [HarmonyPatch(typeof(AudioManager), "Play", typeof(AudioCategory), typeof(AudioDefinition), typeof(bool), typeof(float), typeof(bool))]
        public static void SilenceTheMoans(AudioDefinition audioDefinition, ref float volume)
        {
            if (!BaseHunieModPlugin.SexSFXCensorshipEnabled.Value || audioDefinition == null || audioDefinition.clip == null) return;
            if (audioDefinition.clip.name.Contains("sexual") && audioDefinition.clip.name != "puzzle_token_match_sexual")
                volume = 0f;
        }

        //Keep that bra on
        [HarmonyPrefix]
        [HarmonyPatch(typeof(Girl), "HideBra")]
        public static bool HideBraDisabler() { return false; }

    }
}
