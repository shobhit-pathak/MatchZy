using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Cvars;
using System.IO.Compression;
using System.Net.Http.Json;
using System.Text;

namespace MatchZy
{
    public partial class MatchZy
    {
        public string demoPath = "MatchZy/";
        public string demoNameFormat = "{TIME}_{MATCH_ID}_{MAP}_{TEAM1}_vs_{TEAM2}";
        public string demoUploadURL = "";
        public string demoUploadHeaderKey = "";
        public string demoUploadHeaderValue = "";

        public string activeDemoFile = "";

        public bool isDemoRecording = false;

        public void StartDemoRecording()
        {

            string demoFileName = FormatDemoName();
            try
            {
                string? directoryPath = Path.GetDirectoryName(Path.Join(Server.GameDirectory + "/csgo/", demoPath));
                if (directoryPath != null)
                {
                    if (!Directory.Exists(directoryPath))
                    {
                        Directory.CreateDirectory(directoryPath);
                    }
                }
                string tempDemoPath = demoPath == "" ? demoFileName : demoPath + demoFileName;
                activeDemoFile = tempDemoPath;
                Log($"[StartDemoRecoding] Starting demo recording, path: {tempDemoPath}");
                Server.ExecuteCommand($"tv_record {tempDemoPath}");
                isDemoRecording = true;
            }
            catch (Exception ex)
            {
                Log($"[StartDemoRecording - FATAL] Error: {ex.Message}. Starting demo recording with path. Name: {demoFileName}");
                // This is to avoid demo loss in any case of exception
                Server.ExecuteCommand($"tv_record {demoFileName}");
                isDemoRecording = true;
            }

        }

        public void StopDemoRecording(float delay, string activeDemoFile, long liveMatchId, int currentMapNumber)
        {
            Log($"[StopDemoRecording] Going to stop demorecording in {delay}s");
            string demoPath = Path.Join(Server.GameDirectory + "/csgo/", activeDemoFile);
            AddTimer(delay, () =>
            {
                if (isDemoRecording) Server.ExecuteCommand($"tv_stoprecord");
                AddTimer(15, () =>
                {
                    Task.Run(async () =>
                    {
                        await UploadDemoAsync(demoPath, liveMatchId, currentMapNumber);
                    });
                });
            });
        }

        public int GetTvDelay()
        {
            bool tvEnable = ConVar.Find("tv_enable")!.GetPrimitiveValue<bool>();
            if (!tvEnable) return 0;

            bool tvEnable1 = ConVar.Find("tv_enable1")!.GetPrimitiveValue<bool>();
            int tvDelay = ConVar.Find("tv_delay")!.GetPrimitiveValue<int>();

            if (!tvEnable1) return tvDelay;
            int tvDelay1 = ConVar.Find("tv_delay1")!.GetPrimitiveValue<int>();

            if (tvDelay < tvDelay1) return tvDelay1;
            return tvDelay;
        }

        public async Task UploadDemoAsync(string? demoPath, long matchId, int mapNumber)
        {
            if (demoPath == null || demoUploadURL == "")
            {
                Log($"[UploadDemoAsync] Not able to upload demo, either demoPath or demoUploadURL is not set. demoPath: {demoPath} demoUploadURL: {demoUploadURL}");
            }

            try
            {
                using var httpClient = new HttpClient();
                Log($"[UploadDemoAsync] Going to upload demo on {demoUploadURL}. Complete path: {demoPath}");

                if (!File.Exists(demoPath))
                {
                    Log($"[UploadDemoAsync ERROR] File not found: {demoPath}");
                    return;
                }

                using FileStream fileStream = File.OpenRead(demoPath);

                byte[] fileContent = new byte[fileStream.Length];
                await fileStream.ReadAsync(fileContent, 0, (int)fileStream.Length);

                using ByteArrayContent content = new ByteArrayContent(fileContent);
                content.Headers.Add("Content-Type", "application/octet-stream");

                content.Headers.Add("MatchZy-FileName", Path.GetFileName(demoPath));
                content.Headers.Add("MatchZy-MatchId", matchId.ToString());
                content.Headers.Add("MatchZy-MapNumber", mapNumber.ToString());

                // For Get5 Panel
                content.Headers.Add("Get5-FileName", Path.GetFileName(demoPath));
                content.Headers.Add("Get5-MatchId", matchId.ToString());
                content.Headers.Add("Get5-MapNumber", mapNumber.ToString());

                if (!string.IsNullOrEmpty(demoUploadHeaderKey))
                {
                    httpClient.DefaultRequestHeaders.Add(demoUploadHeaderKey, demoUploadHeaderValue);
                }

                HttpResponseMessage response = await httpClient.PostAsync(demoUploadURL, content);

                if (response.IsSuccessStatusCode)
                {
                    Log($"[UploadDemoAsync] File upload successful for matchId: {matchId} mapNumber: {mapNumber} fileName: {Path.GetFileName(demoPath)}.");
                }
                else
                {
                    Log($"[UploadDemoAsync ERROR] Failed to upload file. Status code: {response.StatusCode} Response: {await response.Content.ReadAsStringAsync()}");
                }
            }
            catch (Exception e)
            {
                Log($"[UploadDemoAsync FATAL] An error occurred: {e.Message}");
            }
        }


        private string FormatDemoName()
        {
            string formattedTime = DateTime.Now.ToString("yyyy-MM-dd HH-mm-ss");

            var demoName = demoNameFormat
                .Replace("{TIME}", formattedTime)
                .Replace("{MATCH_ID}", $"{liveMatchId}")
                .Replace("{MAP}", Server.MapName)
                .Replace("{MAPNUMBER}", matchConfig.CurrentMapNumber.ToString())
                .Replace("{TEAM1}", matchzyTeam1.teamName)
                .Replace("{TEAM2}", matchzyTeam2.teamName)
                .Replace(" ", "_");
            return $"{demoName}.dem";
        }

        [ConsoleCommand("get5_demo_upload_header_key", "If defined, a custom HTTP header with this name is added to the HTTP requests for demos")]
        [ConsoleCommand("matchzy_demo_upload_header_key", "If defined, a custom HTTP header with this name is added to the HTTP requests for demos")]
        public void DemoUploadHeaderKeyCommand(CCSPlayerController? player, CommandInfo command)
        {
            if (player != null) return;
            string header = command.ArgByIndex(1).Trim();

            if (header != "") demoUploadHeaderKey = header;
        }

        [ConsoleCommand("get5_demo_upload_header_value", "If defined, the value of the custom header added to the demos sent over HTTP")]
        [ConsoleCommand("matchzy_demo_upload_header_value", "If defined, the value of the custom header added to the demos sent over HTTP")]
        public void DemoUploadHeaderValueCommand(CCSPlayerController? player, CommandInfo command)
        {
            if (player != null) return;
            string headerValue = command.ArgByIndex(1).Trim();

            if (headerValue != "") demoUploadHeaderValue = headerValue;
        }
    }
}
