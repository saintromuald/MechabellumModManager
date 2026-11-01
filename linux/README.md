# mbmm — Mechabellum mod manager for Linux

A native Linux client for the [Mechabellum mod catalog](../) (`llxlzx/MechabellumMods`),
built as a port of the Windows WPF manager in this repo. It talks to the same
`catalog.json`, enforces the same SHA-256 rules, and deploys into the same
`Mods/` / `Plugins/` / `UserLibs/` slots — but it runs natively against a
Steam/Proton install instead of requiring Wine.

```
./mbmm doctor                 # game, prefix, .NET, MelonLoader status
./mbmm catalog --refresh      # fetch the community catalog
./mbmm list -v                # browse, mark installed/enabled
./mbmm install show-grid      # download + verify + enable + deploy
./mbmm apply                  # sync the active profile into the game
./mbmm launch                 # start through Steam
./mbmm gui                    # GTK4 front-end
```

For replay archives and the offline battle player, see [REPLAYS.md](REPLAYS.md)
and the companion `mb-replay` script:

```
./mb-replay sync
./mb-replay list --region europe --min-elo 2000
./mb-replay play 20261101--1714
```

## Why a port instead of Wine

The upstream manager is `net8.0-windows` **WPF**. .NET Core WPF renders through
`wpfgfx_cor3.dll`, which is not implemented by Wine. Running the published
binary in a clean Wine 11.20 prefix (self-contained, .NET 8, software rendering,
English UI) fails during the first layout pass:

```
Process terminated. Unrecoverable system error.
   at MS.Internal.Shaping.TypefaceMap.MapUnresolvedCharacters(...)
   ...
   at System.Windows.Window.Show()
   at MechabellumModManager.App.OnStartup(...)
```

The headless CLI modes (`--install-melon-loader`, `--seed-install-config`,
`--ensure-redist`, …) **do** work under Wine because they run before any window
is created. `mbmm` reimplements those paths natively instead, so no Wine is
needed for day-to-day use.

## What the client implements

- Steam library discovery (`libraryfolders.vdf`), game validation
  (`Mechabellum.exe` + `GameAssembly.dll`), Proton prefix discovery
  (`compatdata/669330/pfx`).
- Catalog fetch/caching, locale-aware names/summaries, removal-list awareness
  (entries pulled for match fairness are refused).
- `parts` concatenation and `bundle` files, with per-part and final SHA-256 +
  size verification. Downloads are staged and written atomically.
- Libraries, profiles, and a deployment manifest so re-applying removes stale
  files and never deletes files it did not write (unless `--force`).
- MelonLoader provisioning into the game folder, `.NET 6 Desktop Runtime`
  installation into the game's own prefix via Proton, and the
  `version=native,builtin` DLL override.
- Optional GTK4 UI (`mbmm gui`) with checkboxes, search, Apply and Launch.

## MelonLoader under Proton — the recipe that works

Mechabellum is an IL2CPP Unity 2022.3 game with a Windows-only build, so the
Windows MelonLoader is used inside the game's prefix:

1. Install **.NET 6 Desktop Runtime** into `compatdata/669330/pfx`
   (`proton run windowsdesktop-runtime-6.0.x-win-x64.exe /install /quiet /norestart`).
2. Extract `MelonLoader.x64.zip` into the game folder (`version.dll` +
   `MelonLoader/`).
3. Force the native proxy: `HKCU\Software\Wine\DllOverrides\version = native,builtin`
   inside the prefix. Without this, Wine loads its builtin `version.dll` from
   Proton's `lib/wine/x86_64-windows/` and MelonLoader never starts.
4. Launch through Steam as usual.

Verified with MelonLoader v0.7.3, Unity 2022.3.62f3, game 2.0.0.5.2340:

```
[MelonLoader v0.7.3 Open-Beta]
[Show Grid v1.2.0]
1 Mod loaded.
```

No launch options are needed; the override lives in the prefix registry, so
Steam launches work from the normal Play button.

## Notes and limits

- Mods that change match fairness get removed from the catalog upstream; this
  client refuses entries whose hashes appear in `removedSigned`.
- Only the official server branch is targeted. `melonloader remove` restores the
  stock game (it removes `version.dll` and `MelonLoader/`).
- The GTK UI needs PyGObject, which the system Python on Arch provides. The CLI
  has no third-party dependencies.
- Data lives in `~/.local/share/mbmm/`; configuration in `~/.config/mbmm/`.
