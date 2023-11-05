MatchZy - Match Plugin for CS2!
==============

MatchZy is a plugin for CS2 (Counter Strike 2) for running and managing practice/pugs/scrims/matches with easy configuration!

[![Discord](https://discordapp.com/api/guilds/1169549878490304574/widget.png?style=banner2)](https://discord.gg/2zvhy9m7qg)

## Installation
* Install Metamod (https://cs2.poggu.me/metamod/installation/)
* Install CounterStrikeSharp (CSSharp) (https://docs.cssharp.dev/guides/getting-started/). (**Note**: This step can be skipped if you install [MatchZy with CSSharp release](https://github.com/shobhit-pathak/MatchZy/releases/))
	* Go to this link: https://github.com/roflmuffin/CounterStrikeSharp/actions/runs/6732399022
	* Scroll down and download 'counterstrikesharp-with-runtime'
	* Extract the addons folder to the csgo/ directory of the dedicated server. The contents of your addons folder should contain both the counterstrikesharp folder and the metamod folder
	* Verify the installation by typing `meta list` on server console. You should see CounterStrikeSharp plugin by Roflmuffin
	* You can refer to https://docs.cssharp.dev/guides/getting-started for detailed instructions. Initially, it may seem a bit hectic, but trust me, it's worth it! :P 
* Install MatchZy 
	* Download the latest [MatchZy release](https://github.com/shobhit-pathak/MatchZy/releases/) and extract the files to the csgo/ directory of the dedicated server.
	* Verify the installation by typing `css_plugins list` and you should see MatchZy by WD- listed there.

**Note**: CSSharp plugin is only for servers on Linux systems.

## What can MatchZy do?
MatchZy can solve a lot of match management requirements. It provides basic commands like `!ready`, `!unready`, `!pause`, `!unpause`, `!tac`, `!stop`, etc, provides matches stats, and much more!

**Feature higlights:**
- Practice Mode with `.bot`, `.spawn`, `.ctspawn`, `.tspawn`, `.nobots` and `.exitprac` commands!
- Warmup with infinite money 🤑
- Knife round (With expected logic, i.e., team with most players win. If same number of players, then team with HP advantage wins. If same HP, winner is decided randomly)
- Start live match (after side selection is done by knife winner. Knife round can also be disabled).
- Automatically starts demo recording and stop recording when match is ended (Make sure you have tv_enable 1)
- Support for round restore (Currently using the vanilla valve's backup system)
- Ability to create admin and allowing them access to admin commands
- Database Stats and CSV Stats! MatchZy stores data and stats of all the matches in a local SQLite database and also creates a CSV file for detailed stats of every player in that match!
- Provides easy configuration
- And much more!!


## Usage Commands
Most of the commands can also be used using ! prefix instead of . (like !ready)

- `.ready` Marks the player ready
- `.unready` Marks the player unready
- `.pause` Pauses the match in freezetime.
- `.unpause` Request for unpausing the match. Both teams need to type .unpause to unpause the match
- `.stay` Stays on the same side (For knife winner, after the knife round)
- `.switch` Switches the side (For knife winner, after the knife round)
- `.stop` Restore the backup of the current round (Both teams need to type .stop to restore the current round)
- `.tac` Starts a tactical timeout

### Practice Mode Commands

- `.spawn <number>` Spawns to the provided spawn number of same team
- `.ctspawn <number>` Spawns to the provided spawn number of CT
- `.tspawn <number>` Spawns to the provided spawn number of T
- `.bot` Adds a bot on user's current position
- `.nobots` Removes all the bots

### Admin Commands

- `.start` Force starts a match.
- `.restart` Force restarts/resets a match.
- `.pause` Pauses the match as an admin (Players cannot unpause the admin-paused match).
- `.unpause` Force unpauses the match.
- `.restore <round>` Restores the backup of provided round number.
- `.knife` Toggles the knife round. If disabled, match will directly go from Warmup phase to Live phase.
- `.readyrequired <number>` Sets the number of ready players required to start the match. If set to 0, all connected players will have to ready-up to start the match.
- `.settings` Displays the current setting, like whether knife is enabled or not, value of readyrequired  players, etc.
- `.map <mapname>` Changes the map
- `.asay <message>` Say as an admin in all chat
- `.reload_admins` Reloads admins from `admins.json`
- `.prac` Starts Practice Mode
- `.exitprac` Exits from practice mode and loads Match mode.

## Configuration

All the configuration files related to MatchZy can be found in `csgo/cfg/MatchZy` (If you have extracted the contents properly, `MatchZy` folder should be there inside the cfg folder).

### Creating Admins
Inside `csgo/cfg/MatchZy`, a file named `admins.json` should be present. If it is not there, it will be automatically created when the plugin is loaded. You can add Steam64 id of admins in that JSON file like mentioned in the below example:

```
{
    "76561198154367261": "",
    "<another_steam_id>: ""
}
```

### Configuring MatchZy Settings (ConVars)
Again, inside `csgo/cfg/MatchZy`, a file named `config.cfg` should be present. This file is executed whenever the plugin is loaded. If you make any changes in this file and want to reload the config, simply execute `exec MatchZy/config.cfg` command on the server.

Content of the file should be something like mentioned below, it also has description of all the commands.
```
// This config file is executed when MatchZy plugin is loaded
// Do not add commands other than matchzy config console variables
// More configurations and variables will be coming in future updates.

// Whether knife round is enabled by default or not. Default value: true
// This is the default value, but knife can be toggled by admin using .knife command
matchzy_knife_enabled_default true

// Minimum ready players required to start the match. If set to 0, all connected players have to ready-up to start the match. Default: 2
matchzy_minimum_ready_required 2

// Path of folder in which demos will be saved. If defined, it must not start with a slash and must end with a slash. Set to empty string to use the csgo root.
// Example: matchzy_demo_path MatchZy/
// A folder named MatchZy will be created in csgo folder if it does not exist and will store the recorded demos in it. Default value is MatchZy/ which means demos will be stored in MatchZy/
matchzy_demo_path MatchZy/

// Whether !stop/.stop command is enabled by default or not. Default value: false
// Note: We are using Valve backup system to record and restore the backups. In most of the cases, this should be just fine.
// But in some cases, this may not be reliable hence default value is false
matchzy_stop_command_available false

// Whether to pause the match after round restore or not. Default value: true
// Players/admins can unpause the match using !unpause/.unpause. (For players, both the teams will have to use unpause command)
matchzy_pause_after_restore true

// Chat prefix to show whenever a MatchZy message is sent to players. Default value: [{Green}MatchZy{Default}]
// Available Colors: {Default}, {Darkred}, {Green}, {LightYellow}, {LightBlue}, {Olive}, {Lime}, {Red}, {Purple}, {Grey}, {Yellow}, {Gold}, {Silver}, {Blue}, {DarkBlue}
// {BlueGrey}, {Magenta} and {LightRed}. Make sure to end your prefix with {Default} to avoid coloring the complete messages in your prefix color
matchzy_chat_prefix [{Green}MatchZy{Default}]
```


### Configuring Warmup/Knife/Live/Prac CFGs
Again, inside `csgo/cfg/MatchZy`, files named `warmup.cfg`, `knife.cfg`, `live.cfg` and `prac.cfg` should be present. These configs are executed when Warmup, Knife, Live and Practice Mode is started respectively.

You can modify these files according to your requirements.

If these configs are not found in the expected location, MatchZy executes the default configs which are present in the code.

## Match/Players Stats and Data

### Database Stats

MatchZy comes with a default database (SQLite), which configures itself automatically.
Currently we are using 2 tables, `matchzy_match_data` and `matchzy_player_stats`. As their names suggest, `matchzy_match_data` holds the data of every match, like matchid, team names, scores, etc.
Whereas, `matchzy_player_stats` stores data/stats of every player who played in that match. It stores data like matchid, kills, deaths, assists, and other important stats!

### CSV Stats
Once a match is over, data is pulled from the SQLite database and a CSV file is written in the folder:
`csgo/MatchZy_Stats`. This folder will contain CSV file for each match (file name pattern: `match_data_{matchid}.csv`) and it will have the same data which is present in `matchzy_player_stats`.

There is a scope of improvement here, like having the match score in the CSV file or atleast in the file name patter. I'll make this change soon!


## What's missing? Limitations?

- Configuring team names (Currently default names like Counter-Terrorist and Terrorist will be used. This is because CSSharp does not provide access to ConVars yet, which is required to maintain team names and perform operations on them, like swapping team names interally after side switch in halftimes)
	
    - Though you can manually configure team names using mp_teamname_1 and mp_teamname_2 commands, but they will not be reflected in Stats due to above mentioned reason.

- Locking players in a team (Since this is a very important requirement for matches, this will be done soon!)
- Configuring a match using JSON file and/or HTTP Request. (This is also an important requirement and will be implemented once the above points are closed!)
- Sending events and data on a webhook. (I'll be looking into this as well asap, so that we can build a web panel around MatchZy though which we can configure matches and receive events and stats.)
- Map veto system

## Future Scope

Since this is my first time working with a plugin for Counter Strike and also my first time working with C#, I have to sort a lot of things in my plugin code :P

But apart from that, my first aim for the future of MatchZy would be to solve the above mentioned limitations, and side-by-side keep implementing other important features/requirements. Feature suggestions and improvements are welcomed!

## For Developers

Since this plugin is built on C#, [.NET 7.0 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/7.0) will be required if you intend to make changes in this plugin. Once you have that installed,
1. Clone the repository
2. Edit the MatchZy.csproj file and add the correct path for CounterStrikeSarp.API.dll file (This file comes with the CounterStrikeSharp plugin, mentioned in installation steps)
3. Use `dotnet restore` to restore and install the dependencies.
4. Make your changes
5. Use `dotnet publish` command and you'll get a folder called `bin` in your plugin directory.
6. Navigate to `bin/Debug/net7.0/publish/`and copy all the content from there and paste it into `csgo/addons/counterstrikesharp/plugins/MatchZy` (CounterStrikeSharp.API.dll and CounterStrikeSharp.API.pdb can be skipped)
7. It's done! Now you can test your changes, and also contribute to the plugin if you want to :p 

## License
MIT

## Credits and thanks!
* [Get5](https://github.com/splewis/get5) - A lot of functionalities and workings have been referred from Get5 and they did an amazing job for managing matches in CS:GO. Huge thanks to them!
* [eBot](https://github.com/deStrO/eBot-CSGO) - Amazing job in CS:GO and then provided this great panel again in CS2 which is helping a lot of organizers now. Some logics have been referred from eBot as well!
* [CounterStrikeSharp](https://github.com/roflmuffin/CounterStrikeSharp/) - Amazing job with development of CSSharp which gave us a platform to build our own plugins and also sparked my interest in plugin development!
* [AlliedModders and community](https://alliedmods.net/) - They are the reason this whole plugin was possible! They are very helpful and inspire a lot!
* [LOTGaming](https://lotgaming.xyz/) - Helped me a lot with initial testing and provided servers on different systems and locations!
* [CHR15cs](https://github.com/CHR15cs) - Helped me a lot with the practice mode!
