# MatchZy Changelog

# 0.7.7

#### April 29, 2024

- Added wingman support. Now, if `game_mode` is 2, plugin will automatically execute `live_wingman.cfg`. If `game_mode` is 1, `live.cfg` will be executed.
- Setting `wingman` as `true` in match config json will now automatically set `game_mode 2` and reload the map. Wingman toggle from G5V will now also work.
- Removed `UpdatePlayersMap` from player connect and disconnect methods to avoid it getting called multiple times on map change.
- Made `SetMatchEndData` to be an async operation.
- Added updated pt-PT translations.

# 0.7.6

#### April 28, 2024

- Added remaining strings available for translation.
- Fixed force-unpause command not working in knife round.
- Fixed `cfg` folder not available in Windows build of MatchZy with CSSharp.

# 0.7.5

#### April 27, 2024

- Upgraded CounterStrikeSharp to v217
- Fixed CFG execution on Map Start (After the latest update, CFGs were getting overriden by gamemodes cfg. Hence, added a timer to delay MatchZy's CFG execution on MapStart)
- Fixed BO2 setup, now Get5 server will be freed once the BO2 match is over

# 0.7.4

#### April 26, 2024

- Upgraded CounterStrikeSharp to v215
- Added Chinese (Simplified) translations.
- Fixed wrong `[EventPlayerConnectFull] KICKING PLAYER` during match setup.

# 0.7.3

#### April 06, 2024

- Added an automated build and release pipeline.
- Now all the players will be put in their team on veto start.

# 0.7.2

#### April 02, 2024

**Coach**

- Fixed rare case of everyone getting the C4 when coach gets the C4.

**Translation**

- Added German translations
- Added Portuguese (Brazil) translations
- Added French translations
- Added Portuguese (Portugal) translations

# 0.7.1

#### March 31, 2024

**Coach**

- Fixed coaches able to spectate other teams for 1 second on round start.
- Fixed coaches taking competitive spawns. Now coaches will be spawned in air (in non-competitive spawn).
- Fixed coaches getting weapons in freezetime and their weapons getting dropped after freezetime ends. Now they will be spawned without any weapons.
- Fixed coaches getting the C4. Now only the players will get the C4.

**Translation**

- Added translation/multi-lingual support in MatchZy. Currently only match related strings are added in the translation. There will be a folder called `lang` in which translation JSONs will be present. Currently we have the translations for English and Russian (thanks to @innuendo-code). To add more languages, create a JSON file with the language locale code (like `en.json` or `fr.json`, etc). Contribution for translations are much appreciated! :D

**Practice Mode/Match Mode**

- Fixed boost command working in match mode.
- Added `.skipveto` command which will skip veto in match setup (thanks to @lanslide-team)
- Added complete support of `get5_status` command which will now return detailed status of the match setup (thanks to @The0mikkel)
- Added `mp_solid_teammates 1` in knife round for solid teammates.
- Disabled overtime when `.playout` command is used.

**Admin**

- Added a convar `matchzy_everyone_is_admin`, if set to `true`, all the players will be granted admin privileges for MatchZy commands. 

**CSSharp**

- Migrated the plugin to .NET8, make sure you use CS# v201 or above.

# 0.7.0

#### Feb 10, 2024

**Practice Mode**

- Added `!rethrow` command which rethrows player-specific last thrown grenade.
- Added `!last` command which teleports the player to the last thrown grenade position
- Added `!timer` command which starts a timer immediately and stops it when you type .timer again, telling you the duration of time
- Added `!back <number>` command which teleports you back to the provided position in your grenade history
- Added `!throwindex <index> <optional index> <optional index>` command which throws grenade of provided position(s) from your grenade thrown history. Example: `!throwindex 1 2` will throw your 1st and 2nd grenade. `!throwindex 4 5 8 9` will throw your 4th, 5th, 8th and 9th grenade (If you've added delay in grenades, they'll be thrown with their specific delay).
- Added `!delay <delay_in_seconds>` command which sets a delay on your last grenade. This is only used when using .rethrow or .throwindex
- Added `!lastindex` command which prints index (position) number of your last thrown grenade.
- Added grenade-specific rethrow commands like `!rethrowsmoke`, `!rethrownade`, `!rethrowflash`, `!rethrowmolotov` and `!rethrowdecoy`
- Grenades fly-time added. For example, on throwing a grenade, you'll get the message in chat: Flash thrown by WD- took 1.62s to detonate

**Match Mode**

- Fixed `.stay` and `.switch` not working in in side-selection phase after CS2's Arms Race update. 
- Added `!forceready` command which force-readies player's team (Currently works only in match setup using JSON/Get5, based on the value of `min_players_to_ready`)
- Improved `get5_status` command, which will now display proper `gamestate` and `matchid` when a match is loaded using JSON/Get5 (Thanks to [@The0mikkel](https://github.com/The0mikkel))

**Coach**

- Coaches will now die immediately after freeze-time ends, solving the problem of coach blocking players for 1-2 seconds.

# 0.6.1-alpha

#### Dec 27, 2023

- Added DryRun mode for Practice Mode. Use `.dryrun` while in practice mode to activate dryrun! Also added `dryrun.cfg` in `cfg/MatchZy/dryrun.cfg` which can be modified as per your requirements
- Added `.noflash` command in Practice Mode which will make the user immune to flashbangs. Use `.noflash` again to disable noflash.
- Added `.break` command in Practice Mode which will break all the breakable entities like glass windows, wooden doors, vents, etc
- Added `matchzy_demo_name_format` which will allow to set demo name as per the requirement. Default: `{TIME}_{MATCH_ID}_{MAP}_{TEAM1}_{TEAM2}` [Read More](https://shobhit-pathak.github.io/MatchZy/configuration/#matchzy_demo_name_format)
- Fixed players able to use `.tac` even after tactical timeouts were exhausted.

# 0.6.0-alpha

#### Dec 14, 2023

- Added support for Get5 Web panel! (G5V and G5API) (Read more at: https://shobhit-pathak.github.io/MatchZy/get5/)
What can Get5 Web Panel + MatchZy can do?

1. Create teams and setup matches from web panel
2. Support for BO1, BO3, BO5, etc with Veto and Knife Round
2. Get veto, scores and player stats live on the panel
3. Get demo uploaded automatically on the panel (which can be downloaded from its match page)
4. Pause and unpause game from the panel
5. Add players in a live game
6. And much more!!!

# 0.5.1-alpha

#### Dec 8, 2023

- Added `.boost`, `.crouchboost`, `.crouchbot` commands in Practice Mode to spawn Bot/Crouched bot and boost on it.
- Added `.ct`, `.t`, and `.spec` command in Practice Mode to switch the player in requested team
- Added `.fas` and `.watchme` command in Practice Mode which forces all players into spectator except the player who called this command
- Added `matchzy_autostart_mode` command for default launch mode of the plugin (0 for neither/sleep mode, 1 for match mode, 2 for practice mode. Default: 1)
- Added `matchzy_save_nades_as_global_enabled` config convar to save nades globally
- Added `matchzy_use_pause_command_for_tactical_pause` config convar to use `!pause` command as tactical pause
- Renamed `.knife` command to `.roundknife` and added `.rk` alias to resolve conflict with `.knife` command of other plugins
- Fixed tactical timeout force-unpausing the match on timeout end
- Fixed `matchzy_minimum_ready_required 0` not working properly on server startup
- Made `spectator` key in match setup config optional field

# 0.5.0-alpha

#### Dec 6, 2023

- Matches can now be setup using JSON file! This includes locking players to their correct team and side, setting the map(s) and configuring the game rules. Added `matchzy_loadmatch <filepath>` and `matchzy_loadmatch_url "<url>"` commands (read more at https://shobhit-pathak.github.io/MatchZy/match_setup/)
- Demos can now be uploaded to a URL once the map and recording ends. Command to setup the upload URL: `matchzy_demo_upload_url "<url>"` (read more at https://shobhit-pathak.github.io/MatchZy/gotv/#automatic-upload)
- Removed map reload on map end to avoid any issues
- Fixed issues while restoring round during halftime
- Fixed lag on round end which was due to pushing stats into the database. Now that operation is async!
- This one is not related to the working of the plugin, but we have a new documentation page! https://shobhit-pathak.github.io/MatchZy/

# 0.4.3-alpha

#### Nov 25, 2023

- A full implementation of CSSharp's admin system!
You can now fine-tune admin permissions as per your requirement
Flag-wise permissions:

  - `@css/root`: Grants access to all admin commands
  - `@css/config`: Grants access to config related admin commands
  - `@custom/prac`: Grants access to practice related admin commands
  - `@css/map`: Grants access to change map and toggle practice mode
  - `@css/rcon`: Grants access to trigger RCON commands using `!rcon <command>`
  - `@css/chat`: Grants access to send admin chat messages using `!asay <message>`

- Added `.forcepause` and `.forceunpause` commands for admins so that they can use `.pause` and `.unpause` as a player while playing (Use `.fp` and `.fup` for shorter commands)
- Added `.playout` commands to toggle Playout! (If playout is enabled, all rounds would be played irrespective of winner. Useful in scrims!). Also added `matchzy_playout_enabled_default` command to enable/disable playout by default. Default: `matchzy_playout_enabled_default false`
-  Added `matchzy_admin_chat_prefix` command to configure admin chat prefix when using `.asay <message>`. Default: `matchzy_admin_chat_prefix [{Red}ADMIN{Default}]`
- Added `.help` command to list all the available commands during that match phase
- Rounded off blind duration in practice mode to 2 decimal places.
- Added damage report for bot in practice mode (for every hit, similar to Get5 practice mode)
- Fixed CSTV bot getting kicked on adding a bot in practice server
- Improvements in handling saved nades (now JSON structure is used to manage saved nades, thanks to @DEAFPS!)
- Removed "Welcome to the server" message on joining the server

# 0.4.2-alpha

#### Nov 21, 2023

- MatchZy now supports CSSharp's admin system!
You can create a new entry in the `/addons/counterstrikesharp/configs/admins.json` file with `@css/generic` generic flag like mentioned in the below example:
```
{
  "WD-": {
    "identity": "76561198154367261",
    "flags": [
      "@css/generic"
    ]
  },
  "Another admin": {
    "identity": "SteamID 2",
    "flags": [
      "@css/generic"
    ]
  }
}
```

To maintain backwards compatibility, we still support creating admins using older method (by adding entries in `csgo/cfg/MatchZy/admins.json`), so you can choose the most convenient method according to your preference.

# 0.4.1-alpha

#### Nov 20, 2023

- Fixed a case where coaches were not swapping after halftime
- Fixed a case where round restore on halftime would swap the teams internally

# 0.4.0-alpha

#### Nov 17, 2023

- Coach system! `.coach <side>` Starts coaching the specified side. Example: `.coach t` to start coaching terrorist side!
- MySQL Database is now supported! Now same DB can be used with multiple servers! Configure `csgo/cfg/MatchZy/database.json` according to your need!
- `.spawn` command now uses competitive spawns!
- Many commands added in Practice mode: `.clear`, `.fastforward`, `.god`, `.savenade <name> <optional description>`, `.loadnade <name>`, `.deletenade <name>`, `.importnade <code>`, `.listnades <optional filter>` (Refer to [Readme](https://github.com/shobhit-pathak/MatchZy#practice-mode-commands) for their descriptions!)
- Added text message for showing blind duration by a flashbang in practice session!
- Damage report will now be shown for every opponent player (even if damage is not dealt!)

![pracrelease](https://github.com/shobhit-pathak/MatchZy/assets/140690706/533b4d4b-7f09-48ec-a16e-3c6c9a8cb591)

# 0.3.0-alpha

#### Nov 14, 2023

- Team names can now be configured using `!team1 <teamname>` and `!team2 <teamname>` command. The same will be stored in Database and CSV.
- If team names are not configured, it will be configured automatically by picking a player's name randomly from both the teams (For example, if there is a player `WD-`, their teamname will be set to `team_WD-`)
- Damage report in chat will be shown on round end (similar to Faceit!)
- Chat timer delay can now be configured using `matchzy_chat_messages_timer_delay`. Example: `matchzy_chat_messages_timer_delay 12` 
- Players can be whitelisted by adding their steam64id in `cfg/MatchZy/whitelist.cfg`. Whitelisting is a toggleable feature and can be enabled using `.whitelist`. To enable it by default, set `matchzy_whitelist_enabled_default true` in `cfg/MatchZy/config.cfg`

![image](https://github.com/shobhit-pathak/MatchZy/assets/140690706/85b64823-419c-41d2-850d-d8f88fa4a4ca)

# 0.2.0-alpha

#### Nov 5, 2023

- Practice mode added ( with `.bot`, `.spawn`, `.ctspawn`, `.tspawn`, `.nobots` and `.exitprac` commands!)
- Chat prefixes can now be configured using `matchzy_chat_prefix`. Example: `matchzy_chat_prefix [{Green}MatchZy{Default}]` (More details related to colors is present in readme and `config.cfg`)
- Added RCON command via chat! Now admins can use `!rcon <command>` in chat to trigger a command to the server!
- Fixed some bugs related to Demo recording and match pause when match was restarted using `.restart`
