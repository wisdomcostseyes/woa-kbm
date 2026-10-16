using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Msg;
using Table;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using ui;

namespace WoaKbm
{
    /// <summary>Human-readable labels for the keys bound to each canonical gamepad control.</summary>
    internal static class KeyLabels
    {
        private static readonly Dictionary<string, string> KeyNames = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "escape", "Esc" }, { "enter", "Enter" }, { "space", "Space" }, { "tab", "Tab" }, { "backspace", "Bksp" },
            { "leftShift", "Shift" }, { "rightShift", "RShift" }, { "leftCtrl", "Ctrl" }, { "rightCtrl", "RCtrl" },
            { "leftAlt", "Alt" }, { "rightAlt", "RAlt" }, { "home", "Home" }, { "end", "End" }, { "insert", "Ins" }, { "delete", "Del" },
            { "pageUp", "PgUp" }, { "pageDown", "PgDn" }, { "capsLock", "Caps" },
            { "upArrow", "↑" }, { "downArrow", "↓" }, { "leftArrow", "←" }, { "rightArrow", "→" },
            { "leftButton", "LMB" }, { "rightButton", "RMB" }, { "middleButton", "MMB" },
            { "scroll/up", "Wheel↑" }, { "scroll/down", "Wheel↓" }, { "delta", "Mouse" },
        };

        /// <summary>"&lt;Keyboard&gt;/leftShift" → "Shift", "e" → "E".</summary>
        public static string ForPath(string path)
        {
            if (string.IsNullOrEmpty(path)) return "?";
            string name = path;
            int slash = path.IndexOf("/", StringComparison.Ordinal);
            if (path.StartsWith("<") && slash >= 0) name = path.Substring(slash + 1);
            if (KeyNames.TryGetValue(name, out var label)) return label;
            if (name.Length == 1) return name.ToUpperInvariant();
            if (name.StartsWith("digit") && name.Length == 6) return name.Substring(5);
            if (name.StartsWith("numpad")) return "Num" + name.Substring(6);
            if (name.StartsWith("f") && name.Length <= 3 && int.TryParse(name.Substring(1), out _)) return name.ToUpperInvariant();
            return char.ToUpperInvariant(name[0]) + name.Substring(1);
        }

        /// <summary>Label for a canonical Xbox control ("buttonSouth", "dpad/up", "leftStick", ...).</summary>
        public static string ForControl(string control)
        {
            var cfg = Plugin.Cfg;
            string raw;
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
                case "dpad": return Stick(cfg.DpadUp.Value, cfg.DpadDown.Value, cfg.DpadLeft.Value, cfg.DpadRight.Value);
                case "dpad/ud": return ForPath(First(cfg.DpadUp.Value)) + ForPath(First(cfg.DpadDown.Value));
                case "dpad/lr": return ForPath(First(cfg.DpadLeft.Value)) + ForPath(First(cfg.DpadRight.Value));
                case "leftStick":
                {
                    var p = KbmInjector.Split(cfg.LeftStick.Value).ToArray();
                    return p.Length == 4 ? Stick(p[0], p[1], p[2], p[3]) : "Move";
                }
                case "leftStick/ud":
                {
                    var p = KbmInjector.Split(cfg.LeftStick.Value).ToArray();
                    return p.Length == 4 ? ForPath(p[0]) + "/" + ForPath(p[1]) : "Move";
                }
                case "leftStick/lr":
                {
                    var p = KbmInjector.Split(cfg.LeftStick.Value).ToArray();
                    return p.Length == 4 ? ForPath(p[2]) + "/" + ForPath(p[3]) : "Move";
                }
                case "rightStick":
                {
                    var p = KbmInjector.Split(cfg.RightStick.Value).ToArray();
                    return p.Length == 4 ? Stick(p[0], p[1], p[2], p[3]) : "Mouse";
                }
                case "rightStick/ud":
                {
                    var p = KbmInjector.Split(cfg.RightStick.Value).ToArray();
                    return p.Length == 4 ? ForPath(p[0]) + ForPath(p[1]) : "Mouse";
                }
                case "rightStick/lr":
                {
                    var p = KbmInjector.Split(cfg.RightStick.Value).ToArray();
                    return p.Length == 4 ? ForPath(p[2]) + ForPath(p[3]) : "Mouse";
                }
                case "leftStick/up": return StickPart(cfg.LeftStick.Value, 0);
                case "leftStick/down": return StickPart(cfg.LeftStick.Value, 1);
                case "leftStick/left": return StickPart(cfg.LeftStick.Value, 2);
                case "leftStick/right": return StickPart(cfg.LeftStick.Value, 3);
                case "rightStick/up": return StickPart(cfg.RightStick.Value, 0);
                case "rightStick/down": return StickPart(cfg.RightStick.Value, 1);
                case "rightStick/left": return StickPart(cfg.RightStick.Value, 2);
                case "rightStick/right": return StickPart(cfg.RightStick.Value, 3);
                default: return "?";
            }
            return ForPath(First(raw));
        }

        private static string StickPart(string raw, int idx)
        {
            var p = KbmInjector.Split(raw).ToArray();
            return p.Length == 4 ? ForPath(p[idx]) : "?";
        }

        private static string Stick(string u, string d, string l, string r)
        {
            string lu = ForPath(First(u)), ld = ForPath(First(d)), ll = ForPath(First(l)), lr = ForPath(First(r));
            if (lu == "↑") return "↑↓←→"; // arrow keys read best in this order
            string s = lu + ll + ld + lr; // e.g. WASD
            return s.Length <= 4 ? s : lu + "/" + ld + "/" + ll + "/" + lr;
        }

        private static string First(string raw)
        {
            return KbmInjector.Split(raw).FirstOrDefault() ?? "";
        }

        /// <summary>Text-prompt table (MSGKEYACTIONPARAM.Defs, Switch labels) → canonical Xbox control.</summary>
        public static string ControlForMsgKeyAction(int idx)
        {
            switch ((MSGKEYACTIONPARAM.Defs)idx)
            {
                case MSGKEYACTIONPARAM.Defs.ZLbtn: return "leftTrigger";
                case MSGKEYACTIONPARAM.Defs.Lbtn: return "leftShoulder";
                case MSGKEYACTIONPARAM.Defs.L3btn: return "leftStickPress";
                case MSGKEYACTIONPARAM.Defs.Lstick: return "leftStick";
                case MSGKEYACTIONPARAM.Defs.Cross: return "dpad";
                case MSGKEYACTIONPARAM.Defs.Down: return "dpad/down";
                case MSGKEYACTIONPARAM.Defs.Minus: return "select";
                case MSGKEYACTIONPARAM.Defs.Abtn: return "buttonSouth";   // Switch A = confirm = Xbox A
                case MSGKEYACTIONPARAM.Defs.Bbtn: return "buttonEast";    // Switch B = cancel  = Xbox B
                case MSGKEYACTIONPARAM.Defs.Xbtn: return "buttonWest";    // Switch X → game "North" → Xbox X
                case MSGKEYACTIONPARAM.Defs.Ybtn: return "buttonNorth";   // Switch Y → game "West"  → Xbox Y
                case MSGKEYACTIONPARAM.Defs.Plus: return "start";
                case MSGKEYACTIONPARAM.Defs.Rstick: return "rightStick";
                case MSGKEYACTIONPARAM.Defs.R3btn: return "rightStickPress";
                case MSGKEYACTIONPARAM.Defs.Rbtn: return "rightShoulder";
                case MSGKEYACTIONPARAM.Defs.ZRbtn: return "rightTrigger";
                case MSGKEYACTIONPARAM.Defs.Left: return "dpad/left";
                case MSGKEYACTIONPARAM.Defs.Right: return "dpad/right";
                case MSGKEYACTIONPARAM.Defs.Up: return "dpad/up";
                default: return null;
            }
        }

        /// <summary>
        /// Glyph sprite name (UIASSETPATH, e.g. "icon_guide_btn_A_001_00", "icon_guide_stick_R3_001_00_xbox",
        /// "icon_guide_btn_Circle_001_00_ps") → canonical Xbox control, or null if not a button glyph.
        /// </summary>
        public static string ControlForSpriteName(string spriteName)
        {
            if (string.IsNullOrEmpty(spriteName)) return null;
            int at = spriteName.IndexOf("icon_guide_", StringComparison.OrdinalIgnoreCase);
            if (at < 0) return null;
            string s = spriteName.Substring(at + "icon_guide_".Length);
            bool stick = false;
            foreach (var prefix in new[] { "toptab_btn_", "actioncom_btn_", "btn_", "stick_" })
            {
                if (s.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    stick = prefix == "stick_";
                    s = s.Substring(prefix.Length);
                    break;
                }
            }
            // cut the "_001_00[_ps|_ps5|_xbox]" tail
            bool ps = spriteName.EndsWith("_ps", StringComparison.OrdinalIgnoreCase) || spriteName.EndsWith("_ps5", StringComparison.OrdinalIgnoreCase);
            int tail = s.IndexOf("_0", StringComparison.Ordinal);
            if (tail > 0) s = s.Substring(0, tail);
            s = s.ToLowerInvariant();
            // "btn_stick_L/R" (a stick with a press arrow) is the stick *click* (L3/R3), not stick movement
            if (!stick && s.StartsWith("stick_")) { s = s.Substring(6) + "3"; stick = true; }
            if (ps && s == "x") return "buttonEast"; // PlayStation cross = cancel

            if (stick)
            {
                switch (s)
                {
                    case "l": return "leftStick";
                    case "l3": return "leftStickPress";
                    case "lupdown": return "leftStick/ud";
                    case "lleftright": return "leftStick/lr";
                    case "r": return "rightStick";
                    case "r3": return "rightStickPress";
                    case "rupdown": return "rightStick/ud";
                    case "rleftright": return "rightStick/lr";
                    default: return null;
                }
            }
            switch (s)
            {
                // Switch / Xbox letters are label-mapped by the game: A=confirm, B=cancel, X→Xbox X, Y→Xbox Y
                case "a": case "circle": return "buttonSouth";
                case "b": case "x_ps": return "buttonEast";
                case "x": return "buttonWest";
                case "square": return "buttonWest";
                case "y": case "triangle": return "buttonNorth";
                case "l": case "lb": case "l1": return "leftShoulder";
                case "r": case "rb": case "r1": return "rightShoulder";
                case "zl": case "lt": case "l2": return "leftTrigger";
                case "zr": case "rt": case "r2": return "rightTrigger";
                case "minus": case "back": case "pad": case "view": case "share": case "touchpad": return "select";
                case "plus": case "start": case "option": case "menu": return "start";
                case "dir_updown": return "dpad/ud";
                case "dir_leftright": return "dpad/lr";
                default: return null;
            }
        }
    }

    /// <summary>Sidecar describing where the labels go on a blank glyph shape (pixel boxes, top-left origin).</summary>
    [Serializable]
    internal class GlyphLayout
    {
        public int[] canvas;
        public GlyphLabelBox[] labels;
    }

    [Serializable]
    internal class GlyphLabelBox
    {
        public string control;
        public float x, y, w, h;
    }

    /// <summary>Dumps the game's glyph sprites to PNG and loads user-supplied replacement key images.</summary>
    internal static class GlyphAssets
    {
        private static readonly Dictionary<string, GlyphLayout> _layouts = new Dictionary<string, GlyphLayout>(StringComparer.OrdinalIgnoreCase);

        // The sidecar format is fixed and flat: {"canvas":[W,H],"labels":[{"control":"...","x":..,"y":..,"w":..,"h":..},...]}
        private static readonly System.Text.RegularExpressions.Regex CanvasRx =
            new System.Text.RegularExpressions.Regex(@"""canvas""\s*:\s*\[\s*(\d+)\s*,\s*(\d+)\s*\]");
        private static readonly System.Text.RegularExpressions.Regex BoxRx =
            new System.Text.RegularExpressions.Regex(@"\{\s*""control""\s*:\s*""([^""]+)""\s*,\s*""x""\s*:\s*([-\d.]+)\s*,\s*""y""\s*:\s*([-\d.]+)\s*,\s*""w""\s*:\s*([-\d.]+)\s*,\s*""h""\s*:\s*([-\d.]+)\s*\}");

        private static GlyphLayout ParseLayout(string json)
        {
            var inv = System.Globalization.CultureInfo.InvariantCulture;
            var cm = CanvasRx.Match(json);
            if (!cm.Success) return null;
            var layout = new GlyphLayout { canvas = new[] { int.Parse(cm.Groups[1].Value, inv), int.Parse(cm.Groups[2].Value, inv) } };
            var boxes = new List<GlyphLabelBox>();
            foreach (System.Text.RegularExpressions.Match m in BoxRx.Matches(json))
            {
                boxes.Add(new GlyphLabelBox
                {
                    control = m.Groups[1].Value,
                    x = float.Parse(m.Groups[2].Value, inv),
                    y = float.Parse(m.Groups[3].Value, inv),
                    w = float.Parse(m.Groups[4].Value, inv),
                    h = float.Parse(m.Groups[5].Value, inv),
                });
            }
            layout.labels = boxes.ToArray();
            return layout;
        }

        /// <summary>The label layout for an original glyph, from "&lt;original name&gt;.json" next to the image; null if none.</summary>
        public static GlyphLayout LayoutFor(Sprite like)
        {
            if (like == null) return null;
            string name = CleanSpriteName(like.name);
            if (_layouts.TryGetValue(name, out var cached)) return cached;
            GlyphLayout layout = null;
            try
            {
                string path = System.IO.Path.Combine(CustomDir, name + ".json");
                if (System.IO.File.Exists(path))
                {
                    layout = ParseLayout(System.IO.File.ReadAllText(path));
                }
            }
            catch (Exception e)
            {
                Plugin.Log.LogWarning("Bad glyph sidecar for " + name + ": " + e.Message);
            }
            _layouts[name] = layout;
            return layout;
        }

        private static bool _dumpedAll;
        private static readonly HashSet<string> _dumped = new HashSet<string>();
        private static readonly Dictionary<string, Sprite> _custom = new Dictionary<string, Sprite>(StringComparer.OrdinalIgnoreCase);
        private static readonly HashSet<string> _customMissing = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        public static string DumpDir => System.IO.Path.Combine(BepInEx.Paths.BepInExRootPath, "woa-kbm", "glyphs");
        public static string CustomDir
        {
            get
            {
                var d = Plugin.Cfg.CustomKeyImageDir.Value;
                if (string.IsNullOrEmpty(d)) d = System.IO.Path.Combine(BepInEx.Paths.PluginPath, "WoaKbm", "keys");
                return d;
            }
        }

        private static readonly Dictionary<int, Texture> TextureBySprite = new Dictionary<int, Texture>();
        private static int _failLogs;

        /// <summary>Remember which atlas texture an on-screen Image used for a sprite (sprite.texture can be null with late-bound atlases).</summary>
        public static void RememberTexture(Sprite s, Texture t)
        {
            if (s != null && t != null) TextureBySprite[s.GetInstanceID()] = t;
        }

        /// <summary>Read the glyph's pixels (works for non-readable/compressed textures).</summary>
        public static Texture2D ReadPixels(Sprite s)
        {
            Texture tex = s.texture;
            if (tex == null) TextureBySprite.TryGetValue(s.GetInstanceID(), out tex);
            if (tex == null) throw new InvalidOperationException("sprite has no texture (atlas not bound yet)");
            var tr = s.textureRect;
            int w = Mathf.RoundToInt(tr.width), h = Mathf.RoundToInt(tr.height);
            var rt = RenderTexture.GetTemporary(tex.width, tex.height, 0, RenderTextureFormat.ARGB32);
            var prev = RenderTexture.active;
            Graphics.Blit(tex, rt);
            RenderTexture.active = rt;
            var copy = new Texture2D(w, h, TextureFormat.RGBA32, false);
            copy.ReadPixels(new Rect(tr.x, tr.y, w, h), 0, 0);
            copy.Apply();
            RenderTexture.active = prev;
            RenderTexture.ReleaseTemporary(rt);
            return copy;
        }

        public static void DumpIfWanted(Sprite s)
        {
            if (!Plugin.Cfg.DumpGlyphs.Value || s == null || !_dumped.Add(s.name)) return;
            try
            {
                System.IO.Directory.CreateDirectory(DumpDir);
                var tex = ReadPixels(s);
                var png = ImageConversion.EncodeToPNG(tex);
                UnityEngine.Object.Destroy(tex);
                System.IO.File.WriteAllBytes(System.IO.Path.Combine(DumpDir, s.name + ".png"), png);
            }
            catch (Exception e)
            {
                _dumped.Remove(s.name); // retry later (e.g. once the atlas is bound)
                if (_failLogs++ < 3) Plugin.Log.LogWarning("Glyph dump failed for " + s.name + ": " + e);
            }
        }

        /// <summary>Load every icon_guide sprite the game knows about and dump it. Runs once, after the UI exists.</summary>
        public static void DumpAllOnce()
        {
            if (_dumpedAll || !Plugin.Cfg.DumpGlyphs.Value) return;
            _dumpedAll = true;
            try
            {
                // Sprites loaded individually are atlas-packed and have no texture until the atlas binds, so
                // pull them from the atlases the game has already loaded: those copies are texture-bound.
                // Make sure every UI atlas is loaded, not just the ones the current screen pulled in.
                foreach (var tag in new[] { "ui_common_atlas", "ui_ingame_atlas", "ui_mainmenu_atlas", "ui_othermenu_atlas", "ui_map_atlas", "ui_dressup_atlas" })
                {
                    try { UIManager.OnAtlasRequested(tag, null); }
                    catch (Exception e) { Plugin.Log.LogDebug("Atlas load failed for " + tag + ": " + e.Message); }
                }
                var atlases = HarmonyLib.Traverse.Create(typeof(UIManager)).Field("_atlasList")
                    .GetValue<Dictionary<int, UnityEngine.U2D.SpriteAtlas>>();
                if (atlases == null || atlases.Count == 0) { _dumpedAll = false; return; }
                int n = 0;
                foreach (var atlas in atlases.Values)
                {
                    if (atlas == null) continue;
                    var sprites = new Sprite[atlas.spriteCount];
                    atlas.GetSprites(sprites);
                    foreach (var sp in sprites)
                    {
                        if (sp == null || sp.name.IndexOf("icon_guide", StringComparison.OrdinalIgnoreCase) < 0) continue;
                        sp.name = sp.name.Replace("(Clone)", "");
                        DumpIfWanted(sp);
                        n++;
                    }
                }
                Plugin.Log.LogInfo("Dumped " + n + " glyph sprites from " + atlases.Count + " atlases to " + DumpDir);
            }
            catch (Exception e)
            {
                Plugin.Log.LogWarning("Glyph dump-all failed: " + e);
            }
        }

        /// <summary>
        /// User replacement image for a key label ("E.png", "Esc.png", "LMB.png", "up.png" ...), scaled so it
        /// occupies the same on-screen size as the glyph it replaces. Null if none.
        /// </summary>
        public static Sprite Custom(string label, Sprite like)
        {
            // 1) exact per-original override: "<original sprite name>.png", used as-is (same canvas).
            if (like != null)
            {
                string cleanName = CleanSpriteName(like.name);
                var exact = CustomFile(cleanName, like, exact: true);
                if (exact != null) return exact;
                if (_loggedExactMiss.Add(cleanName))
                {
                    Plugin.Log.LogInfo("No exact key image for glyph '" + like.name + "' (looked for " + cleanName + ".png); falling back to label '" + label + "'.");
                }
            }
            // 2) variant-specific image ("E.actioncom.png" for the solid world-prompt disc,
            //    "Q.toptab.png" for the wide tab pill), then 3) the plain one; both re-canvassed into the
            //    original art's bounding box.
            string variant = VariantOf(like);
            if (variant != null)
            {
                var v = CustomFile(FileNameFor(label) + "." + variant, like);
                if (v != null) return v;
            }
            return CustomFile(FileNameFor(label), like);
        }

        private static readonly HashSet<string> _loggedExactMiss = new HashSet<string>();

        /// <summary>Atlas clones and instantiated sprites get suffixes like "(Clone)"; strip them.</summary>
        private static string CleanSpriteName(string name)
        {
            if (string.IsNullOrEmpty(name)) return name;
            int i = name.IndexOf('(');
            if (i > 0) name = name.Substring(0, i);
            return name.Trim();
        }

        private static string VariantOf(Sprite like)
        {
            if (like == null) return null;
            string n = like.name;
            if (n.IndexOf("actioncom", StringComparison.OrdinalIgnoreCase) >= 0) return "actioncom";
            if (n.IndexOf("toptab", StringComparison.OrdinalIgnoreCase) >= 0) return "toptab";
            return null;
        }

        private static Sprite CustomFile(string file, Sprite like, bool exact = false)
        {
            string key = file + "|" + (like != null ? like.rect.width + "x" + like.rect.height + "@" + like.pixelsPerUnit : "") + (exact ? "|exact" : "");
            if (_custom.TryGetValue(key, out var sp) && sp != null) return sp;
            if (_customMissing.Contains(file)) return null;
            string path = System.IO.Path.Combine(CustomDir, file + ".png");
            if (!System.IO.File.Exists(path)) { _customMissing.Add(file); return null; }
            try
            {
                var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
                if (!ImageConversion.LoadImage(tex, System.IO.File.ReadAllBytes(path))) { _customMissing.Add(file); return null; }
                float ppu = 100f;
                if (like != null && exact)
                {
                    // Same canvas as the original: identical world size, no repositioning.
                    ppu = like.pixelsPerUnit * (tex.width / like.rect.width);
                }
                else if (like != null)
                {
                    // UI slots stretch the sprite to their own rect, so the custom image must have the
                    // original sprite's aspect, and its art must sit exactly where the original art sat
                    // (which is often off-centre). Re-canvas: scale the custom art to fit the original art's
                    // bounding box and place it there.
                    var px = tex.GetPixels32();
                    int aw = tex.width, ah = tex.height, minX = aw, maxX = -1, minY = ah, maxY = -1;
                    for (int y = 0; y < ah; y++) for (int x = 0; x < aw; x++)
                    {
                        if (px[y * aw + x].a > 40) { if (x < minX) minX = x; if (x > maxX) maxX = x; if (y < minY) minY = y; if (y > maxY) maxY = y; }
                    }
                    if (maxX >= minX)
                    {
                        var bounds = KeycapSprite.ArtBoundsOf(like);           // original px
                        float artW = maxX - minX + 1, artH = maxY - minY + 1;   // custom px
                        // s = original px per custom px, chosen so the custom art fits the original box
                        float s = Mathf.Min(bounds.width / artW, bounds.height / artH);
                        int cw = Mathf.Max(1, Mathf.RoundToInt(like.rect.width / s));
                        int ch = Mathf.Max(1, Mathf.RoundToInt(like.rect.height / s));
                        // target centre of the art, in custom px
                        float tcx = (bounds.x + bounds.width / 2f) / s;
                        float tcy = (bounds.y + bounds.height / 2f) / s;
                        int ox = Mathf.RoundToInt(tcx - artW / 2f) - minX;
                        int oy = Mathf.RoundToInt(tcy - artH / 2f) - minY;
                        var canvas = new Color32[cw * ch];
                        for (int y = 0; y < ah; y++) for (int x = 0; x < aw; x++)
                        {
                            int dx = x + ox, dy = y + oy;
                            if (dx < 0 || dy < 0 || dx >= cw || dy >= ch) continue;
                            canvas[dy * cw + dx] = px[y * aw + x];
                        }
                        var padded = new Texture2D(cw, ch, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
                        padded.SetPixels32(canvas);
                        padded.Apply();
                        UnityEngine.Object.Destroy(tex);
                        tex = padded;
                        ppu = like.pixelsPerUnit / s;
                    }
                    else
                    {
                        ppu = like.pixelsPerUnit * Mathf.Max(tex.width / like.rect.width, tex.height / like.rect.height);
                    }
                }
                sp = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), ppu);
                sp.name = "woa_kbm_custom_" + file;
                _custom[key] = sp;
                return sp;
            }
            catch (Exception e)
            {
                Plugin.Log.LogWarning("Failed to load custom key image " + path + ": " + e.Message);
                _customMissing.Add(file);
                return null;
            }
        }

        private static string FileNameFor(string label)
        {
            switch (label)
            {
                case "↑": return "up";
                case "↓": return "down";
                case "←": return "left";
                case "→": return "right";
                case "↑↓": return "updown";
                case "←→": return "leftright";
                case "↑↓←→": return "arrows";
                case "Wheel↑": return "WheelUp";
                case "Wheel↓": return "WheelDown";
            }
            foreach (var c in System.IO.Path.GetInvalidFileNameChars()) label = label.Replace(c, '_');
            return label;
        }
    }

    /// <summary>Procedurally drawn key-cap sprites, one per glyph size so layouts are unchanged.</summary>
    internal static class KeycapSprite
    {
        private static readonly Dictionary<string, Sprite> Cache = new Dictionary<string, Sprite>();

        /// <summary>Anything the mod produced (drawn caps, loaded key images): never treat as a game glyph.</summary>
        public static bool IsKeycap(Sprite s)
        {
            return s != null && s.name.StartsWith("woa_kbm_", StringComparison.Ordinal);
        }

        private static readonly Dictionary<int, Rect> ArtBounds = new Dictionary<int, Rect>();

        /// <summary>
        /// Pixel bounds (sprite-local, bottom-left origin) of the glyph art inside the sprite. Glyph sprites
        /// carry transparent margins and the art is often off-centre (stick bases, tab pills), so replacements
        /// must land in exactly this box.
        /// </summary>
        public static Rect ArtBoundsOf(Sprite s)
        {
            if (s == null) return new Rect(0, 0, 1, 1);
            int id = s.GetInstanceID();
            if (ArtBounds.TryGetValue(id, out var r)) return r;
            float sw = s.rect.width, sh = s.rect.height;
            r = new Rect(sw * 0.125f, sh * 0.125f, sw * 0.75f, sh * 0.75f); // fallback if unreadable
            try
            {
                var copy = GlyphAssets.ReadPixels(s);
                int w = copy.width, h = copy.height;
                var px = copy.GetPixels32();
                UnityEngine.Object.Destroy(copy);
                int minX = w, maxX = -1, minY = h, maxY = -1;
                for (int y = 0; y < h; y++)
                {
                    for (int x = 0; x < w; x++)
                    {
                        if (px[y * w + x].a > 40)
                        {
                            if (x < minX) minX = x;
                            if (x > maxX) maxX = x;
                            if (y < minY) minY = y;
                            if (y > maxY) maxY = y;
                        }
                    }
                }
                if (maxX >= minX && maxY >= minY)
                {
                    r = new Rect(minX, minY, maxX - minX + 1, maxY - minY + 1);
                }
            }
            catch (Exception e)
            {
                Plugin.Log.LogDebug("Glyph bounds read failed for " + s.name + ": " + e.Message);
            }
            ArtBounds[id] = r;
            return r;
        }

        /// <summary>Fraction (0..1) of the sprite's width/height that the glyph art covers.</summary>
        public static Vector2 ArtFractionOf(Sprite s)
        {
            if (s == null) return Vector2.one;
            var r = ArtBoundsOf(s);
            return new Vector2(Mathf.Clamp01(r.width / s.rect.width), Mathf.Clamp01(r.height / s.rect.height));
        }

        /// <summary>
        /// A key cap sprite with the same pixel size and pixels-per-unit as <paramref name="like"/> (so UI
        /// layout is unchanged), with the cap drawn only over the area the original glyph art covered.
        /// </summary>
        public static Sprite Like(Sprite like, bool round)
        {
            int w = like != null ? Mathf.Max(8, Mathf.RoundToInt(like.rect.width)) : 64;
            int h = like != null ? Mathf.Max(8, Mathf.RoundToInt(like.rect.height)) : 64;
            float ppu = like != null ? like.pixelsPerUnit : 100f;
            var frac = ArtFractionOf(like);
            // Caps look best square-ish; use the larger extent for both axes but never exceed the sprite.
            float ext = Mathf.Max(frac.x, frac.y);
            int cw = Mathf.Clamp(Mathf.RoundToInt(w * ext), 8, w);
            int ch = Mathf.Clamp(Mathf.RoundToInt(h * ext), 8, h);
            string key = w + "x" + h + "@" + ppu + ":" + cw + "x" + ch + (round ? ":o" : ":r");
            if (Cache.TryGetValue(key, out var sp) && sp != null) return sp;

            // Single letters get a ring like the game's own glyphs; longer labels get a pill.
            float radius = round ? Mathf.Min(cw, ch) * 0.5f : Mathf.Min(cw, ch) * 0.2f;
            float border = Mathf.Max(1.5f, Mathf.Min(cw, ch) * 0.06f);
            int ox = (w - cw) / 2, oy = (h - ch) / 2;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
            var fill = new Color(0.13f, 0.11f, 0.09f, 0.92f);
            var edge = new Color(0.97f, 0.91f, 0.72f, 1f);
            var px = new Color[w * h];
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float d = RoundRectDistance(x - ox + 0.5f, y - oy + 0.5f, cw, ch, radius);
                    Color c;
                    if (d > 0.5f) c = Color.clear;
                    else if (d > -border) c = edge;
                    else c = fill;
                    if (d > -0.5f && d <= 0.5f) c.a *= 0.5f - d + 0.5f;
                    px[y * w + x] = c;
                }
            }
            tex.SetPixels(px);
            tex.Apply();
            sp = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), ppu);
            sp.name = "woa_kbm_keycap_" + key;
            Cache[key] = sp;
            return sp;
        }

        /// <summary>Fraction of the sprite the cap occupies (for positioning the label).</summary>
        public static Vector2 CapFraction(Sprite like)
        {
            var frac = ArtFractionOf(like);
            float ext = Mathf.Max(frac.x, frac.y);
            return new Vector2(Mathf.Clamp01(ext), Mathf.Clamp01(ext));
        }

        private static float RoundRectDistance(float x, float y, float w, float h, float r)
        {
            float cx = Mathf.Abs(x - w / 2f) - (w / 2f - r);
            float cy = Mathf.Abs(y - h / 2f) - (h / 2f - r);
            float outside = new Vector2(Mathf.Max(cx, 0f), Mathf.Max(cy, 0f)).magnitude;
            float inside = Mathf.Min(Mathf.Max(cx, cy), 0f);
            return outside + inside - r;
        }
    }

    /// <summary>
    /// Attached to any UI Image that shows a controller glyph; swaps it between the glyph and a key cap
    /// depending on the last-used device.
    /// </summary>
    internal class PromptOverlay : MonoBehaviour
    {
        private static readonly List<PromptOverlay> All = new List<PromptOverlay>();
        private static bool _hooked;
        internal static bool Applying;

        private Image _image;
        private Sprite _glyph;
        private string _control;
        private readonly List<TMP_Text> _labels = new List<TMP_Text>();
        private static readonly HashSet<string> _diagLogged = new HashSet<string>();

        /// <summary>Called whenever an Image gets a sprite or is enabled.</summary>
        public static void Consider(Image image)
        {
            if (image == null || Applying) return;
            var sprite = image.sprite;
            if (sprite == null || KeycapSprite.IsKeycap(sprite)) return;
            string control = KeyLabels.ControlForSpriteName(sprite.name);
            if (control != null)
            {
                GlyphAssets.RememberTexture(sprite, image.mainTexture);
                GlyphAssets.DumpIfWanted(sprite);
                GlyphAssets.DumpAllOnce();
            }
            var ov = image.GetComponent<PromptOverlay>();
            if (control == null)
            {
                // Image now shows something that isn't a glyph; stop managing it.
                if (ov != null) ov.Detach();
                return;
            }
            if (!_hooked)
            {
                DeviceTracker.Changed += _ => RefreshAll();
                _hooked = true;
            }
            if (ov == null)
            {
                ov = image.gameObject.AddComponent<PromptOverlay>();
                ov._image = image;
                All.Add(ov);
            }
            ov._glyph = sprite;
            ov._control = control;
            ov.Apply(DeviceTracker.UsingKbm);
        }

        /// <summary>Put every managed Image back to its original glyph (used at shutdown).</summary>
        public static void RestoreAll()
        {
            All.RemoveAll(o => o == null);
            foreach (var o in All)
            {
                try { o.Apply(false); } catch { }
            }
        }

        private static void RefreshAll()
        {
            All.RemoveAll(o => o == null);
            foreach (var o in All)
            {
                try { o.Apply(DeviceTracker.UsingKbm); }
                catch (Exception e) { Plugin.Log.LogWarning("Prompt refresh failed: " + e.Message); }
            }
        }

        private void OnEnable()
        {
            if (_image != null && _control != null) Apply(DeviceTracker.UsingKbm);
        }

        private void OnDestroy()
        {
            All.Remove(this);
        }

        private void Detach()
        {
            HideLabels(0);
            _control = null;
            _glyph = null;
        }

        private void HideLabels(int fromIndex)
        {
            for (int i = fromIndex; i < _labels.Count; i++)
            {
                if (_labels[i] != null) _labels[i].gameObject.SetActive(false);
            }
        }

        private void Apply(bool kbm)
        {
            if (_image == null || _control == null) return;
            Applying = true;
            try
            {
                if (kbm && Plugin.Cfg.KbmPrompts.Value)
                {
                    string text = KeyLabels.ForControl(_control);
                    var custom = GlyphAssets.Custom(text, _glyph);
                    if (custom != null)
                    {
                        _image.sprite = custom;
                        // Blank shape + sidecar: render one label per box with the current bindings,
                        // so remapping keys updates every prompt. No sidecar = image carries its own label.
                        var layout = GlyphAssets.LayoutFor(_glyph);
                        if (_diagLogged.Add(_glyph.name))
                        {
                            Plugin.Log.LogInfo("Overlay " + _glyph.name + ": layout=" + (layout == null ? "null" : (layout.labels == null ? "nolabels" : layout.labels.Length.ToString()))
                                + " canvas=" + (layout?.canvas == null ? "null" : string.Join("x", layout.canvas)) + " imageRect=" + _image.rectTransform.rect.size);
                        }
                        if (layout != null && layout.labels != null && layout.canvas != null && layout.canvas.Length == 2)
                        {
                            float W = layout.canvas[0], H = layout.canvas[1];
                            var rect = _image.rectTransform.rect;
                            for (int i = 0; i < layout.labels.Length; i++)
                            {
                                var box = layout.labels[i];
                                var label = EnsureLabel(i);
                                var rt = (RectTransform)label.transform;
                                rt.anchorMin = new Vector2(box.x / W, 1f - (box.y + box.h) / H);
                                rt.anchorMax = new Vector2((box.x + box.w) / W, 1f - box.y / H);
                                rt.offsetMin = Vector2.zero;
                                rt.offsetMax = Vector2.zero;
                                FitLabel(label, KeyLabels.ForControl(box.control), box.w * rect.width / W, box.h * rect.height / H);
                                label.gameObject.SetActive(true);
                            }
                            HideLabels(layout.labels.Length);
                        }
                        else
                        {
                            HideLabels(0);
                        }
                        return;
                    }
                    _image.sprite = KeycapSprite.Like(_glyph, text.Length <= 1);
                    var single = EnsureLabel(0);
                    // Centre the label on the drawn cap, with a little inset.
                    var cf = KeycapSprite.CapFraction(_glyph) * 0.8f;
                    var srt = (RectTransform)single.transform;
                    srt.anchorMin = new Vector2(0.5f - cf.x / 2f, 0.5f - cf.y / 2f);
                    srt.anchorMax = new Vector2(0.5f + cf.x / 2f, 0.5f + cf.y / 2f);
                    srt.offsetMin = Vector2.zero;
                    srt.offsetMax = Vector2.zero;
                    var r = _image.rectTransform.rect;
                    FitLabel(single, text, cf.x * r.width, cf.y * r.height);
                    single.gameObject.SetActive(true);
                    HideLabels(1);
                }
                else
                {
                    if (_glyph != null) _image.sprite = _glyph;
                    HideLabels(0);
                }
            }
            finally
            {
                Applying = false;
            }
        }

        /// <summary>
        /// Size a label to its box explicitly. TMP's auto-size won't shrink to width with wrapping off, and with
        /// wrapping on it breaks words, so measure once at a reference size and scale.
        /// </summary>
        private static void FitLabel(TMP_Text label, string text, float boxW, float boxH)
        {
            label.enableAutoSizing = false;
            label.text = text;
            if (boxW <= 0f || boxH <= 0f) { label.fontSize = 1f; return; }
            const float refSize = 100f;
            label.fontSize = refSize;
            var pv = label.GetPreferredValues(text);
            if (pv.x <= 0f || pv.y <= 0f) { label.fontSize = boxH; return; }
            // The measured height is a full line (ascender to descender); the visible capitals are ~60% of it.
            // Matching the line box to the label box gives letters at ~60% of the box, like the game's own glyphs.
            float byHeight = refSize * boxH / pv.y;
            float byWidth = refSize * (boxW * 0.95f) / pv.x;
            label.fontSize = Mathf.Max(1f, Mathf.Min(byHeight, byWidth));
        }

        private TMP_Text EnsureLabel(int index)
        {
            while (_labels.Count <= index) _labels.Add(null);
            if (_labels[index] != null) return _labels[index];
            var go = new GameObject("WoaKbmKeyLabel" + index, typeof(RectTransform));
            go.transform.SetParent(_image.transform, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            var t = go.AddComponent<TextMeshProUGUI>();
            TMP_FontAsset font = null;
            try { font = MessageManager.Instance != null ? MessageManager.Instance.GetFontAsset() : null; } catch { }
            if (font == null)
            {
                var sibling = _image.GetComponentInParent<TMP_Text>();
                if (sibling != null) font = sibling.font;
            }
            if (font != null) t.font = font;
            if (index == 0 && _diagLogged.Add("font:" + (font != null ? font.name : "null")))
            {
                Plugin.Log.LogInfo("Label font: " + (font != null ? font.name : "null (TMP default)"));
            }
            t.alignment = TextAlignmentOptions.Center;
            t.enableAutoSizing = false;
            t.fontStyle = FontStyles.Normal; // the game's font asset is already the bold cut
            t.color = new Color32(239, 231, 181, 255); // the game's glyph cream
            t.raycastTarget = false;
            t.textWrappingMode = TextWrappingModes.NoWrap;
#pragma warning disable CS0618
            t.enableWordWrapping = false;
#pragma warning restore CS0618
            t.overflowMode = TextOverflowModes.Overflow;
            t.characterSpacing = -2f;
            t.margin = Vector4.zero;
            _labels[index] = t;
            return t;
        }
    }

    /// <summary>
    /// Rewrites controller glyph characters embedded in text (circled letters such as Ⓐ Ⓑ Ⓧ Ⓨ, or
    /// private-use icon glyphs from the game font) into key labels.
    /// </summary>
    internal static class GlyphText
    {
        private static readonly Dictionary<char, string> Map = new Dictionary<char, string>
        {
            { 'Ⓐ', "buttonSouth" }, { 'Ⓑ', "buttonEast" }, { 'Ⓧ', "buttonWest" }, { 'Ⓨ', "buttonNorth" },
            { 'Ⓛ', "leftShoulder" }, { 'Ⓡ', "rightShoulder" },
            { 'ⓐ', "buttonSouth" }, { 'ⓑ', "buttonEast" }, { 'ⓧ', "buttonWest" }, { 'ⓨ', "buttonNorth" },
            { 'ⓛ', "leftShoulder" }, { 'ⓡ', "rightShoulder" },
            { '⊕', "start" }, { '⊖', "select" },
        };
        private static readonly HashSet<char> _loggedUnknown = new HashSet<char>();
        private static Dictionary<char, string> _userMap;

        private static Dictionary<char, string> UserMap()
        {
            if (_userMap != null) return _userMap;
            _userMap = new Dictionary<char, string>();
            // "E000=buttonSouth;E001=buttonEast"
            foreach (var item in KbmInjector.Split(Plugin.Cfg.GlyphCharMap.Value))
            {
                var kv = item.Split('=');
                if (kv.Length == 2 && int.TryParse(kv[0].Trim(), System.Globalization.NumberStyles.HexNumber, null, out int code))
                {
                    _userMap[(char)code] = kv[1].Trim();
                }
            }
            return _userMap;
        }

        public static bool NeedsRewrite(string s)
        {
            if (string.IsNullOrEmpty(s)) return false;
            foreach (char c in s)
            {
                if (IsGlyphCandidate(c)) return true;
            }
            return false;
        }

        private static bool IsGlyphCandidate(char c)
        {
            return (c >= 'Ⓐ' && c <= 'ⓩ') || c == '⊕' || c == '⊖' || (c >= '' && c <= '');
        }

        public static string Rewrite(string s)
        {
            var sb = new System.Text.StringBuilder(s.Length + 16);
            var user = UserMap();
            foreach (char c in s)
            {
                string control = null;
                if (user.TryGetValue(c, out var uc)) control = uc;
                else if (Map.TryGetValue(c, out var mc)) control = mc;

                if (control != null)
                {
                    sb.Append(string.Format(Plugin.Cfg.TextPromptFormat.Value, KeyLabels.ForControl(control)));
                }
                else
                {
                    if (IsGlyphCandidate(c) && _loggedUnknown.Add(c))
                    {
                        Plugin.Log.LogInfo(string.Format("Unmapped glyph character U+{0:X4} in text: \"{1}\" (add to Prompts.GlyphCharMap)", (int)c, Trim(s)));
                    }
                    sb.Append(c);
                }
            }
            return sb.ToString();
        }

        private static string Trim(string s)
        {
            s = s.Replace("\n", " ");
            return s.Length > 60 ? s.Substring(0, 60) + "..." : s;
        }
    }

    internal static class PromptPatches
    {
        private static bool _inTextSetter;

        // All game text is TextMeshPro. Rewrite embedded glyph characters on assignment.
        [HarmonyPatch(typeof(TMP_Text), nameof(TMP_Text.text), MethodType.Setter)]
        [HarmonyPrefix]
        private static void TMP_Text_set_text_Prefix(ref string value)
        {
            try
            {
                if (_inTextSetter || !DeviceTracker.UsingKbm || Plugin.Cfg == null || !Plugin.Cfg.KbmPrompts.Value) return;
                if (GlyphText.NeedsRewrite(value))
                {
                    _inTextSetter = true;
                    value = GlyphText.Rewrite(value);
                }
            }
            catch (Exception e)
            {
                Plugin.Log.LogWarning("Text glyph rewrite failed: " + e.Message);
            }
            finally
            {
                _inTextSetter = false;
            }
        }

        // Inline text prompts ("Press {A} to ...").
        [HarmonyPatch(typeof(MessageManager), nameof(MessageManager.GetKeyAction))]
        [HarmonyPostfix]
        private static void MessageManager_GetKeyAction_Postfix(int idx, ref string __result)
        {
            try
            {
                if (!Plugin.Cfg.KbmPrompts.Value || !DeviceTracker.UsingKbm) return;
                string control = KeyLabels.ControlForMsgKeyAction(idx);
                if (control == null) return;
                __result = string.Format(Plugin.Cfg.TextPromptFormat.Value, KeyLabels.ForControl(control));
            }
            catch (Exception e)
            {
                Plugin.Log.LogWarning("GetKeyAction patch failed: " + e.Message);
            }
        }

        // Every glyph in the game is a UI Image whose sprite is named icon_guide_*; catch them on assignment and enable.
        [HarmonyPatch(typeof(Image), nameof(Image.sprite), MethodType.Setter)]
        [HarmonyPostfix]
        private static void Image_set_sprite_Postfix(Image __instance)
        {
            try { PromptOverlay.Consider(__instance); }
            catch (Exception e) { Plugin.Log.LogWarning("Image.sprite hook failed: " + e.Message); }
        }

        [HarmonyPatch(typeof(Image), "OnEnable")]
        [HarmonyPostfix]
        private static void Image_OnEnable_Postfix(Image __instance)
        {
            try { PromptOverlay.Consider(__instance); }
            catch (Exception e) { Plugin.Log.LogWarning("Image.OnEnable hook failed: " + e.Message); }
        }

        // Belt and braces: the guide bar assigns sprites in Initialize; feed them to the overlay explicitly.
        [HarmonyPatch(typeof(OperateGuideButton), nameof(OperateGuideButton.Initialize))]
        [HarmonyPostfix]
        private static void OperateGuideButton_Initialize_Postfix(List<Image> ____iconImage)
        {
            try { foreach (var img in ____iconImage) PromptOverlay.Consider(img); }
            catch (Exception e) { Plugin.Log.LogWarning("OperateGuideButton hook failed: " + e.Message); }
        }

        [HarmonyPatch(typeof(OperateGuideHoldButton), nameof(OperateGuideHoldButton.Initialize))]
        [HarmonyPostfix]
        private static void OperateGuideHoldButton_Initialize_Postfix(List<Image> ____iconImage)
        {
            try { foreach (var img in ____iconImage) PromptOverlay.Consider(img); }
            catch (Exception e) { Plugin.Log.LogWarning("OperateGuideHoldButton hook failed: " + e.Message); }
        }
    }
}
