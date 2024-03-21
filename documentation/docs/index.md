MatchZy - Match Plugin for CS2!
==============

MatchZy is a plugin for CS2 (Counter Strike 2) for running and managing practice/pugs/scrims/matches with easy configuration!

[![Discord](https://discordapp.com/api/guilds/1169549878490304574/widget.png?style=banner2)](https://discord.gg/2zvhy9m7qg)


## What can MatchZy do?
MatchZy can solve a lot of match management requirements. It provides basic commands like `!ready`, `!unready`, `!pause`, `!unpause`, `!tac`, `!stop`, etc, provides matches stats, and much more!

**Feature Highligts:**

* Pug mode with simple commands to manage!
* Support of [Get5 Panel!](./get5.md)
* Support BO1/BO3/BO5 and Veto when using Match configuration or Get5 Panel!
* [Setting up matches](./match_setup) and locking players into their team
* Practice Mode with `.bot`, `.spawn`, `.ctspawn`, `.tspawn`, `.nobots`, `.rethrow`, `.last`, `.timer`, `.clear`, `.exitprac` and many more commands!
* Knife round (With expected logic, i.e., team with most players win. If same number of players, then team with HP advantage wins. If same HP, winner is decided randomly)
* Automatically starts demo recording and stop recording when match is ended (Make sure you have tv_enable 1)
* Automatically uploads demo on map end on the given URL.
* Players whitelisting (Thanks to [DEAFPS](https://github.com/DEAFPS)!)
* Coaching system
* Damage report after every round
* Support for round restore (Currently using the vanilla valve's backup system)
* Ability to create admin and allowing them access to admin commands
* Database Stats and CSV Stats! MatchZy stores data and stats of all the matches in a local SQLite database (MySQL Database is also supported!) and also creates a CSV file for detailed stats of every player in that match!
* Provides easy configuration
* And much more!!
