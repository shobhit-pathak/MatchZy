# Usage Commands
Most of the commands can also be used using ! prefix instead of . (like !ready)

- `.ready` Marks the player ready
- `.unready` Marks the player unready
- `.pause` Pauses the match in freezetime (Tactical or normal pause, depends on `matchzy_use_pause_command_for_tactical_pause`).
- `.tech` Pauses the match in freezetime.
- `.unpause` Request for unpausing the match. Both teams need to type .unpause to unpause the match
- `.stay` Stays on the same side (For knife winner, after the knife round)
- `.switch`/`.swap` Switches the side (For knife winner, after the knife round)
- `.stop` Restore the backup of the current round (Both teams need to type .stop to restore the current round)
- `.tac` Starts a tactical timeout
- `.coach <side>` Starts coaching the specified side. Example: `.coach t` to start coaching terrorist side!

# Practice Mode Commands

- `.spawn <number>` Spawns to the provided competitive spawn number of same team
- `.ctspawn <number>` Spawns to the provided competitive spawn number of CT
- `.tspawn <number>` Spawns to the provided competitive spawn number of T
- `.bot` Adds a bot on user's current position
- `.nobots` Removes all the bots
- `.clear` Clears all the active smokes, molotoves and incendiaries
- `.fastforward` Fastforwards the server time to 20 seconds
- `.god` Turns on god mode
- `.savenade <name> <optional description>` Saves a lineup
- `.loadnade <name>` Loads a lineup
- `.deletenade <name>` Deletes a lineup from file
- `.importnade <code>` Upon saving a lineup a code will be printed to chat, alternatively those can be retrieved from the savednades.cfg
- `.listnades <optional filter>` Lists either all saved lineups ever or if given a filter only those that match the filter

# Admin Commands

- `.start` Force starts a match.
- `.restart` Force restarts/resets a match.
- `.forcepause` Pauses the match as an admin (Players cannot unpause the admin-paused match). (`.fp` for shorter command)
- `.forceunpause` Force unpauses the match. (`.fup` for shorter command)
- `.restore <round>` Restores the backup of provided round number.
- `.roundknife` / `.rk` Toggles the knife round. If disabled, match will directly go from Warmup phase to Live phase.
- `.playout` Toggles playout (If playout is enabled, all rounds would be played irrespective of winner. Useful in scrims!)
- `.whitelist` Toggles whitelisting of players. To whitelist a player, add the steam64id in `cfg/MatchZy/whitelist.cfg`
- `.readyrequired <number>` Sets the number of ready players required to start the match. If set to 0, all connected players will have to ready-up to start the match.
- `.settings` Displays the current setting, like whether knife is enabled or not, value of readyrequired  players, etc.
- `.map <mapname>` Changes the map
- `.asay <message>` Say as an admin in all chat
- `.reload_admins` Reloads admins from `admins.json`
- `.team1 <name>` Sets name for Team 1 (CT by default)
- `.team2 <name>` Sets name for Team 2 (Terrorist by default)
- `.prac` Starts Practice Mode
- `.exitprac` Exits from practice mode and loads Match mode.
- `.rcon <command>` Sends command to the server
