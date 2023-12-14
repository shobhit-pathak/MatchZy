using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Modules.Cvars;
using System.IO.Compression;

namespace MatchZy
{
    public partial class MatchZy
    {
        public string demoPath = "MatchZy/";
        public string demoFormat = "{TIME}_{MATCH_ID}_{MAP}_{TEAM1}_{TEAM2}";
        public string demoUploadURL = "";

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
            AddTimer(delay, () => {
                if (isDemoRecording) Server.ExecuteCommand($"tv_stoprecord");
                AddTimer(15, () => {
                    Task.Run(async () => {
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
                using (var httpClient = new HttpClient())
                using (var formData = new MultipartFormDataContent())
                {
                    Log($"[UploadDemoAsync] Going to upload demo on {demoUploadURL}. Complete path: {demoPath}");

                    if (!File.Exists(demoPath))
                    {
                        Log($"[UploadDemoAsync ERROR] File not found: {demoPath}");
                        return;
                    }

                    var compressedFilePath = Path.ChangeExtension(demoPath, "zip"); // Change to ".gz" for GZip compression

                    using (var compressedFileStream = new FileStream(compressedFilePath, FileMode.Create))
                    using (var zipArchive = new ZipArchive(compressedFileStream, ZipArchiveMode.Create))
                    {
                        // Add the .dem file to the zip archive
                        var zipEntry = zipArchive.CreateEntry(Path.GetFileName(demoPath));
                        using (var entryStream = zipEntry.Open())
                        using (var demoFileStream = new FileStream(demoPath, FileMode.Open, FileAccess.Read))
                        {
                            await demoFileStream.CopyToAsync(entryStream);
                        }
                    }

                    var compressedFileStreamContent = new StreamContent(new FileStream(compressedFilePath, FileMode.Open, FileAccess.Read));
                    compressedFileStreamContent.Headers.Add("Content-Type", "application/zip");

                    formData.Add(compressedFileStreamContent, "file", Path.GetFileName(compressedFilePath));

                    formData.Headers.Add("MatchZy-FileName", Path.GetFileName(compressedFilePath));
                    formData.Headers.Add("MatchZy-MatchId", matchId.ToString());
                    formData.Headers.Add("MatchZy-MapNumber", mapNumber.ToString());

                    var response = await httpClient.PostAsync(demoUploadURL, formData).ConfigureAwait(false);

                    if (response.IsSuccessStatusCode)
                    {
                        Log($"[UploadDemoAsync] File upload successful for matchId: {matchId} mapNumber: {mapNumber} fileName: {Path.GetFileName(compressedFilePath)}.");
                    }
                    else
                    {
                        Log($"[UploadDemoAsync ERROR] Failed to upload file. Status code: {response.StatusCode}");
                    }

                    // Clean up: Delete the temporary compressed file
                    File.Delete(compressedFilePath);
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

            var demoName = demoFormat
                .Replace("{TIME}", formattedTime)
                .Replace("{MATCH_ID}", $"{liveMatchId}")
                .Replace("{MAP}", Server.MapName)
                .Replace("{TEAM1}", matchzyTeam1.teamName)
                .Replace("{TEAM2}", matchzyTeam2.teamName)
                .Replace(" ", "_");
            return $"{demoName}.dem";
        }
    }
}
