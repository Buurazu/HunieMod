﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
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
    public class InputPatches
    {
        public static bool mouseDown = false;

        public static List<KeyCode> mouseKeyboardKeys = new List<KeyCode>();

        public static float[] prevAxises = new float[BaseHunieModPlugin.AXISES+1];

        public const float DEADZONE = 0.5f;

        public static bool mashCheat = false;

        public static BepInEx.Logging.ManualLogSource Logger = BepInEx.Logging.Logger.CreateLogSource("InputPatches");

        public static bool IsMouseKeyDown()
        {
            if (mashCheat) return true;
            if (BaseHunieModPlugin.MouseWheelEnabled.Value && Input.GetAxis("Mouse ScrollWheel") != 0) return true;
            if (BaseHunieModPlugin.AxisesEnabled.Value)
            {
                for (int i = 0; i <= BaseHunieModPlugin.AXISES; i++)
                {
                    if (Mathf.Abs(Input.GetAxisRaw("Axis " + i)) > DEADZONE && Mathf.Abs(prevAxises[i]) <= DEADZONE) return true;
                }
            }
            for (int i = 0; i < mouseKeyboardKeys.Count; i++)
            {
                if (Input.GetKeyDown(mouseKeyboardKeys[i])) return true;
            }
            return false;
        }

        public static bool IsMouseKeyUp()
        {
            if (mashCheat) return true;
            if (BaseHunieModPlugin.MouseWheelEnabled.Value && Input.GetAxis("Mouse ScrollWheel") != 0) return true;
            if (BaseHunieModPlugin.AxisesEnabled.Value)
            {
                for (int i = 0; i <= BaseHunieModPlugin.AXISES; i++)
                {
                    if (Mathf.Abs(Input.GetAxisRaw("Axis " + i)) <= DEADZONE && Mathf.Abs(prevAxises[i]) > DEADZONE) return true;
                }
            }
            for (int i = 0; i < mouseKeyboardKeys.Count; i++)
            {
                if (Input.GetKeyUp(mouseKeyboardKeys[i])) return true;
            }
            return false;
        }

        public static bool IsMouseKey()
        {
            if (mashCheat) return true;
            if (BaseHunieModPlugin.MouseWheelEnabled.Value && Input.GetAxis("Mouse ScrollWheel") != 0) return true;
            if (BaseHunieModPlugin.AxisesEnabled.Value)
            {
                for (int i = 0; i <= BaseHunieModPlugin.AXISES; i++)
                {
                    if (Mathf.Abs(Input.GetAxisRaw("Axis " + i)) > DEADZONE) return true;
                }
            }
            for (int i = 0; i < mouseKeyboardKeys.Count; i++)
            {
                if (Input.GetKey(mouseKeyboardKeys[i])) return true;
            }
            return false;
        }


        [HarmonyPrefix]
        [HarmonyPatch(typeof(CursorManager), "Update")]
        public static bool FakeMouseClicks(CursorManager __instance, ref List<CursorAttachedObject> ____attachedObjects,
            ref DisplayObject ____mouseTarget, ref DisplayObject ____mouseDownTarget, ref Vector3 ____mouseDelta, ref Vector3 ____previousMousePosition)
        {
            //if (!stopwatch.IsRunning) stopwatch.Start();
            //Logger.LogMessage(stopwatch.ElapsedMilliseconds);
            Vector3 mousePosition = __instance.GetMousePosition();
            for (int i = 0; i < ____attachedObjects.Count; i++)
            {
                CursorAttachedObject cursorAttachedObject = ____attachedObjects[i];
                cursorAttachedObject.displayObject.gameObj.transform.position = new Vector3(mousePosition.x + cursorAttachedObject.xOffset, mousePosition.y + cursorAttachedObject.yOffset, cursorAttachedObject.displayObject.gameObj.transform.position.z);
            }

            DisplayObject displayObject = __instance.FindMouseTarget(mousePosition);
            if (displayObject != ____mouseTarget)
            {
                if (____mouseTarget != null)
                {
                    ____mouseTarget.MouseOut();
                    if (____mouseTarget == ____mouseDownTarget)
                    {
                        ____mouseDownTarget = null;
                    }
                }
                ____mouseTarget = displayObject;
                if (____mouseTarget != null)
                {
                    ____mouseTarget.MouseOver();
                }
            }

            if (Input.GetMouseButtonDown(0) || IsMouseKeyDown())
            {
                mouseDown = true;
                //mouseWasDown = true;
                if (____mouseTarget != null)
                {
                    ____mouseTarget.MouseDown();
                    ____mouseDownTarget = ____mouseTarget;
                }
                GameManager.Stage.MouseDown();
            }
            if (mouseDown && (Input.GetMouseButtonUp(0) || IsMouseKeyUp()))
            {
                mouseDown = false;
                //mouseWasClicked = true;
                if (____mouseTarget != null)
                {
                    ____mouseTarget.MouseUp();
                    if (____mouseTarget == ____mouseDownTarget)
                    {
                        ____mouseTarget.MouseClick();
                    }
                }
                GameManager.Stage.MouseUp();
                ____mouseDownTarget = null;
            }
            ____mouseDelta = mousePosition - ____previousMousePosition;
            ____previousMousePosition = mousePosition;

            for (int i = 0; i <= BaseHunieModPlugin.AXISES; i++)
            {
                prevAxises[i] = Input.GetAxisRaw("Axis " + i);
            }

            return false;
        }

    }
}
