using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Core.Capabilities;
using CounterStrikeSharp.API.Modules.Entities;
using CounterStrikeSharp.API.Modules.Utils;
using MenuManager;
using System.Collections.Generic;
using System.Linq;

namespace MoveSpec;

public class MoveSpec : BasePlugin
{
    private IMenuApi? _menuApi;
    private readonly PluginCapability<IMenuApi?> _menuApiCapability = new("menu:nfcore");

    private CCSPlayerController? _tCaptain;
    private CCSPlayerController? _ctCaptain;
    private List<CCSPlayerController> _availablePlayers = new();
    private bool _isPickingInProgress = false;
    private bool _isCTTurn = true; // CT picks first
    private int _pickedPlayers = 0; // 8 picks (4 per team)

    public override string ModuleName => "MoveSpec";
    public override string ModuleVersion => "1.0.0";
    public override string ModuleAuthor => "NoxianWill";
    public override string ModuleDescription => "MoveSpec: Admin team picking and captain system with menu support.";

    public override void OnAllPluginsLoaded(bool hotReload)
    {
        _menuApi = _menuApiCapability.Get();
    }

    [ConsoleCommand("css_spec", "Moves all players to spectators and starts captain picking")]
    public void OnSpecCommand(CCSPlayerController? player, CommandInfo command)
    {
        if (!IsAdmin(player)) return;
        if (_menuApi == null)
        {
            player?.PrintToChat("[MoveSpec] MenuManager not available!");
            return;
        }
        if (_tCaptain == null || _ctCaptain == null)
        {
            player?.PrintToChat("[MoveSpec] Both captains must be set first!");
            return;
        }

        _availablePlayers.Clear();
        foreach (var p in Utilities.GetPlayers())
        {
            if (p != null && p.IsValid && p.PlayerPawn.IsValid && p != _tCaptain && p != _ctCaptain)
            {
                p.ChangeTeam(CsTeam.Spectator);
                _availablePlayers.Add(p);
            }
        }

        _isPickingInProgress = true;
        _isCTTurn = true;
        _pickedPlayers = 0;
        PrintToAll("[MoveSpec] Captain picking has started! CT captain picks first.");
        ShowPickingMenu(_ctCaptain);
    }

    [ConsoleCommand("css_tcapt", "Sets the Terrorist team captain")]
    [CommandHelper(minArgs: 1, usage: "<player name>")]
    public void OnTCaptCommand(CCSPlayerController? player, CommandInfo command)
    {
        if (!IsAdmin(player)) return;
        var targetName = command.GetArg(1);
        var target = FindPlayerByName(targetName);
        if (target == null)
        {
            player?.PrintToChat("[MoveSpec] Player not found!");
            return;
        }
        _tCaptain = target;
        target.ChangeTeam(CsTeam.Terrorist);
        PrintToAll($"[MoveSpec] Terrorist captain set to: {target.PlayerName}");
    }

    [ConsoleCommand("css_ctcapt", "Sets the Counter-Terrorist team captain")]
    [CommandHelper(minArgs: 1, usage: "<player name>")]
    public void OnCTCaptCommand(CCSPlayerController? player, CommandInfo command)
    {
        if (!IsAdmin(player)) return;
        var targetName = command.GetArg(1);
        var target = FindPlayerByName(targetName);
        if (target == null)
        {
            player?.PrintToChat("[MoveSpec] Player not found!");
            return;
        }
        _ctCaptain = target;
        target.ChangeTeam(CsTeam.CounterTerrorist);
        PrintToAll($"[MoveSpec] Counter-Terrorist captain set to: {target.PlayerName}");
    }

    private void ShowPickingMenu(CCSPlayerController? captain)
    {
        if (!_isPickingInProgress || _menuApi == null || captain == null || !captain.IsValid)
            return;

        var menu = _menuApi.GetMenu($"Pick a player for {(captain.Team == CsTeam.CounterTerrorist ? "CT" : "T")} team");
        foreach (var p in _availablePlayers)
        {
            if (p != null && p.IsValid)
            {
                menu.AddMenuOption(p.PlayerName, (picker, option) => OnPlayerPicked(p, captain));
            }
        }
        menu.Open(captain);
    }

    private void OnPlayerPicked(CCSPlayerController picked, CCSPlayerController captain)
    {
        if (!_isPickingInProgress || picked == null || !picked.IsValid)
            return;

        picked.ChangeTeam(captain.Team);
        _availablePlayers.Remove(picked);
        _pickedPlayers++;

        PrintToAll($"[MoveSpec] {captain.PlayerName} picked {picked.PlayerName}");

        if (_pickedPlayers >= 8)
        {
            _isPickingInProgress = false;
            PrintToAll("[MoveSpec] Team picking is complete! Teams are now 5v5.");
            return;
        }

        // Switch turns
        _isCTTurn = !_isCTTurn;
        var nextCaptain = _isCTTurn ? _ctCaptain : _tCaptain;
        ShowPickingMenu(nextCaptain);
    }

    private CCSPlayerController? FindPlayerByName(string name)
    {
        return Utilities.GetPlayers().FirstOrDefault(p => p.PlayerName.Contains(name, StringComparison.OrdinalIgnoreCase));
    }

    private bool IsAdmin(CCSPlayerController? player)
    {
        if (player == null || !player.IsValid || !player.PlayerPawn.IsValid)
            return false;
        if (!AdminManager.PlayerHasPermissions(player, "@css/admin"))
        {
            player.PrintToChat("[MoveSpec] You don't have permission to use this command!");
            return false;
        }
        return true;
    }

    private void PrintToAll(string message)
    {
        Server.PrintToChatAll($" \x04{message}");
    }
} 