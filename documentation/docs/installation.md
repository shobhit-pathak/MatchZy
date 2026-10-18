# Installation

* Install [Metamod](https://cs2.poggu.me/metamod/installation/)
* Install [CounterStrikeSharp (CSSharp)](https://docs.cssharp.dev/guides/getting-started/). (**Note**: This step can be skipped if you install [MatchZy with CSSharp release](https://github.com/shobhit-pathak/MatchZy/releases/))
	* Go to this link: https://github.com/roflmuffin/CounterStrikeSharp/releases
	* Scroll down and download 'counterstrikesharp-with-runtime'. MatchZy needs CounterStrikeSharp **v369 or newer** (.NET 10 runtime); it will not load on older versions.
	* Extract the addons folder to the csgo/ directory of the dedicated server. The contents of your addons folder should contain both the counterstrikesharp folder and the metamod folder
	* Verify the installation by typing `meta list` on server console. You should see CounterStrikeSharp plugin by Roflmuffin
	* You can refer to https://docs.cssharp.dev/guides/getting-started for detailed instructions. Initially, it may seem a bit hectic, but trust me, it's worth it! :P 
* Install MatchZy 
	* Download the latest [MatchZy release](https://github.com/shobhit-pathak/MatchZy/releases/) and extract the files to the csgo/ directory of the dedicated server.
	* Verify the installation by typing `css_plugins list` and you should see MatchZy by WD- listed there.
	* The release also contains `addons/counterstrikesharp/gamedata/matchzy.json`, which practice mode needs to rethrow smokes, HE grenades, molotovs and decoys with the native game functions. Keep it up to date when updating MatchZy; after a CS2 update, a new version of this file can fix rethrows without a new plugin build.
	* `cfg/MatchZy/admins.json`, `database.json`, `savednades.json` and `whitelist.cfg` are not part of the release: MatchZy creates them when they are first needed, and extracting an update keeps your admins, database settings, saved lineups and whitelist. The other `.cfg` files in `cfg/MatchZy` are replaced by an update, so don't put your own settings in them: create [override CFGs](configuration.md#override-cfgs) (`<name>_override.cfg`) instead. MatchZy executes them right after the CFG they override, and they are not part of the release either, so updates keep them. If you have edited a CFG that comes with MatchZy, move your changes to its override CFG before updating.

**Note**: If you want to use MatchZy on Windows server, you will need Windows build of `counterstrikesharp` which is available on its [releases page](https://github.com/roflmuffin/CounterStrikeSharp/releases)

## Round backups

!!! warning "Round backups need the `-allow_round_backup_files` launch option"
    Since the CS2 update of October 14, 2026, round backup files require the game server startup flag `-allow_round_backup_files`. If you want round backups and round restores to work, add it to the launch options of your server and restart the server, for example:

    ```
    cs2 -dedicated +map de_dust2 -allow_round_backup_files
    ```

    Without it, CS2 doesn't save round backups, so the backups MatchZy saves (in `csgo/MatchZyDataBackup`, and the ones it uploads to [`matchzy_remote_backup_url`](configuration.md#matchzy_remote_backup_url)) have no round data, and nothing can be restored from them: `.stop`, `.restore`, `matchzy_loadbackup` and `matchzy_loadbackup_url` refuse with "has no complete round data". Matches can still be played, and MatchZy logs `Round file ... is missing or incomplete` after every round.
