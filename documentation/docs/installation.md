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
	* `cfg/MatchZy/admins.json`, `database.json`, `savednades.json` and `whitelist.cfg` are not part of the release: MatchZy creates them when they are first needed, and extracting an update keeps your admins, database settings, saved lineups and whitelist. The `.cfg` files in `cfg/MatchZy` are replaced by an update, so keep a copy of any you have edited.

**Note**: If you want to use MatchZy on Windows server, you will need Windows build of `counterstrikesharp` which is available on its [releases page](https://github.com/roflmuffin/CounterStrikeSharp/releases)
