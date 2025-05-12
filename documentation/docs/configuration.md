# Configuration

All the configuration files related to MatchZy can be found in `csgo/cfg/MatchZy` (If you have extracted the contents properly, `MatchZy` folder should be there inside the cfg folder).

### Creating Admins
There are two ways to create an admin for MatchZy; you can choose the most convenient one according to your preference.

1. **Using CSSharp's Admin system:**

    You can create a new entry in the `/addons/counterstrikesharp/configs/admins.json` file with appropirate flags like mentioned in the below example:
    ```json
    {
    "WD-": {
        "identity": "76561198154367261",
        "flags": [
        "@css/root"
        ]
    },
    "Another admin": {
        "identity": "SteamID 2",
        "flags": [
        "@css/config",
        "@css/rcon"
        ]
    }
    }
    ```

    Flag-wise permissions:

    - `@css/root`: Grants access to all admin commands
    - `@css/config`: Grants access to config related admin commands
    - `@custom/prac`: Grants access to practice related admin commands
    - `@css/map`: Grants access to change map and toggle practice mode
    - `@css/rcon`: Grants access to trigger RCON commands using `!rcon <command>`
    - `@css/chat`: Grants access to send admin chat messages using `!asay <message>`


2. **Using MatchZy's Admin system:**

    Inside `csgo/cfg/MatchZy`, a file named `admins.json` should be present. If it is not there, it will be automatically created when the plugin is loaded. You can add Steam64 id of admins in that JSON file like mentioned in the below example:

    ```json
    {
        "76561198154367261": "",
        "<another_steam_id>": ""
    }
    ```

### Configuring MatchZy Settings (ConVars)
Again, inside `csgo/cfg/MatchZy`, a file named `config.cfg` should be present. This file is executed whenever the plugin is loaded. If you make any changes in this file and want to reload the config, simply execute `exec MatchZy/config.cfg` command on the server.

####`matchzy_knife_enabled_default`
:   Whether knife round is enabled by default or not. This is the default value, but knife can be toggled by [admins](#creating-admins) using .roundknife command.<br>**`Default: true`**

####`matchzy_minimum_ready_required`
:   Minimum ready players required to start the match. If set to 0, all connected players have to ready-up to start the match.<br>**`Default: 2`**

####`matchzy_stop_command_available`
:   Whether !stop/.stop command to restore the backup of the current round is enabled by default or not.<br>**`Default: false`**

####`matchzy_stop_command_no_damage`
:   Whether the stop command becomes unavailable if a player damages a player from the opposing team.<br>**`Default: false`**

####`matchzy_pause_after_restore`
:   Whether to pause the match after round restore or not. Players can unpause the match using !unpause/.unpause. (Both the teams will have to use unpause command) or admins can use `.fup` to force-unpause the game<br>**`Default: true`**

####`matchzy_whitelist_enabled_default`
:   Whether [whitelist](#whitelisting-players) is enabled by default or not. This is the default value, but whitelist can be toggled by admin using ``.whitelist`` command<br>**`Default: false`**

####`matchzy_kick_when_no_match_loaded`
:   Whether to kick all clients and prevent anyone from joining the server if no match is loaded. This means if server is in match mode, a match needs to be set-up using `matchzy_loadmatch`/`matchzy_loadmatch_url` to load and configure a match.<br>**`Default: false`**

####`matchzy_demo_path`
:   Path of folder in which demos will be saved. If defined, it must not start with a slash and must end with a slash. Set to empty string to use the csgo root. Example: `matchzy_demo_path MatchZy/`<br>**`Default: MatchZy/`**

####`matchzy_demo_name_format`
:   Format of demo filname. You may use {TIME}, {MATCH_ID}, {MAP}, {MAPNUMBER}, {TEAM1} and {TEAM2}. Demo files will be named according to the format specified. Do not include .dem format, it will be added automatically. Make sure to keep {TIME} in the format to create a unique demo file everytime.<br>**`Default: "{TIME}_{MATCH_ID}_{MAP}_{TEAM1}_vs_{TEAM2}"`**

####`matchzy_demo_upload_url`
:   If defined, recorded demo will be [uploaded](../gotv#automatic-upload) to this URL once the map ends. Make sure that the URL is wrapped in double quotes (""). 
Example: `matchzy_demo_upload_url "https://your-website.com/upload-endpoint"` <br>**`Default: ""`**

####`matchzy_kick_when_no_match_loaded`
:   Whether to kick all clients and prevent anyone from joining the server if no match is loaded. This means if server is in match mode, a match needs to be set-up using `matchzy_loadmatch`/`matchzy_loadmatch_url` to load and configure a match.<br>**`Default: false`**

####`matchzy_chat_prefix`
:   Chat prefix to show whenever a MatchZy message is sent to players. Available Colors: {Default}, {Darkred}, {Green}, {LightYellow}, {LightBlue}, {Olive}, {Lime}, {Red}, {Purple}, {Grey}, {Yellow}, {Gold}, {Silver}, {Blue}, {DarkBlue}, {BlueGrey}, {Magenta} and {LightRed}. Make sure to end your prefix with {Default} to avoid coloring the messages in your prefix color.<br>**`Default: [{Green}MatchZy{Default}]`**

####`matchzy_admin_chat_prefix`
:   Chat prefix to show whenever an admin sends message using `.asay <message>`. Avaiable Colors are mentioned above.<br>**`Default: [{Red}ADMIN{Default}]`**

####`matchzy_chat_messages_timer_delay`
:   Number of seconds of delay before sending reminder messages from MatchZy (like unready message, paused message, etc). Note: Changing this timer wont affect the active timer, so if you change this setting in warmup, you will have to restart warmup to make the change effective.<br>**`Default: 13[{Red}ADMIN{Default}]`**

####`matchzy_playout_enabled_default`
:   Whether playout (play max rounds) is enabled. This is the default value, but playout can be toggled by admin using `.playout` command.<br>**`Default: false`**

####`matchzy_reset_cvars_on_series_end`
:   Whether parameters from the cvars section of a match configuration are restored to their original values when a series ends.<br>**`Default: true`**

####`matchzy_use_pause_command_for_tactical_pause`
:   Whether to use !pause/.pause command for tactical pause or normal pause (unpauses only when both teams use unpause command, for admin force-unpauses the game).<br>**`Default: false`**

####`matchzy_autostart_mode`
:   Whether the plugin will load the match mode, the practice moder or neither by startup. 0 for neither, 1 for match mode, 2 for practice mode.<br>**`Default: 1`**

####`matchzy_save_nades_as_global_enabled`
:   Whether nades should be saved globally instead of being privated to players by default or not.<br>**`Default: false`**

####`matchzy_allow_force_ready`
:   Whether force ready using !forceready is enabled or not (Currently works in Match Setup only).<br>**`Default: true`**

####`matchzy_max_saved_last_grenades`
:   Maximum number of grenade history that may be saved per-map, per-client. Set to 0 to disable the limit and allow unlimited grenades to be stored.<br>**`Default: 512`**

####`matchzy_smoke_color_enabled`
:   If enabled, the smoke's color will be changed to player's team color (player's color seen in the radar) .<br>**`Default: false`**

####`matchzy_everyone_is_admin`
:   If set to true, everyone will be granted admin permissions for MatchZy.<br>**`Default: false`**

####`matchzy_show_credits_on_match_start`
:   Whether to show 'MatchZy Plugin by WD-' message on match start.<br>**`Default: true`**

####`matchzy_hostname_format`
:   The server hostname to use. Set to "" to disable/use existing. Available variables: {TIME}, {MATCH_ID}, {MAP}, {MAPNUMBER}, {TEAM1}, {TEAM2}, {TEAM1_SCORE}, {TEAM2_SCORE}<br>**`Default: MatchZy | {TEAM1} vs {TEAM2}`**

####`matchzy_match_start_message`
:   Message to show when the match starts. Use $$$ to break message into multiple lines. Set to "" to disable. Available Colors: {Default}, {Darkred}, {Green}, {LightYellow}, {LightBlue}, {Olive}, {Lime}, {Red}, {Purple}, {Grey}, {Yellow}, {Gold}, {Silver}, {Blue}, {DarkBlue}. Example usage: matchzy_match_start_message {Green} Welcome to the server! {Default} $$$ Agent models are not allowed and may lead to {Red}disqualification!{Default}
<br>**`Default: ""`**

####`matchzy_enable_match_scrim`
:   Allows match setup with no players defined and run like a scrim.<br>**`Default: false`**

####`matchzy_loadbackup`
:   Loads a match backup from the given file. Relative to `csgo/MatchZyDataBackup/`.

####`matchzy_loadbackup_url`
:   Loads a match backup from a remote host by sending an HTTP(S) GET to the given URL. You may optionally provide an HTTP header and value pair using the header name and header value arguments. You should put all arguments inside quotation marks ("").

####`matchzy_remote_backup_url`
:   If defined, MatchZy will automatically send backups to this URL in an HTTP POST request. If no protocol is provided, http:// will be prepended to this value. Requires the SteamWorks extension. 

####`matchzy_remote_backup_header_key`
:   If this and matchzy_remote_backup_header_value are defined, this header name and value will be used for your backup upload HTTP request. **`Default: "Authorization"`**

####`matchzy_remote_backup_header_value`
:   If this and matchzy_remote_backup_header_key are defined, this header name and value will be used for your backup upload HTTP request. **`Default: ""`**

####`matchzy_enable_damage_report`
:   Whether to show damage report after each round or not. **`Default: "true"`**

####`matchzy_addplayer <steam64> <team1|team2|spec> [name]`
:   Adds a Steam64 to the provided team. The name parameter locks the player's name.

####`matchzy_removeplayer <steam64>`
:   Removes a Steam64 from all teams

### Configuring Warmup/Knife/Live/Prac CFGs
Again, inside `csgo/cfg/MatchZy`, files named `warmup.cfg`, `knife.cfg`, `live.cfg` and `prac.cfg` should be present. These configs are executed when Warmup, Knife, Live and Practice Mode is started respectively.

You can modify these files according to your requirements, or add live_override.cfg / live_wingman_override.cfg to make "overriding" config.

If these configs are not found in the expected location, MatchZy executes the default configs which are present in the code.

### Whitelisting players
Again, inside `csgo/cfg/MatchZy`, there will be a file called `whitelist.cfg`. You can add Steam64 id of whitelisted players like mentioned in the below example:

```
76561198154367261
steamid2
steamid3
```

## Match/Players Stats and Data

### Database Stats

MatchZy comes with a default database (SQLite), which configures itself automatically. MySQL Database can also be used with MatchZy!
Currently we are using 2 tables, `matchzy_stats_matches` and `matchzy_stats_players`. As their names suggest, `matchzy_stats_matches` holds the data of every match, like matchid, team names, scores, etc.
Whereas, `matchzy_stats_players` stores data/stats of every player who played in that match. It stores data like matchid, kills, deaths, assists, and other important stats!

### Using MySQL Database with MatchZy

To use MySQL Database with MatchZy, open `csgo/cfg/MatchZy/database.json` file. It's content will be like this:
```json
{
    "DatabaseType": "SQLite",
    "MySqlHost": "your_mysql_host",
    "MySqlDatabase": "your_mysql_database",
    "MySqlUsername": "your_mysql_username",
    "MySqlPassword": "your_mysql_password",
    "MySqlPort": 3306
}
```
Here, change the `DatabaseType` from `SQLite` to `MySQL` and then fill-up all the other details like host, database, username, etc.
MySQL Database is useful for those who wants to use a common database across multiple servers!

### CSV Stats
Once a match is over, data is pulled from the database and a CSV file is written in the folder:
`csgo/MatchZy_Stats`. This folder will contain CSV file for each match (file name pattern: `match_data_map{mapNumber}_{matchId}.csv`) and it will have the same data which is present in `matchzy_stats_players`.

There is a scope of improvement here, like having the match score in the CSV file or atleast in the file name patter. I'll make this change soon!


## Events and HTTP Logging

####`matchzy_remote_log_url`
:   The URL to send all [events](../events_and_forwards) to (POST request). Set to empty string to disable. [OpenAPI Doc for the events](events.html)<br>**`Default: ""`**<br>Usage: `matchzy_remote_log_url "url"`<br>Alias: `get5_remote_log_url`

####`matchzy_remote_log_header_key`
:   If this and matchzy_remote_log_header_value are defined, this header name and value will be added in your [HTTP Post requests'](../events_and_forwards) header.<br>**`Default: ""`**<br>Usage: `matchzy_remote_log_header_key "Authorization"`<br>Alias: `get5_remote_log_header_key`

####`matchzy_remote_log_header_value`
:   If this and matchzy_remote_log_header_key are defined, this header name and value will be added in your [HTTP Post requests'](../events_and_forwards) header.<br>**`Default: ""`**<br>Usage: `matchzy_remote_log_header_value "header_value"`<br>Alias: `get5_remote_log_header_value`
