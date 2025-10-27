# Changelog

## [1.1.0] - 2025-10-27

### Added

-   `PathwaysManager.GetOrCreateRecentSavePath()` method to retrieve or create a recent save path.

### Changed

-   `PathwaysManager` now auto creates itself in the scene if not found when accessing static instance.

## [1.0.1] - 2025-07-19

### Added

-   `ToggleAutoSave(bool)` overloaded method to `PathwaysManager`.

## [1.0.0] - 2025-07-18

### Changed

-   Moved `AutoSaveSlots` from individual `Pathways` to `PathwaysGlobalConfigs` (still settable from `PathwaysManager`).
-   `PathwaysManager` will automatically refresh expected variables when deletion methods (`DeleteCurrentPathway()`, `DeleteFile(string)`) are called.
-   Improved `PathwaysManagerEditor` inspector UI.
-   Updated sample scene to display usage instructions.
-   Updated documentation and samples to reflect new API and recommended usage.

## [0.1.2] - 2025-07-16

### Changed

-   Tweaked `PathwaysManagerEditor` displaying of the auto-save status.

### Removed

-   `Pathways.Samples` namespace.
