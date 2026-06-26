# Changelog

## v1.0.0.0 - 2026-06-26

### New Tweaks
- Estate Key:
  - `/lock` and `/unlock` toggle your estate's guest access without opening the housing menus
  - `/estatetp on|off` controls teleport permission on its own
  - Optionally target a specific property (`personal`, `apartment`, `chambers`, `fc`), or leave it blank to use your first owned estate
  - Locking or unlocking preserves your current teleport setting, so it only changes the access you asked for

### Added
- Changelog viewer - after an update, a window shows what's new; reopen any time via `/llux changelog` or the scroll icon in the tweak window's title bar
- "New!" badge marks tweaks you haven't opened yet, clearing once you select them

### Notes
- Estate Key is off by default - enable it in the tweak window
- Estate Key only works while you're on your home world and outside instanced content (dungeons, raids, and the like)
- Stray kittens can now be kept at a respectful distance ;3

## v0.0.0.6 - 2026-06-23

### New Tweaks
- Contact Copy:
  - Adds a "Copy Name" option to the right-click menu in the Contact List (your recent players), copying a player's name to the clipboard
  - Option to include the home world, so you can copy `Name@World` or just the name on its own

### Notes
- Off by default - enable it in the tweak window

## v0.0.0.5 - 2026-06-19

### Fixed
- `/cpose <index>` now works immediately after sitting, lying down, or sitting on the ground. Previously the pose wasn't recognized until you pressed cpose once
- Regular emotes (hum, dances, and the like) are no longer mistaken for an idle pose

## v0.0.0.4 - 2026-06-14

### New Tweaks
- Deterministic Posing:
  - Extends `/cpose` with an index so you can jump straight to a pose (`/cpose 3`) instead of cycling one at a time
  - `/cpose list` shows the poses available in your current stance; `/cpose help` shows usage
  - Options to set the cycle speed and to number poses from 1 instead of 0

### Notes
- Off by default - enable it in the tweak window
- Plain `/cpose` still cycles exactly as before; the index only applies while standing, sitting, sitting on the ground, or dozing

## v0.0.0.3 - 2026-06-13

### Added
- Manage Arrows window - whitelist individual furnishings so their arrows stay visible while the rest are hidden; saved per house and remembered across sessions and game restarts
- Each placed furnishing is tracked on its own, so whitelisting one of several identical pieces (say, one of two Summoning Bells) only affects that one
- Prevent Interaction - stops hidden furnishings from being clickable so you can't accidentally target them
- Always Display - keep arrows visible for entire furnishing types (Summoning Bell, Orchestrion, and the like), no matter which house you're in
- Highlight - tints a furnishing's arrow pink from the Manage Arrows window so you can spot which piece an entry points to
- Search bar in the Manage Arrows window for filtering furnishings by name
- Whitelisting is limited to houses you own or have permission to edit

### Fixed
- Cleaned up an event handler that lingered when the plugin was reloaded

### Notes
- Per-furnishing whitelisting works inside houses you own; basic arrow hiding still works in any housing zone
- Whitelist entries for furnishings you move or store are cleared automatically - just re-whitelist them in their new spot

## v0.0.0.2 - 2026-06-05

### Fixed
- Plugin icon now displays correctly after installation (oops lol)

## v0.0.0.1 - 2026-06-05 - Initial Release

### New Tweaks
- Hide Housing Arrows:
  - Hides the selection arrows that appear in housing areas, toggleable via the tweak manager

### Added
- Two-panel tweak manager UI via `/llux` - selector on the left, description and configuration on the right
- Filter bar for quickly finding tweaks by name
- `/llux hide` and `/llux show` commands for toggling Hide Housing Arrows from chat
- Personal Estate Labels placeholder - coming soon
- Party Finder Cleanup placeholder - coming soon
- Deterministic Posing placeholder - coming soon
- Character Select Tweaks placeholder - coming soon

### Notes
- Housing arrow hiding is only active while inside a housing zone
