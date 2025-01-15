## MordModFix
![image](https://github.com/user-attachments/assets/83004dea-f691-4d4a-89d1-e4aab160e0d6)

Fixes your Mordhau mod installations by making sure the mod files are present and up to date with the modio.json, reinstalls the mod from mod.io if not.
This program should detect the most common reasons for mods suddenly not working after being updated, saves you the effort of either deleting the .modio directory and having to reinstall \_every_ mod, or manually going through all your mods to find out which ones were not updated correctly.

If MordModFix does not find any errors, yet your mods are still broken, go ahead and delete .modio as usual. So far it has taken care of all of my broken mods just fine, though.


## Usage:

### Windows:
1. Download MordModFix.zip from [releases](https://github.com/10xJSChad/MordModFix/releases)
2. Extract the contents
3. Set your Mordhau game path in game-path.txt
4. Run MordModFix.exe

### Linux:

I built this on Linux, and that's the platform it was primarily tested on. Linux binaries are not provided, but MordModFix will work just fine if built for Linux.
If you are using MordModFix on Linux and would prefer to not have to keep it in the same directory as game-path.txt, you can also put your path in ~/.config/mordmodfix.

## Building:

```dotnet build```
