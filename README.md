# Little Luxuries

A Dalamud plugin for FFXIV that collects small quality-of-life tweaks designed to make your time in Eorzea a little more comfortable, one small luxury at a time.

## Tweaks

- **Blind Faith** *(coming soon)* - Hides other players inside Leap of Faith courses so the platforms stay clear.
- **Character Select Tweaks** *(coming soon)* - Customise elements on the character select screen.
- **Commend Queue** - Queue your commendation for a player mid-duty and have it awarded automatically when the duty ends.
- **Contact Copy** - Adds a "Copy Name" option to the Contact List right-click menu, copying a player's name (optionally with home world) to your clipboard.
- **Deterministic Posing** - Extends `/cpose`, `/sit`, `/groundsit` and `/doze` to accept an index, jumping directly to a specific pose.
- **Estate Key** - Lock or unlock your estate's guest access and toggle teleport permission from chat, without opening the housing menus.
- **Hide Housing Arrows** - Hides the directional arrows that appear in housing areas.
- **Party Finder Cleanup** *(coming soon)* - Removes duplicate listings from the Party Finder's Other tab.
- **Personal Estate Labels** - Give your estates, shared estates and apartment custom names in the Teleport menu, optionally with ward and plot shown alongside, and reorder them to taste.
- **Quick Commands** - Chat shortcuts for common interactions: target the nearest object, accept a duty, enter a house or apartment, and use an inventory item by name.

## Installation

1. Open XIVLauncher settings
2. Navigate to **Dalamud** → **Custom Plugin Repositories**
3. Add the following URL:
```
https://raw.githubusercontent.com/devoreofox/LittleLuxuries/main/repo.json
```
4. Open `/xlplugins` in-game and search for **Little Luxuries**

## Usage

Type `/llux` in-game to open the tweak manager. Select a tweak from the left panel to view its description and configuration options.

### Commands

- `/llux` - Open the tweak manager
- `/llux changelog` - Open the changelog window
- `/lock [target]` - Lock your estate's guest access (Estate Key)
- `/unlock [target]` - Unlock your estate's guest access (Estate Key)
- `/estatetp on|off [target]` - Toggle estate teleport permission (Estate Key)
- `/targetnearest` - Target the nearest interactable object (Quick Commands)
- `/acceptduty` - Press Commence on the Duty Ready popup (Quick Commands)
- `/enterhouse [room]` - Enter the nearest house or apartment, optionally a specific apartment room number (Quick Commands)
- `/itemaction <item name>` - Use an inventory item by its name (Quick Commands)

`target` is optional for the Estate Key commands - `personal`, `apartment`, `chambers`, or `fc`. Leave it blank to use your first owned estate. These commands work only while on your home world and outside instanced content.

## Contributing

This plugin is a personal project. If you have a tweak idea, feel free to open an issue!

## License

Little Luxuries is licensed under [AGPL-3.0](LICENSE).
