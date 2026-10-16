using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BepInEx;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Utilities;

namespace WoaKbm
{
    /// <summary>
    /// Adds keyboard/mouse bindings to the game's InputActionAsset.
    ///
    /// The game names its actions after Nintendo button labels and binds confirm/cancel through
    /// the generic usages "*/{Submit}" and "*/{Cancel}", with per-layout bindings for Xbox, PS and
    /// Switch controllers. We canonicalise on the Xbox physical layout: each action's Xbox-layout
    /// (or usage) bindings are translated to keys; Nintendo-layout bindings are ignored because the
    /// same action lands on a differently-named physical button there.
    ///
    /// Runs before PlayerInput.OnEnable so device pairing sees the new bindings.
    /// </summary>
    internal static class KbmInjector
    {
        private static readonly HashSet<int> _done = new HashSet<int>();

        /// <summary>Layouts whose physical button names match the Xbox layout.</summary>
        private static readonly string[] XboxLikeLayouts =
        {
            "Gamepad", "XInputController", "GXDKGamepad", "DualShockGamepad", "PS4DualShockGamepad", "PS5DualSenseGamepad"
        };

        public static void Apply(PlayerInput playerInput)
        {
            var log = Plugin.Log;
            var cfg = Plugin.Cfg;

            if (playerInput == null)
            {
                log.LogWarning("InputSystemManager has no PlayerInput component; nothing to patch.");
                return;
            }
            var asset = playerInput.actions;
            if (asset == null)
            {
                log.LogWarning("PlayerInput has no actions asset; nothing to patch.");
                return;
            }
            if (!_done.Add(asset.GetInstanceID()))
            {
                log.LogInfo("Asset already patched; skipping.");
                return;
            }

            string dumpDir = Path.Combine(Paths.BepInExRootPath, "woa-kbm");
            if (cfg.DumpAsset.Value)
            {
                try
                {
                    Directory.CreateDirectory(dumpDir);
                    File.WriteAllText(Path.Combine(dumpDir, "actions.before.json"), asset.ToJson());
                }
                catch (Exception e)
                {
                    log.LogError("Dump failed: " + e);
                }
            }

            if (cfg.AllowAllDevices.Value && InputSystem.settings.supportedDevices.Count > 0)
            {
                log.LogInfo("Clearing supportedDevices (was: " + string.Join(", ", InputSystem.settings.supportedDevices) + ")");
                InputSystem.settings.supportedDevices = new ReadOnlyArray<string>();
            }
            log.LogInfo("Devices now: " + string.Join(", ", InputSystem.devices.Select(d => d.layout + ":" + d.name)));

            if (!cfg.InjectBindings.Value)
            {
                log.LogWarning("InjectBindings=false: leaving the input asset untouched (diagnostic mode).");
                DeviceTracker.Init(Gamepad.current == null);
                return;
            }

            if (cfg.StripControlSchemes.Value)
            {
                foreach (var name in asset.controlSchemes.Select(s => s.name).ToList())
                {
                    asset.RemoveControlScheme(name);
                }
                log.LogInfo("Removed control schemes from asset.");
            }

            int erased = 0, added = 0;
            foreach (var map in asset.actionMaps)
            {
                erased += EraseTemplateBindings(map);
            }
            foreach (var map in asset.actionMaps)
            {
                added += InjectMap(map);
            }
            log.LogInfo("Erased " + erased + " leftover non-gamepad template bindings; injected " + added + " keyboard/mouse bindings.");

            // If a controller was present, PlayerInput.OnEnable (which runs before this) already created an
            // InputUser with the "Gamepad" scheme active. That puts a binding mask and a device whitelist on the
            // asset, which hides every binding we just added. Drop the user so the actions resolve against all devices.
            try
            {
                if (playerInput.user.valid)
                {
                    log.LogInfo("Dropping PlayerInput user (scheme=" + (playerInput.currentControlScheme ?? "none") + ") so keyboard/mouse bindings are not masked.");
                    playerInput.user.UnpairDevicesAndRemoveUser();
                }
                asset.bindingMask = null;
                asset.devices = null;
            }
            catch (Exception e)
            {
                log.LogWarning("Could not reset PlayerInput user: " + e.Message);
            }
            DeviceTracker.Init(Gamepad.current == null);

            if (cfg.DumpAsset.Value)
            {
                try
                {
                    File.WriteAllText(Path.Combine(dumpDir, "actions.after.json"), asset.ToJson());
                    var info = new List<string>
                    {
                        "asset: " + asset.name,
                        "defaultControlScheme: " + playerInput.defaultControlScheme,
                        "defaultActionMap: " + playerInput.defaultActionMap,
                        "neverAutoSwitchControlSchemes: " + playerInput.neverAutoSwitchControlSchemes,
                        "devices: " + string.Join(", ", InputSystem.devices.Select(d => d.layout + ":" + d.name)),
                    };
                    File.WriteAllLines(Path.Combine(dumpDir, "playerinput.txt"), info);
                }
                catch (Exception e)
                {
                    log.LogError("Dump (after) failed: " + e);
                }
            }
        }

        /// <summary>
        /// The asset was started from Unity's default template and still carries a few raw keyboard,
        /// mouse, touch, joystick and XR bindings (unscaled pointer delta on Look, left click on the
        /// dash trigger...). Remove them so our config is the single source of truth.
        /// </summary>
        private static int EraseTemplateBindings(InputActionMap map)
        {
            int erased = 0;
            // Walk backwards so indices stay valid while erasing. Composite heads must be erased
            // after their parts (which come after them), which backwards iteration guarantees.
            var bindings = map.bindings;
            for (int i = bindings.Count - 1; i >= 0; i--)
            {
                var b = bindings[i];
                if (string.IsNullOrEmpty(b.action)) continue;
                var action = map.FindAction(b.action);
                if (action == null) continue;

                bool remove = false;
                if (b.isComposite)
                {
                    // A composite whose parts were all non-gamepad is now empty: remove the head too.
                    bool hasParts = i + 1 < bindings.Count && bindings[i + 1].isPartOfComposite;
                    remove = !hasParts;
                }
                else
                {
                    remove = IsNonGamepadPath(b.path);
                }

                if (remove)
                {
                    int idx = IndexInAction(action, b.id);
                    if (idx >= 0)
                    {
                        action.ChangeBinding(idx).Erase();
                        erased++;
                        bindings = map.bindings;
                    }
                }
            }
            return erased;
        }

        private static int IndexInAction(InputAction action, Guid id)
        {
            var bs = action.bindings;
            for (int i = 0; i < bs.Count; i++)
            {
                if (bs[i].id == id) return i;
            }
            return -1;
        }

        private static bool IsNonGamepadPath(string path)
        {
            if (string.IsNullOrEmpty(path)) return false;
            return path.StartsWith("<Keyboard>", StringComparison.OrdinalIgnoreCase)
                || path.StartsWith("<Mouse>", StringComparison.OrdinalIgnoreCase)
                || path.StartsWith("<Pointer>", StringComparison.OrdinalIgnoreCase)
                || path.StartsWith("<Touchscreen>", StringComparison.OrdinalIgnoreCase)
                || path.StartsWith("<Joystick>", StringComparison.OrdinalIgnoreCase)
                || path.StartsWith("<XRController>", StringComparison.OrdinalIgnoreCase)
                || path.StartsWith("<Pen>", StringComparison.OrdinalIgnoreCase);
        }

        private static int InjectMap(InputActionMap map)
        {
            var cfg = Plugin.Cfg;
            var log = Plugin.Log;
            int added = 0;

            var excluded = new HashSet<string>(Split(cfg.ExcludeActions.Value), StringComparer.OrdinalIgnoreCase);
            var mouseLookActions = new HashSet<string>(Split(cfg.MouseLookActions.Value), StringComparer.OrdinalIgnoreCase);

            // Snapshot: we'll be mutating the map while iterating.
            var bindings = map.bindings.ToArray();

            // Per action: paths already present or added (dedupe).
            var have = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
            foreach (var b in bindings)
            {
                if (b.isComposite || string.IsNullOrEmpty(b.action)) continue;
                Set(have, b.action).Add(b.path);
            }

            for (int i = 0; i < bindings.Length; i++)
            {
                var b = bindings[i];
                if (string.IsNullOrEmpty(b.action)) continue;
                if (excluded.Contains(b.action)) continue;
                var action = map.FindAction(b.action);
                if (action == null) continue;

                if (b.isComposite)
                {
                    // e.g. 2DVector built from <Gamepad>/leftStick/up..., or OneModifier(RT + select).
                    var parts = new List<InputBinding>();
                    for (int j = i + 1; j < bindings.Length && bindings[j].isPartOfComposite; j++)
                    {
                        parts.Add(bindings[j]);
                    }
                    i += parts.Count;

                    // Each part becomes one or more parts with the same part name.
                    var translated = new List<KeyValuePair<string, string>>();
                    foreach (var p in parts)
                    {
                        string canon = Canonical(p.path);
                        if (canon == null) continue;
                        foreach (var kp in KeysFor(canon))
                        {
                            if (kp.StartsWith("<Mouse>/scroll", StringComparison.OrdinalIgnoreCase)) continue; // not button-like
                            translated.Add(new KeyValuePair<string, string>(p.name, kp));
                        }
                    }
                    if (translated.Count == 0) continue;
                    // Only add if the composite would be complete for every part name.
                    var partNames = parts.Select(p => p.name).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
                    if (partNames.Any(pn => !translated.Any(t => string.Equals(t.Key, pn, StringComparison.OrdinalIgnoreCase)))) continue;

                    string key = "composite:" + b.path + ":" + string.Join(",", translated.Select(t => t.Key + "=" + t.Value));
                    if (!Set(have, action.name).Add(key)) continue;

                    var syntax = action.AddCompositeBinding(b.path, b.interactions, b.processors);
                    foreach (var t in translated)
                    {
                        syntax = syntax.With(t.Key, t.Value);
                    }
                    added++;
                    continue;
                }
                if (b.isPartOfComposite) continue;

                string control = Canonical(b.path);
                if (control == null) continue;

                if (control == "leftStick" || control == "rightStick" || control == "dpad")
                {
                    bool isLook = control == "rightStick" && mouseLookActions.Contains(action.name);
                    var keys = isLook ? null : StickKeys(control); // camera actions are mouse-only
                    if (keys != null)
                    {
                        string key = "stick:" + control;
                        if (Set(have, action.name).Add(key))
                        {
                            action.AddCompositeBinding("2DVector(mode=2)", b.interactions)
                                .With("Up", keys[0]).With("Down", keys[1]).With("Left", keys[2]).With("Right", keys[3]);
                            added++;
                        }
                    }
                    if (control == "rightStick" && mouseLookActions.Contains(action.name))
                    {
                        if (Set(have, action.name).Add("<Mouse>/delta"))
                        {
                            float s = cfg.MouseSensitivity.Value;
                            float sy = cfg.MouseInvertY.Value ? -s : s;
                            string proc = string.Format(System.Globalization.CultureInfo.InvariantCulture,
                                "scaleVector2(x={0},y={1})", s, sy);
                            action.AddBinding("<Mouse>/delta", b.interactions, proc);
                            added++;
                        }
                    }
                    continue;
                }

                foreach (var path in KeysFor(control))
                {
                    if (!Set(have, action.name).Add(path)) continue;
                    string processors = b.processors;
                    if (path.StartsWith("<Mouse>/scroll", StringComparison.OrdinalIgnoreCase))
                    {
                        // Scroll deltas are large (±120 per notch); turn them into a 0..1 pulse.
                        processors = "clamp(min=0,max=1)";
                    }
                    action.AddBinding(path, b.interactions, processors);
                    added++;
                }
            }

            if (added > 0)
            {
                log.LogDebug("Map '" + map.name + "': +" + added);
            }
            return added;
        }

        private static HashSet<string> Set(Dictionary<string, HashSet<string>> d, string key)
        {
            if (!d.TryGetValue(key, out var set))
            {
                d[key] = set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            }
            return set;
        }

        /// <summary>
        /// Map a binding path to a canonical Xbox-layout control name, or null if it should be ignored.
        ///   "&lt;Gamepad&gt;/dpad/up"            → "dpad/up"
        ///   "&lt;XInputController&gt;/buttonWest" → "buttonWest"
        ///   "*/{Submit}"                       → "buttonSouth"
        ///   "*/{Cancel}"                       → "buttonEast"
        ///   "&lt;PS5DualSenseGamepad&gt;/touchpadButton" → "select"
        ///   "&lt;SwitchProControllerHID&gt;/buttonEast"  → null (Nintendo layout)
        /// </summary>
        private static string Canonical(string path)
        {
            if (string.IsNullOrEmpty(path)) return null;
            if (path.StartsWith("*/{", StringComparison.Ordinal))
            {
                string usage = path.Substring(3).TrimEnd('}');
                if (usage.Equals("Submit", StringComparison.OrdinalIgnoreCase)) return "buttonSouth";
                if (usage.Equals("Cancel", StringComparison.OrdinalIgnoreCase)) return "buttonEast";
                if (usage.Equals("Menu", StringComparison.OrdinalIgnoreCase)) return "start";
                return null;
            }
            if (!path.StartsWith("<")) return null;
            int close = path.IndexOf('>');
            if (close < 0 || close + 1 >= path.Length || path[close + 1] != '/') return null;
            string layout = path.Substring(1, close - 1);
            if (!XboxLikeLayouts.Contains(layout)) return null;
            string control = path.Substring(close + 2);
            if (control == "touchpadButton") return "select";
            return control;
        }

        private static string[] StickKeys(string control)
        {
            var cfg = Plugin.Cfg;
            string raw;
            switch (control)
            {
                case "leftStick": raw = cfg.LeftStick.Value; break;
                case "rightStick": raw = cfg.RightStick.Value; break;
                case "dpad":
                    raw = First(cfg.DpadUp.Value) + ";" + First(cfg.DpadDown.Value) + ";" + First(cfg.DpadLeft.Value) + ";" + First(cfg.DpadRight.Value);
                    break;
                default: return null;
            }
            var parts = Split(raw).Select(ToPath).ToArray();
            return parts.Length == 4 ? parts : null;
        }

        private static string First(string raw)
        {
            return Split(raw).FirstOrDefault() ?? "";
        }

        /// <summary>Keyboard/mouse control paths configured for one canonical Xbox control.</summary>
        private static IEnumerable<string> KeysFor(string control)
        {
            var cfg = Plugin.Cfg;
            string raw = null;
            switch (control)
            {
                case "buttonSouth": raw = cfg.ButtonSouth.Value; break;
                case "buttonEast": raw = cfg.ButtonEast.Value; break;
                case "buttonWest": raw = cfg.ButtonWest.Value; break;
                case "buttonNorth": raw = cfg.ButtonNorth.Value; break;
                case "leftShoulder": raw = cfg.LeftShoulder.Value; break;
                case "rightShoulder": raw = cfg.RightShoulder.Value; break;
                case "leftTrigger": raw = cfg.LeftTrigger.Value; break;
                case "rightTrigger": raw = cfg.RightTrigger.Value; break;
                case "leftStickPress": raw = cfg.LeftStickPress.Value; break;
                case "rightStickPress": raw = cfg.RightStickPress.Value; break;
                case "start": raw = cfg.Start.Value; break;
                case "select": raw = cfg.Select.Value; break;
                case "dpad/up": raw = cfg.DpadUp.Value; break;
                case "dpad/down": raw = cfg.DpadDown.Value; break;
                case "dpad/left": raw = cfg.DpadLeft.Value; break;
                case "dpad/right": raw = cfg.DpadRight.Value; break;
                case "leftStick/up": raw = Pick(cfg.LeftStick.Value, 0); break;
                case "leftStick/down": raw = Pick(cfg.LeftStick.Value, 1); break;
                case "leftStick/left": raw = Pick(cfg.LeftStick.Value, 2); break;
                case "leftStick/right": raw = Pick(cfg.LeftStick.Value, 3); break;
                case "rightStick/up": raw = Pick(cfg.RightStick.Value, 0); break;
                case "rightStick/down": raw = Pick(cfg.RightStick.Value, 1); break;
                case "rightStick/left": raw = Pick(cfg.RightStick.Value, 2); break;
                case "rightStick/right": raw = Pick(cfg.RightStick.Value, 3); break;
                default:
                    Plugin.Log.LogDebug("No KBM translation for gamepad control '" + control + "'");
                    return Enumerable.Empty<string>();
            }
            return Split(raw).Select(ToPath).ToList();
        }

        private static string Pick(string raw, int idx)
        {
            var parts = Split(raw).ToArray();
            return idx < parts.Length ? parts[idx] : null;
        }

        internal static IEnumerable<string> Split(string raw)
        {
            if (string.IsNullOrEmpty(raw)) yield break;
            foreach (var s in raw.Split(';'))
            {
                var t = s.Trim();
                if (t.Length > 0) yield return t;
            }
        }

        internal static string ToPath(string item)
        {
            return item.StartsWith("<") ? item : "<Keyboard>/" + item;
        }
    }
}
