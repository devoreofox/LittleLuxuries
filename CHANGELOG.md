# Changelog

## v1.2.0.0 - 2026-10-01

### Improved
- Personal Estate Labels:
  - Bookmarks: save anyone's house or apartment, on any world, with your own label; they're listed at the end of Residential Areas in the Teleport menu
  - Click a bookmark to travel there: with Lifestream installed it takes you all the way to the plot; without it, you teleport to that district's city, or to your choice of Limsa Lominsa, Ul'dah or Gridania when the bookmark is on another world so you can world visit
  - Bookmark rows show the world, district, ward and plot (or apartment room), plus the gil cost of the first teleport
  - Reorder bookmarks, and see at a glance whether Lifestream is connected
  - Tidier settings: estates and bookmarks are laid out in tables, and worlds are picked from a list grouped by region and data center
  - Bookmarks show up even without an estate of your own, and while you're visiting another world
  - While visiting another world, your own estates stay in the Teleport menu too; clicking one takes you home to it, the same way a bookmark does

## v1.1.0.0 - 2026-09-25

### New Tweaks
- Commend Queue:
  - Right-click a party member during a duty and choose "Commend at Duty End" to queue your commendation
  - Right-click again to cancel, or pick someone else to swap your choice at any time
  - The queued player is commended automatically when the duty ends
  - Yourself and anyone already in your party when you queued are skipped, so they aren't offered
- Quick Commands:
  - `/targetnearest` - target the nearest interactable object (housing door, entrance, aetheryte, and the like)
  - `/acceptduty` - press Commence on the Duty Ready popup
  - `/enterhouse` - interact with the nearest house or apartment entrance and confirm entry for you; add a room number (`/enterhouse 12`) to enter a specific apartment
  - `/itemaction <item name>` - use an inventory item by its name (potions, food, prisms etc)
- Personal Estate Labels:
  - Give your personal estate, shared estates and apartment custom names in the Teleport menu's Residential Areas
  - Labelled rows show the district alongside your label, e.g. "The Lavender Beds - Home"
  - Reorder your residential rows so your favourite place sits at the top
  - Option to show the ward and plot (or apartment room) next to the district, e.g. "The Lavender Beds (W13 P18)"
  - Option to show the label before the location instead of after
  - Labels are kept per character, and each character's estates are picked up the first time you open Teleport on them
  - "Forget" removes an estate you've moved out of, along with its label

### Improved
- Deterministic Posing:
  - `/sit`, `/groundsit` and `/doze` now accept a pose index too, e.g. `/groundsit 3` sits you straight into your third ground pose
  - Already in that pose type? The index just switches your pose instead of standing you back up
  - Plain `/sit`, `/groundsit` and `/doze` work exactly as before

### Fixed
- Stopped Little Luxuries from insisting on holding hands with Silkstring if both plugins were installed

### Notes
- All three tweaks are off by default - enable them in the tweak window
- Commend Queue can't be toggled while you're in a duty; leave the duty to change it

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
