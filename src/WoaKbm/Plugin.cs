using System;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;
using UnityEngine.InputSystem;

namespace WoaKbm
{
    [BepInPlugin(GUID, NAME, VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        public const string GUID = "danreed.woa.kbm";
        public const string NAME = "Winds of Anthos Keyboard & Mouse";
        public const string VERSION = "1.0.0";

        internal static ManualLogSource Log;
        internal static KbmConfig Cfg;

        internal static Harmony HarmonyInstance => _instance?._harmony;
        private static Plugin _instance;
        private Harmony _harmony;

        private void Awake()
        {
            _instance = this;
            Log = Logger;
            Cfg = new KbmConfig(Config);

            _harmony = new Harmony(GUID);
            _harmony.PatchAll(typeof(Patches));
            // NOTE: nothing else happens here. The chainloader runs before the engine's native input is
            // initialised; registering the prompt patches or touching UnityEngine.InputSystem at this point
            // hangs the game at "Using Windows.Gaming.Input". Both are done from the InputSystemManager.Awake
            // prefix instead (see Patches.InstallLate).

            Application.quitting += OnQuitting;
            Log.LogInfo($"{NAME} {VERSION} loaded.");
        }

        // The game crashes inside Mono at shutdown with live detours and callbacks in place.
        // Tear everything down while the runtime is still healthy.
        private void OnQuitting()
        {
            try
            {
                MonoAssemblyRepair.Run("quit");
                PromptOverlay.RestoreAll();
                DeviceTracker.Uninstall();
                _harmony?.UnpatchSelf();
                Log.LogInfo("Unpatched for shutdown.");
            }
            catch (Exception e)
            {
                Log.LogWarning("Shutdown cleanup failed: " + e.Message);
            }
        }

        private void Update()
        {
            CursorControl.Tick();
        }
    }

    internal class KbmConfig
    {
        public readonly ConfigEntry<bool> DumpAsset;
        public readonly ConfigEntry<bool> StripControlSchemes;
        public readonly ConfigEntry<bool> AllowAllDevices;
        public readonly ConfigEntry<string> ExcludeActions;
        public readonly ConfigEntry<bool> InjectBindings;

        public readonly ConfigEntry<bool> LockCursor;
        public readonly ConfigEntry<string> CursorUnlockKey;
        public readonly ConfigEntry<string> MouseLookActions;
        public readonly ConfigEntry<float> MouseSensitivity;
        public readonly ConfigEntry<bool> MouseInvertY;

        public readonly ConfigEntry<bool> KbmPrompts;
        public readonly ConfigEntry<string> TextPromptFormat;
        public readonly ConfigEntry<string> GlyphCharMap;
        public readonly ConfigEntry<bool> DumpGlyphs;
        public readonly ConfigEntry<string> CustomKeyImageDir;

        public readonly ConfigEntry<string> WalkModifierKey;
        public readonly ConfigEntry<bool> SuppressCancelOnMenuKey;

        // One entry per Xbox-layout gamepad control; values are ';'-separated.
        // A bare name means a keyboard key (<Keyboard>/name); anything starting with '<' is a raw control path.
        public readonly ConfigEntry<string> ButtonSouth;
        public readonly ConfigEntry<string> ButtonEast;
        public readonly ConfigEntry<string> ButtonWest;
        public readonly ConfigEntry<string> ButtonNorth;
        public readonly ConfigEntry<string> LeftShoulder;
        public readonly ConfigEntry<string> RightShoulder;
        public readonly ConfigEntry<string> LeftTrigger;
        public readonly ConfigEntry<string> RightTrigger;
        public readonly ConfigEntry<string> LeftStickPress;
        public readonly ConfigEntry<string> RightStickPress;
        public readonly ConfigEntry<string> Start;
        public readonly ConfigEntry<string> Select;
        public readonly ConfigEntry<string> DpadUp;
        public readonly ConfigEntry<string> DpadDown;
        public readonly ConfigEntry<string> DpadLeft;
        public readonly ConfigEntry<string> DpadRight;
        // Sticks: exactly four entries "up;down;left;right" (keyboard keys only). Empty = no keyboard binding.
        public readonly ConfigEntry<string> LeftStick;
        public readonly ConfigEntry<string> RightStick;

        public KbmConfig(ConfigFile f)
        {
            DumpAsset = f.Bind("Debug", "DumpInputAsset", false,
                "Write the game's InputActionAsset JSON (before and after injection) to BepInEx/woa-kbm/ on startup.");
            StripControlSchemes = f.Bind("Advanced", "StripControlSchemes", true,
                "Remove control schemes from the input asset so PlayerInput pairs every device that has bindings (keyboard, mouse and gamepad at once).");
            AllowAllDevices = f.Bind("Advanced", "AllowAllDevices", true,
                "Clear InputSystem.settings.supportedDevices. The game ships with a gamepad-only whitelist, which is why keyboard and mouse are dead without this.");
            InjectBindings = f.Bind("Advanced", "InjectBindings", true,
                "Diagnostic: when false, the input asset is left untouched (no keyboard bindings, no control-scheme removal). Devices are still enabled.");
            ExcludeActions = f.Bind("Advanced", "ExcludeActions", "DebugMenu",
                "Action names that never receive keyboard/mouse bindings (';'-separated).");

            LockCursor = f.Bind("Mouse", "LockCursor", true,
                "Lock and hide the mouse cursor while the game window is focused (needed for mouse look).");
            CursorUnlockKey = f.Bind("Mouse", "CursorUnlockHoldKey", "leftAlt",
                "Hold this keyboard key to temporarily release the cursor. Empty disables.");
            MouseLookActions = f.Bind("Mouse", "MouseLookActions", "Look;EventLook",
                "Action names (any map) that receive <Mouse>/delta in addition to the right-stick keys.");
            MouseSensitivity = f.Bind("Mouse", "Sensitivity", 0.08f,
                "Scale applied to raw mouse delta (pixels per frame) before it is fed to the camera as a stick value.");
            MouseInvertY = f.Bind("Mouse", "InvertY", false, "Invert vertical mouse look.");

            KbmPrompts = f.Bind("Prompts", "KeyboardPrompts", true,
                "Show key names instead of controller glyphs in button prompts while the keyboard/mouse is the last-used device.");
            TextPromptFormat = f.Bind("Prompts", "TextPromptFormat", "[{0}]",
                "Format for key names inside dialogue/tip text. {0} is the key label. TextMeshPro rich text allowed.");
            GlyphCharMap = f.Bind("Prompts", "GlyphCharMap", "",
                "Extra text glyph characters to rewrite, as hex code = control, ';'-separated (e.g. \"E001=buttonSouth;E002=buttonEast\"). Controls: buttonSouth, buttonEast, buttonWest, buttonNorth, leftShoulder, rightShoulder, leftTrigger, rightTrigger, leftStickPress, rightStickPress, start, select, dpad/up, dpad/down, dpad/left, dpad/right, leftStick, rightStick. Unmapped glyphs are logged.");

            DumpGlyphs = f.Bind("Prompts", "DumpGlyphs", false,
                "Write the game's original controller glyph sprites as PNGs to BepInEx/woa-kbm/glyphs/ (for making replacement key images).");
            CustomKeyImageDir = f.Bind("Prompts", "CustomKeyImageDir", "",
                "Folder with replacement key images named by label: E.png, Esc.png, Shift.png, Tab.png, LMB.png, RMB.png, up.png... Empty = BepInEx/plugins/WoaKbm/keys. When a file exists it is used instead of the drawn key cap.");

            WalkModifierKey = f.Bind("Keyboard", "WalkModifierKey", "leftCtrl",
                "Hold this key to walk instead of run (keyboard movement is always full-magnitude, which the game treats as running). Empty disables.");
            SuppressCancelOnMenuKey = f.Bind("Keyboard", "SuppressCancelOnMenuKey", true,
                "When a key is bound to both B (cancel) and Start (open menu), ignore the B press in the field on the frame the menu key fires. Lets Escape be 'back' in menus and 'menu' in the field without also dismounting etc.");

            // Game semantics (Xbox labels): A=confirm/interact, B=cancel/dismount, X=item bag/X-prompts, Y=Y-prompts/skip,
            // LB=photo mode/left tab, RB=sneak/right tab, LT=zoom in/left tab, RT=dash/zoom out/right tab,
            // L3=camera reset, R3=top-down camera, Start=menu, Select=doc pad/album, D-pad=quick actions/menu nav.
            ButtonSouth     = f.Bind("Keys", "A (buttonSouth)",       "f;enter;<Mouse>/leftButton", "Confirm / interact / use");
            ButtonEast      = f.Bind("Keys", "B (buttonEast)",        "escape;<Mouse>/rightButton", "Cancel / back / dismount");
            ButtonWest      = f.Bind("Keys", "X (buttonWest)",        "tab;i", "Item bag / X prompts");
            ButtonNorth     = f.Bind("Keys", "Y (buttonNorth)",       "r", "Y prompts / skip event");
            LeftShoulder    = f.Bind("Keys", "LB (leftShoulder)",     "q", "Photo mode / previous tab");
            RightShoulder   = f.Bind("Keys", "RB (rightShoulder)",    "e", "Sneak / next tab");
            LeftTrigger     = f.Bind("Keys", "LT (leftTrigger)",      "z;<Mouse>/scroll/up", "Camera zoom in / previous tab");
            RightTrigger    = f.Bind("Keys", "RT (rightTrigger)",     "leftShift;<Mouse>/scroll/down", "Dash / camera zoom out / next tab");
            LeftStickPress  = f.Bind("Keys", "L3 (leftStickPress)",   "home;<Mouse>/middleButton", "Reset camera");
            RightStickPress = f.Bind("Keys", "R3 (rightStickPress)",  "v", "Top-down camera");
            Start           = f.Bind("Keys", "Start",                 "escape", "Open menu / options");
            Select          = f.Bind("Keys", "Select",                "m", "Doc pad / album");
            DpadUp          = f.Bind("Keys", "D-pad up",              "upArrow", "");
            DpadDown        = f.Bind("Keys", "D-pad down",            "downArrow", "");
            DpadLeft        = f.Bind("Keys", "D-pad left",            "leftArrow", "");
            DpadRight       = f.Bind("Keys", "D-pad right",           "rightArrow", "");
            LeftStick       = f.Bind("Keys", "Left stick",            "w;s;a;d", "Move / menu navigation as up;down;left;right");
            RightStick      = f.Bind("Keys", "Right stick",           "upArrow;downArrow;leftArrow;rightArrow", "Right stick in menus (category/value selectors) as up;down;left;right keys. Camera look always uses the mouse and ignores this.");
        }
    }

    /// <summary>
    /// Works around MonoMod issue #329 (fixed upstream Oct 2026, not yet in BepInEx 5's bundled MonoMod):
    /// on newer 32-bit Unity Mono, Assembly.SetMonoCorlibInternal(true) writes its flag byte at the wrong
    /// offset of the native MonoAssembly struct, landing on the friend_assembly_names pointer (+0x4C) and
    /// turning a null list into 0x01000000. Mono then crashes at domain teardown (mono_domain_free ->
    /// mono_assembly_close_except_image_pools). BepInEx's own hooks trigger it, so even zero plugins crash.
    /// The verified mitigation is restoring that word, which is what this does.
    /// </summary>
    internal static class MonoAssemblyRepair
    {
        private const int FriendListOffsetX86 = 0x4C;
        private const int Poison = 0x01000000;

        public static void Run(string when)
        {
            if (IntPtr.Size != 4) return; // offsets are for x86 Mono; this game is x86
            int repaired = 0, scanned = 0;
            try
            {
                System.Reflection.FieldInfo field = null;
                foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
                {
                    if (field == null || !field.DeclaringType.IsInstanceOfType(asm))
                    {
                        field = FindMonoAssemblyField(asm.GetType());
                        if (field == null) continue;
                    }
                    IntPtr native;
                    try { native = (IntPtr)field.GetValue(asm); } catch { continue; }
                    if (native == IntPtr.Zero) continue;
                    scanned++;
                    int value = System.Runtime.InteropServices.Marshal.ReadInt32(native, FriendListOffsetX86);
                    if (value == Poison)
                    {
                        System.Runtime.InteropServices.Marshal.WriteInt32(native, FriendListOffsetX86, 0);
                        repaired++;
                        Plugin.Log.LogDebug("Repaired friend_assembly_names on " + asm.GetName().Name);
                    }
                }
            }
            catch (Exception e)
            {
                Plugin.Log.LogWarning("MonoAssembly repair failed: " + e.Message);
                return;
            }
            Plugin.Log.LogInfo("MonoAssembly repair (" + when + "): scanned " + scanned + ", repaired " + repaired + ".");
        }

        private static System.Reflection.FieldInfo FindMonoAssemblyField(Type t)
        {
            for (var cur = t; cur != null; cur = cur.BaseType)
            {
                var f = cur.GetField("_mono_assembly", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public);
                if (f != null && f.FieldType == typeof(IntPtr)) return f;
            }
            return null;
        }
    }

    internal static class CursorControl
    {
        private static bool _wasLocked;

        private static bool InGameplayMap()
        {
            try
            {
                if (!SingletonMonoBehaviour<input.InputSystemManager>.ExistsInstance) return false;
                var id = SingletonMonoBehaviour<input.InputSystemManager>.Instance.GetCurrentActionSetId();
                return id == input.InputSystemManager.ActionSetId.Player
                    || id == input.InputSystemManager.ActionSetId.Photo
                    || id == input.InputSystemManager.ActionSetId.Event;
            }
            catch
            {
                return false;
            }
        }

        public static void Tick()
        {
            var cfg = Plugin.Cfg;
            if (cfg == null || !cfg.LockCursor.Value)
            {
                return;
            }

            // Only lock during actual gameplay (camera-controlled action maps), never while booting, loading or in menus.
            bool wantLock = Application.isFocused && DeviceTracker.UsingKbm && InGameplayMap();
            string unlockKey = cfg.CursorUnlockKey.Value;
            if (wantLock && !string.IsNullOrEmpty(unlockKey) && Keyboard.current != null)
            {
                var ctrl = Keyboard.current.TryGetChildControl(unlockKey) as UnityEngine.InputSystem.Controls.ButtonControl;
                if (ctrl != null && ctrl.isPressed)
                {
                    wantLock = false;
                }
            }

            if (wantLock)
            {
                if (Cursor.lockState != CursorLockMode.Locked)
                {
                    Cursor.lockState = CursorLockMode.Locked;
                }
                if (Cursor.visible)
                {
                    Cursor.visible = false;
                }
                _wasLocked = true;
            }
            else if (_wasLocked)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = false; // the game hides it anyway
                _wasLocked = false;
            }
        }
    }

    /// <summary>Tracks whether the player last used keyboard/mouse or a gamepad.</summary>
    internal static class DeviceTracker
    {
        public static bool UsingKbm { get; private set; } = true;
        public static event Action<bool> Changed;

        private static bool _installed;

        public static void Install()
        {
            if (_installed) return;
            _installed = true;
            InputSystem.onActionChange += OnActionChange;
        }

        public static void Uninstall()
        {
            if (!_installed) return;
            _installed = false;
            InputSystem.onActionChange -= OnActionChange;
        }

        public static void Init(bool kbm)
        {
            if (kbm == UsingKbm) return;
            UsingKbm = kbm;
            Changed?.Invoke(kbm);
        }

        private static void OnActionChange(object obj, InputActionChange change)
        {
            if (change != InputActionChange.ActionPerformed && change != InputActionChange.ActionStarted) return;
            var action = obj as InputAction;
            var control = action?.activeControl;
            var device = control?.device;
            if (device == null) return;
            bool kbm = device is Keyboard || device is Mouse;
            bool pad = device is Gamepad;
            if (!kbm && !pad) return;
            if (kbm == UsingKbm) return;
            // Only deliberate input flips the mode: a pressed key/button, or a stick pushed past half.
            // Mouse motion and stick drift from an idle, plugged-in pad must not count.
            if (control is UnityEngine.InputSystem.Controls.ButtonControl b)
            {
                if (!b.isPressed) return;
            }
            else if (control is UnityEngine.InputSystem.Controls.Vector2Control v2)
            {
                // sticks and the d-pad (DpadControl is a Vector2Control); mouse delta is a DeltaControl, excluded below
                if (control is UnityEngine.InputSystem.Controls.DeltaControl) return;
                if (v2.ReadValue().magnitude < 0.5f) return;
            }
            else if (control is UnityEngine.InputSystem.Controls.AxisControl a)
            {
                if (Mathf.Abs(a.ReadValue()) < 0.5f) return; // triggers
            }
            else
            {
                return;
            }
            UsingKbm = kbm;
            Plugin.Log.LogInfo("Input device switched to " + (kbm ? "keyboard/mouse" : "gamepad"));
            Changed?.Invoke(kbm);
        }
    }
}

