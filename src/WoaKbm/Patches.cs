using System;
using System.Linq;
using HarmonyLib;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using input;
using player;

namespace WoaKbm
{
    internal static class Patches
    {
        private static bool _lateInstalled;

        /// <summary>Everything that must not run from the BepInEx chainloader (engine not fully initialised yet).</summary>
        private static void InstallLate()
        {
            if (_lateInstalled) return;
            _lateInstalled = true;
            DeviceTracker.Install();
            if (Plugin.Cfg.KbmPrompts.Value)
            {
                try
                {
                    Plugin.HarmonyInstance.PatchAll(typeof(PromptPatches));
                    Plugin.Log.LogInfo("Prompt patches installed.");
                }
                catch (Exception e)
                {
                    Plugin.Log.LogError("Prompt patches failed: " + e);
                }
            }
            MonoAssemblyRepair.Run("startup");
        }

        // Runs before the game caches its action maps and before PlayerInput.OnEnable pairs devices.
        [HarmonyPatch(typeof(InputSystemManager), "Awake")]
        [HarmonyPrefix]
        private static void InputSystemManager_Awake_Prefix(InputSystemManager __instance)
        {
            try
            {
                InstallLate();
                KbmInjector.Apply(__instance.GetComponent<PlayerInput>());
            }
            catch (Exception e)
            {
                Plugin.Log.LogError("KBM injection failed: " + e);
            }
        }

        // PlayerInput pairs devices in OnEnable, which runs after every Awake on the prefab.
        [HarmonyPatch(typeof(PlayerInput), "OnEnable")]
        [HarmonyPostfix]
        private static void PlayerInput_OnEnable_Postfix(PlayerInput __instance)
        {
            try
            {
                string devs = __instance.user.valid
                    ? string.Join(", ", __instance.user.pairedDevices.Select(d => d.layout + ":" + d.name))
                    : "(user invalid)";
                Plugin.Log.LogInfo("PlayerInput enabled: map=" + (__instance.currentActionMap != null ? __instance.currentActionMap.name : "null")
                    + " scheme=" + (__instance.currentControlScheme ?? "none") + " paired=[" + devs + "]");
            }
            catch (Exception e)
            {
                Plugin.Log.LogError("OnEnable postfix failed: " + e);
            }
        }

        // Keyboard movement is always magnitude 1, which the game treats as running. Hold a modifier to walk.
        [HarmonyPatch(typeof(PlayerInputController), nameof(PlayerInputController.Update))]
        [HarmonyPostfix]
        private static void PlayerInputController_Update_Postfix(PlayerInputController __instance)
        {
            var key = Plugin.Cfg?.WalkModifierKey.Value;
            if (string.IsNullOrEmpty(key) || Keyboard.current == null || !__instance.IsRunning) return;
            var ctrl = Keyboard.current.TryGetChildControl(key) as ButtonControl;
            if (ctrl != null && ctrl.isPressed)
            {
                __instance.IsRunning = false;
            }
        }

        // Escape is both B (South) and Start (OpenMenu) in the field. When the menu key fires, swallow the B press.
        [HarmonyPatch(typeof(PlayerInputController), nameof(PlayerInputController.IsTriggerSouth))]
        [HarmonyPostfix]
        private static void PlayerInputController_IsTriggerSouth_Postfix(ref bool __result)
        {
            if (!__result || Plugin.Cfg == null || !Plugin.Cfg.SuppressCancelOnMenuKey.Value) return;
            try
            {
                var mgr = SingletonMonoBehaviour<InputSystemManager>.Instance;
                if (mgr == null) return;
                var map = mgr.GetActionMap(InputSystemManager.ActionSetId.Player);
                var openMenu = map?.FindAction("OpenMenu");
                if (openMenu != null && openMenu.triggered && IsKeyboardOrMouse(openMenu.activeControl))
                {
                    __result = false;
                }
            }
            catch
            {
                // never break input over a nicety
            }
        }

        private static bool IsKeyboardOrMouse(InputControl c)
        {
            return c != null && (c.device is Keyboard || c.device is Mouse);
        }
    }
}
