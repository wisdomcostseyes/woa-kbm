# Winds of Anthos: Keyboard & Mouse

Keyboard and mouse support, with matching on-screen key prompts, for **Harvest Moon: The Winds of Anthos** (Steam).

Somebody shipped a PC port of a farming game in the year of our lord 2023 and decided that the keyboard, a device
that has been attached to every PC since before the first Harvest Moon existed, was an optional accessory. The Steam
page says so with a straight face: *"requires a controller."* The engine supports keyboards. The game's own input
asset still has the template's WASD bindings sitting in it, unused. Somebody simply wrote a list of allowed devices and
left the keyboard off it. That's the entire reason you couldn't play this with the thing you typed the Steam search
query on.

This mod puts the keyboard back on the list, wires up a sensible layout, and swaps every controller glyph for a key cap
drawn in the game's own style whenever you're using the keyboard, switching back to pad glyphs the moment you touch
a controller. The developers get the credit for the game; the keyboard gets its dignity back.

## Install

1. Install **BepInEx 5 (x86)**: download `BepInEx_win_x86_5.4.23.5.zip` from the
   [BepInEx releases page](https://github.com/BepInEx/BepInEx/releases) and extract it into the game folder
   (`...\steamapps\common\Harvest Moon The Winds of Anthos\`, next to the game's exe). The game is a 32-bit build, so it
   must be the **x86** package.
2. Run the game once so BepInEx creates its folders, then quit.
3. Extract this mod's zip into the same game folder. You should end up with
   `BepInEx\plugins\WoaKbm\WoaKbm.dll` and `BepInEx\plugins\WoaKbm\keys\`.
4. Play. A controller can stay plugged in; both work at once.

To uninstall, delete `BepInEx\plugins\WoaKbm`. To remove BepInEx too, delete `winhttp.dll`, `doorstop_config.ini`
and the `BepInEx` folder.

## Default keys

| Action | Keys |
|---|---|
| Move | W A S D |
| Camera | Mouse |
| Confirm / interact / use tool | **F**, Enter, left click |
| Cancel / back / dismount | **Esc**, right click |
| Item bag | **Tab** (or I) |
| Y prompts (skip, sort, ...) | **R** |
| Previous / next tab, photo mode, sneak | **Q** / **E** |
| Dash | **Shift** (hold) |
| Camera zoom in / out | **Z** / mouse wheel |
| Camera reset / top-down view | **Home** (or middle click) / **V** |
| Menu / options | **Esc** |
| DocPad | **M** |
| Quick actions (d-pad), menu selectors | Arrow keys |
| Walk instead of run | hold **Ctrl** |
| Release the mouse cursor | hold **Alt** |

Everything is remappable in `BepInEx\config\danreed.woa.kbm.cfg` (created on first launch). Each entry lists keys
separated by `;`; a bare name is a keyboard key (`e`, `leftShift`, `upArrow`...), anything in angle brackets is a raw
Unity Input System path such as `<Mouse>/leftButton`. Prompts follow your bindings automatically.

## Known limits

- The tutorial pictures shown inside the DocPad are pre-rendered screenshots and still show controller buttons.
- Menus are navigated with keys, not the mouse pointer. The game has no pointer UI to hook.

## How it works (for modders)

- **Why there's no keyboard support.** The game's Input System settings whitelist only gamepad layouts, so Unity never
  creates keyboard or mouse devices. The mod clears that whitelist at startup.
- **Bindings.** The game names actions after Switch labels (`East` = confirm via `*/{Submit}`, `South` = cancel via
  `*/{Cancel}`) and carries per-layout bindings. `KbmInjector.cs` canonicalises every action on the Xbox layout and adds
  keyboard/mouse bindings beside each gamepad binding; control schemes are removed so pad, keyboard and mouse coexist.
- **Prompts.** Every glyph is a UI `Image` whose sprite is named `ui_common_icon_guide_*`. A hook on `Image.sprite`
  swaps in the matching PNG from `keys\` (exact original name first, then `Label.variant.png`, then `Label.png`, then
  a drawn fallback). Text prompts go through `MessageManager.GetKeyAction` and a `TMP_Text.text` rewrite. See `Prompts.cs`.
- **Startup rule.** Nothing may touch `UnityEngine.InputSystem` or register the prompt patches from the BepInEx
  chainloader: the game hangs at "Using Windows.Gaming.Input". Both happen from the `InputSystemManager.Awake` prefix.
- **Exit crash.** BepInEx 5.4.23 crashes this game on exit even with no plugins (MonoMod issue #329: on newer x86
  Mono, `SetMonoCorlibInternal` writes to the wrong offset and poisons `MonoAssembly.friend_assembly_names`;
  fixed upstream in MonoMod PR #330, not yet shipped in BepInEx 5). `MonoAssemblyRepair` in `Plugin.cs` restores the
  poisoned word at startup and at quit. It's a no-op once BepInEx ships a fixed MonoMod.

## Building

- .NET SDK 8 or newer. References resolve from the game's `Managed` folder and `BepInEx\core` (set `GameDir` in the
  csproj or pass `-p:GameDir=...`).
- `tools\deploy.ps1` builds and copies the DLL plus `assets\keys` into the game (game must be closed).
- `tools\package.ps1` builds `dist\WoaKbm-<version>.zip` for release.
- `tools\make_keycaps.ps1` regenerates the key images from `assets\glyph_map.csv` (needs the game's glyph sprites
  dumped to `reference\glyphs` via the `DumpGlyphs` config option, and the FOT-Seurat Pro B font installed).
  `tools\keycap_groups.ps1` renders a review sheet.

## Layout

| Path | What |
|---|---|
| `src/WoaKbm/` | Plugin source (C#, .NET Standard 2.1, BepInEx 5 + HarmonyX). |
| `assets/keys/` | Key prompt images, one per original glyph sprite name. |
| `assets/glyph_map.csv` | Original glyph → key label mapping that drives the generator. |
| `tools/` | Build, deploy, package and glyph-generation scripts. |

`reference/` (decompiled game code, dumped assets) and `archive/` are local only and git-ignored.

## License

MIT. The key images are original drawings made for this mod; no game assets are redistributed.
