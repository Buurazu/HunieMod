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
        public static void RefreshBigPhotoPostfix(PhotoGallery __instance, ref int ____activeThumbnailIndex, ref tk2dSpriteCollectionData ____activeSpriteCollection, int __state)
        {
            ____activeThumbnailIndex = __state;
            __instance.bigPhoto.RemoveAllChildren();
            if (__state % 4 == 3 && BaseHunieModPlugin.CustomCGs.Value == true)
            {
                SpriteObject spr = GameUtil.ImageFileToSprite(GirlNames[__state / 4] + ".png", GirlNames[__state / 4]);

                if (spr != null)
                {
                    __instance.bigPhoto.AddChild(spr);

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
            //gallery thumbnails doesn't need to be censored if it's in specific CG mode
            if (!singlePhoto)
            {
                for (int i = 0; i < photos.Count; i++)
                {
                    photos[i].girlPhoto = photos[i - i % 4].girlPhoto;
                }
            }
        }

        //Keep that bra on
        [HarmonyPrefix]
        [HarmonyPatch(typeof(Girl), "HideBra")]
        public static bool HideBraDisabler() { return false; }

    }
}
