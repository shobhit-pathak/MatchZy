using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Utils;
using CounterStrikeSharp.API.Modules.Timers;
using CounterStrikeSharp.API.Modules.Memory;

using System;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Net.Mime;



namespace MatchZy
{

    public partial class MatchZy
    {
        public const string sleepCfgPath = "MatchZy/sleep.cfg";

        public void StartSleepMode()
        {
            if (matchStarted) return;
            isSleep = true;
            isPractice = false;
            isWarmup = false;
            readyAvailable = false;
            matchStarted = false;
            isSideSelectionPhase = false;
            isMatchLive = false;

            var absolutePath = Path.Join(Server.GameDirectory + "/csgo/cfg", sleepCfgPath);

            if (File.Exists(Path.Join(Server.GameDirectory + "/csgo/cfg", sleepCfgPath)))
            {
                Log($"Starting Sleep Mode! Executing Sleep CFG from {sleepCfgPath}");
                Server.ExecuteCommand($"exec {sleepCfgPath}");
            }
            else
            {
                Log($"Starting Sleep Mode! Sleep CFG not found in {absolutePath}, using default CFG!");
                ExecUnpracCommands();
                Server.ExecuteCommand("""exec gamemode_competitive.cfg;""");
            }
            Server.PrintToChatAll($"{chatPrefix} [MatchZy] deactivated!");
        }

        [ConsoleCommand("css_sleep", "Starts sleep mode")]
        public void OnSleepCommand(CCSPlayerController? player, CommandInfo? command)
        {
            if (!IsPlayerAdmin(player, "css_sleep", "@css/map", "@custom/prac"))
            {
                SendPlayerNotAdminMessage(player);
                return;
            }

            if (matchStarted)
            {
                ReplyToUserCommand(player, "Sleep Mode cannot be started when a match has been started!");
                return;
            }
            StartSleepMode();
        }

    }
}
