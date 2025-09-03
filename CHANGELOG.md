# MatchZy Changelog

# 0.8.13

#### September 03, 2025

- Fixed coach bomb bug and updated CSS version.
- Added `matchzy_demo_recording_enabled` convar to toggle demo recording.
- Fixed the Map Winner Logic in MapWinner event
- Fixed first Map Name in database stats

# 0.8.12

#### August 25, 2025

- Updated CSS Version to fix `.last` and `.throw` commands.

# 0.8.11

#### August 9, 2025

- Updated CSS Version
- Fixed SmokeGrenadeProjectile Signatures

# 0.8.10

#### May 12, 2025

- Fixed decoy airtime in Practice Mode. (Switched `EventDecoyDetonate` to `EventDecoyStarted` so it takes the air time once it lands, and not when the decoy finishes)
- Added `.ct` and `.t` command for side selection after knife round.
- Rename `ive_wingman_override.cfg` to `live_wingman_override.cfg`
- Fixed es-ES.json

# 0.8.9

#### April 3, 2025

- Fixed issue with EventPlayerChat (. commands) post CSSharp v315 update.

# 0.8.8

#### January 1, 2025

- Fixed issue with !pause command where non-admin players were not able to take pauses when `matchzy_tech_pause_flag ""` was set.

# 0.8.7

#### December 4, 2024

- Fixed backup / restore on Windows.
- Dryrun will now have random competitive spawns rather than same spawns every time.
- Made `.pause` / `.tech` toggleable. Use `matchzy_enable_tech_pause` convar to toggle.
- Updated pt-PT translation.
- Fixed live_override

# 0.8.6

#### September 13, 2024

- Improvements in coach, now coaches will spawn on the fixed defined spawn to avoid spawning and getting stuck with the players. Spawns will be defined in `addons/counterstrikesharp/plugins/MatchZy/spawns/coach/<map_name>.json`. Each map will have its json file, in which there will be 2 keys, "3" and "2". 3 -> CT, 2 -> T and the values will be an array of Vector and QAngle objects.
- Added `.showspawns` and `.hidespawns` command for Practice mode to toggle highlighting of competitive spawns. (Image attached)
- Removed auto-join of players in match setup which was causing players to spawn under the ground.
- Added `.rr` alias for `.restart` command.

# 0.8.5

#### August 27, 2024

- Added `matchzy_match_start_message` convar to configure message to show when the match starts. Use $$$ to break message into multiple lines.
- Some improvements and guard checks in coach system
- Fixed `matchzy_hostname_format` not getting disabled on setting its value to ""
- Fixed winner side in `round_end` event

# 0.8.4

#### August 27, 2024

- Fixes in coach system, where players would spawn into each other.
- Improved backup loading (teams will be locked automatically with the first restore if match setup was being used.)
- Fixed veto bug where ready system would not work after the veto.
- Updated Uzbek translations 

# 0.8.3

#### August 25, 2024

- Fixed issues with backup restore where `.restore` command would show as round restored, but nothing happened. (Improved file naming and backup saving logic)
- Updated live.cfg as per new rules (mp_team_timeout_max 3; mp_team_timeout_ot_max 1; mp_team_timeout_ot_add_each 1)
- Added css_globalnades alias (!globalnades) for css_save_nades_as_global / .globalnades

# 0.8.2

#### August 25, 2024

- Added capability to have multiple coaches in a team.
- Coaches will now be invisible, they will drop the bomb on the spawn if they get it and will die 1 second before freezetime ends.
- If a match is loaded, player will directly join their respective team, skipping the join team menu.
- Fixed a bug where loading a saved nade would make the player stuck.
- Added `matchzy_stop_command_no_damage` convar to determine whether the stop command becomes unavailable if a player damages a player from the opposing team.
- `.map` command can now be used without "de_" prefix for maps. (Example: .map dust2)

# 0.8.1

#### August 17, 2024

- Added matchzy_enable_damage_report convar to toggle damage report after every round.
- Fixed bad demo name formatting.
- Updated Uzbek translations.

# 0.8.0

#### August 17, 2024

- Improved backup and restore system. (Added matchzy_loadbackup and matchzy_loadbackup_url commands, now round backups will be stored in .json file in csgo/MatchZyDataBackup/ directory which will have valve backup and other match config data.)
- Added matchzy_listbackups which lists all the backups for the provided matchid. By default lists backups of the current match.
- Added matchzy_hostname_format for hostname formatting.
- Improved player color smokes in practice mode
- Fixed .last grenade's player rotation
- Added switching of maps without adding de_ prefix (using .map command)
- Marked the requestBody as required: true in event_schema.yml

# 0.7.13

#### July 07, 2024

- Added alias for .savenade -> .sn, .loadnade -> .ln, .deletenade -> .dn, .importnade -> .in, .listnades -> .lin, .ctspawn -> .cts, .tspawn -> .ts
- Added smart quering for nadenames, where the closest name is being selected for loading names (.ln mid can be used to load a nade with name midflash)
- Added to allow the same lineup-name on different maps, so you can pick like b-smoke multiple times, but once per map. Updated logic for savenade, deletenade, importnade for this.
- Added missing ! commands for listnades, importnade and deletenade.
- Changed cash_team_planted_bomb_but_defused from 800 to 600 as per the update https://store.steampowered.com/news/app/730/view/4177730135016140040
- Added "override" config for live.cfg and live_wingman.cfg (Simply create live_override.cfg and live_wingman_override.cfg in the cfg folder if you want to override any of the commands.)
- Added Uzbek, Japanese, Hungarian and Traditional Chinese translations


# 0.7.12

#### June 27, 2024

- Removed unused cvars from cfgs which were causing the server to crash with the new CS# versions.
- Added MatchZyOnDemoUploadEnded Event ater demo is uploaded
- Fixed SendEventAsync Post failing when header is not empty with empty value
- Fixed decoy message localization id
- Made MatchID as int

# 0.7.11

#### May 19, 2024

- Improved `.help` command with better readability and updated commands
- Fixed overtime getting automatically enabled even if turned off in `live.cfg`
- Added `matchzy_show_credits_on_match_start` config convar to toggle 'MatchZy Plugin by WD-' message on match start.
- Added gradient while printing `KNIFE!` and `LIVE!` message.
- Added `.pip` alias for `.traj` command to toggle `sv_grenade_trajectory_prac_pipreview` in practice mode.

# 0.7.10

#### May 19, 2024

- Added `matchzy_smoke_color_enabled` config convar for practice mode which changes the smoke's color to player's team color (player's color seen in the radar)
- Added `.bestspawn` command which teleports you to your team's closest spawn from your current position
- Added `.worstspawn` command which teleports you to your team's furthest spawn from your current position
- Added `.bestctspawn` command which teleports you to CT team's closest spawn from your current position
- Added `.worstctspawn` command which teleports you to CT team's furthest spawn from your current position
- Added `.besttspawn` command which teleports you to T team's closest spawn from your current position
- Added `.worsttspawn` command which teleports you to T team's furthest spawn from your current position
- Practice mode will no longer have `warmup` help text.

# 0.7.9

#### May 06, 2024

- Updated `pt-BR` and `ru` translations.
- Added `.notready` alias for `.unready`.
- Added `.forceend` alias for `.restart`/`.endmatch`.

# 0.7.8

#### May 02, 2024

- Added `.solid` command in practice mode to toggle `mp_solid_teammates`.
- Added `.impacts` command in practice mode to toggle `sv_showimpacts`.
- Added `.traj` command in practice mode to toggle `sv_grenade_trajectory_prac_pipreview`.
- Fixed double prefix in damage report.
- Added commonly used aliases for multiple commands. (`.force` and `.forcestart` for force-start, `.tactics` to enable practice mode, `.noblind` to toggle no-flash in practice, `.cbot` to spawn a crouched-bot in practice.)
- Added `pt-BR` and `zh-Hans` updated translations.
- Renamed the `pt-pt` translation file to `pt-PT`
- Fixed translation in `fr`

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
