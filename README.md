### MordModFix
Fixes your Mordhau mod installations by making sure the mod files are present and up to date with the modio.json, reinstalls the mod from mod.io if not.
This program should detect the most common reasons for mods suddenly not working after being updated, saves you the effort of either deleting the .modio directory and having to reinstall \_every_ mod, or manually going through all your mods to find out which ones were not updated correctly.

If MordModFix does not find any errors, yet your mods are still broken, go ahead and delete .modio as usual. So far it has taken care of all of my broken mods just fine, though.

Usage:
1. Download MordModFix.zip from [releases](https://github.com/10xJSChad/MordModFix/releases)
2. Extract the contents
3. Set your Mordhau game path in game-path.txt
4. Run MordModFix.exe

Building:

```dotnet build```
