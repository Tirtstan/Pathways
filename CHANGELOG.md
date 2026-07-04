# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [2.0.0] - 2026-07-04

### 🚀 Highlights

- **Static, scene-free API (`Pathways`)**:
    - **No more `PathwaysManager` GameObject in your scenes!** The old MonoBehaviour singleton is now a fully static facade — call `Pathways.LoadProfile(...)`, `Pathways.GetPath(...)`, and `Pathways.GetLatest()` from anywhere without hunting for an instance.
    - `PathwaysGlobalConfigs` is gone; storage location, file extension, auto-save prefix, quicksave filename, and slot count are all static properties on `Pathways`.
- **Typed save categories (`SaveType`)**:
    - First-class support for **Manual**, **QuickSave**, and **AutoSave** paths — no more inferring save type from filename heuristics.
    - Quick save is a dedicated overwrite slot (`{profile}_quicksave.sav`) instead of reusing manual-save naming.
    - Filter by type when loading or building a save-game menu: `GetLatest(SaveType.Manual)`, `GetSaves(SaveType.QuickSave)`, etc.
- **`SaveProfile` — clearer profile model (renamed from `Pathway`)**:
    - Three focused methods cover most workflows: `GetPath(SaveType, customName)`, `GetLatest(SaveType?)`, and `GetSaves(SaveType?)`.
    - File lists auto-refresh from disk on every query — no stale cache after external file changes.
- **`PathwaysDebugWindow` Editor tooling**:
    - Replaces the old `PathwaysManager` custom inspector with a dedicated window at **Tools → Pathways → Debug Window**.
    - Browse profiles, inspect saves on disk, copy paths, and delete files while iterating in the Editor.
- **Fast Play Mode ready (Unity 6 / Domain Reload disabled)**:
    - Static state resets via `SubsystemRegistration` so play sessions don't leak profile or config data between Enter Play Mode cycles.

### Changed

- `PathwaysManager` (MonoBehaviour singleton) converted to a fully static class `Pathways`.
- Namespace moved from `Pathways` to `Tirt.Pathways` to avoid C# type/namespace resolution collisions.
- `Pathway` renamed to `SaveProfile`; `PathwayId` renamed to `ProfileId`.
- `PathwaysGlobalConfigs` absorbed into static properties on the `Pathways` class.
- Events renamed: `OnCurrentPathwayChanged` → `OnActiveProfileChanged`; `OnAutoSavePathRequested` is now static on `Pathways`.
- Auto-save API simplified: `ToggleAutoSave(...)` → `EnableAutoSave(...)` / `DisableAutoSave()`.
- Unity minimum version updated to **2021.3**.
- Sample scene and `SaveManager.cs` updated for the v2.0 static API, including QuickSave and QuickLoad.
- Sample scene UI text now points to the Debug Window instead of the old manager inspector.

### Added

- `Tirt.Pathways` namespace.
- `SaveType` enum (`Manual`, `QuickSave`, `AutoSave`).
- `SaveProfile` class with:
    - `GetPath(SaveType type, string customName)` — resolve a path to write to.
    - `GetLatest(SaveType? type)` — most recently modified save file path, optionally filtered by type.
    - `GetSaves(SaveType? type)` — all matching files, newest first.
- `PathwaysDebugWindow` EditorWindow (menu: `Tools > Pathways > Debug Window`).
- Built-in defensive reset for Unity Fast Play Mode (Domain Reload disabled).
- Documentation screenshot and refreshed `README.md` for v2.0.

### Removed

- `PathwaysGlobalConfigs.cs` and `PathwaysManagerExtensions.cs`.
- `PathwaysManagerEditor.cs` (custom inspector).
- `PathwaysManager.cs` and `Pathway.cs`.

### Breaking

- All `PathwaysManager` / `Pathway` APIs must migrate to `Pathways` / `SaveProfile`.
- Namespace changed from `Pathways` to `Tirt.Pathways` — update all `using` directives.
- Scene `PathwaysManager` GameObjects are no longer used and can be removed.
- Extension methods on `PathwaysManager` (`GetAllSaveFiles`, `GetManualSaveFiles`, etc.) are replaced by `Pathways.GetSaves(SaveType?)` and `Pathways.GetLatest(SaveType?)`.
- `GetOrCreateRecentSavePath()` removed — use `GetLatest()` to load or `GetPath(SaveType.Manual)` to create a new save path.

### Migration Notes

If upgrading from 1.x:

- Add `using Tirt.Pathways;` and remove references to the old `Pathways` namespace.
- Remove any `PathwaysManager` GameObject from your scenes — it is no longer required.
- Replace `PathwaysManager.Instance` with static `Pathways` calls:

    | 1.x | 2.0 |
    | --- | --- |
    | `PathwaysManager.Instance` | `Pathways` (static) |
    | `CurrentPathway` | `ActiveProfile` |
    | `CreateOrLoadPathway(id)` | `LoadProfile(id)` |
    | `SetCurrentPathway(id)` | `SetActiveProfile(id)` |
    | `SelectRecentPathway()` | `SelectRecentProfile()` |
    | `GetAllPathwayIds()` | `GetAllProfileIds()` |
    | `DeleteCurrentPathway()` | `DeleteProfile(activeProfile.ProfileId)` |
    | `pathway.GetSavePath(name)` | `Pathways.GetPath(SaveType.Manual, name)` |
    | `GetAutoSavePath()` | `Pathways.GetPath(SaveType.AutoSave)` |
    | `GetOrCreateRecentSavePath()` | `Pathways.GetLatest()` or `Pathways.GetPath(SaveType.Manual)` |
    | `GetManualSavePath(name)` | `Pathways.GetPath(SaveType.Manual, name)` |
    | `manager.GetAllSaveFiles()` | `Pathways.GetSaves()` |
    | `manager.GetManualSaveFiles()` | `Pathways.GetSaves(SaveType.Manual)` |
    | `manager.GetAutoSaveFiles()` | `Pathways.GetSaves(SaveType.AutoSave)` |
    | `manager.GetRecentSaveFile()` | `Pathways.GetLatest()` |
    | `ToggleAutoSave(true, slots, interval)` | `Pathways.EnableAutoSave(interval, slots)` |
    | `ToggleAutoSave(false)` | `Pathways.DisableAutoSave()` |
    | `SetTimescale(unscaled)` | `Pathways.UseUnscaledTime = unscaled` |
    | `PathwaysGlobalConfigs.SaveExtension` | `Pathways.SaveExtension` |
    | `OnCurrentPathwayChanged` | `OnActiveProfileChanged` |

- For quicksave (new in 2.0): use `Pathways.GetPath(SaveType.QuickSave)` — there was no dedicated quicksave slot in 1.x.
- Existing save files on disk remain compatible; only your C# call sites need updating.
- Use **Tools → Pathways → Debug Window** instead of selecting a `PathwaysManager` in the Inspector.

## [1.1.0] - 2025-10-27

### Added

- `PathwaysManager.GetOrCreateRecentSavePath()` method to retrieve or create a recent save path.

### Changed

- `PathwaysManager` now auto creates itself in the scene if not found when accessing static instance.

## [1.0.1] - 2025-07-19

### Added

- `ToggleAutoSave(bool)` overloaded method to `PathwaysManager`.

## [1.0.0] - 2025-07-18

### Changed

- Moved `AutoSaveSlots` from individual `Pathways` to `PathwaysGlobalConfigs` (still settable from `PathwaysManager`).
- `PathwaysManager` will automatically refresh expected variables when deletion methods (`DeleteCurrentPathway()`, `DeleteFile(string)`) are called.
- Improved `PathwaysManagerEditor` inspector UI.
- Updated sample scene to display usage instructions.
- Updated documentation and samples to reflect new API and recommended usage.

## [0.1.2] - 2025-07-16

### Changed

- Tweaked `PathwaysManagerEditor` displaying of the auto-save status.

### Removed

- `Pathways.Samples` namespace.
