# Changelog

## v0.0.0.4 - 2026-06-14

### Added
- Deterministic Posing - extends `/cpose` with an index so you can jump straight to a pose (`/cpose 3`) instead of cycling one at a time
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

### Added
- Hide Housing Arrows - hides the selection arrows that appear in housing areas, toggleable via the tweak manager
- Two-panel tweak manager UI via `/llux` - selector on the left, description and configuration on the right
- Filter bar for quickly finding tweaks by name
- `/llux hide` and `/llux show` commands for toggling Hide Housing Arrows from chat
- Personal Estate Labels placeholder - coming soon
- Party Finder Cleanup placeholder - coming soon
- Deterministic Posing placeholder - coming soon
- Character Select Tweaks placeholder - coming soon

### Notes
- Housing arrow hiding is only active while inside a housing zone
