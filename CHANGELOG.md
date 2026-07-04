# Changelog

## [2.0.0] - 2026-07-04

### Added

- `Tirt.Pathways` namespace to avoid C# type/namespace resolution collisions.
- `SaveType` enum (`Manual`, `QuickSave`, `AutoSave`) to categorize save file paths.
- `SaveProfile` class (renamed from `Pathway`), featuring a simplified semantic API:
    - `GetPath(SaveType type, string customName)`: Get a new path to save to.
    - `GetLatest(SaveType? type)`: Get the path of the most recently modified save file.
    - `GetSaves(SaveType? type)`: Get all existing files.
- `PathwaysDebugWindow` EditorWindow (menu: `Tools > Pathways > Debug Window`) replacing the old `PathwaysManager` custom inspector.
- Built-in defensive code for Unity 6 / modern Unity Fast Play Mode (with Domain Reload disabled).

### Changed

- `PathwaysManager` (MonoBehaviour singleton) converted to a fully static class `Pathways`.
- `PathwaysGlobalConfigs` absorbed into static properties on the `Pathways` class.
- Cache list in `SaveProfile` now auto-refreshes from disk when calling `GetLatest()` or `GetSaves()`.
- Sample code updated to showcase v2.0 static API, including QuickSave and QuickLoad.

### Removed

- `PathwaysGlobalConfigs.cs` and `PathwaysManagerExtensions.cs`.
- `PathwaysManagerEditor.cs` (custom inspector).

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
