using CounterStrikeSharp.API;


namespace MatchZy
{
    public partial class MatchZy
    {
        public string demoPath = "MatchZy/";

        public void StartDemoRecording()
        {
            string formattedTime = DateTime.Now.ToString("yyyy-MM-dd HH-mm-ss").Replace(" ", "_");
            string demoFileName = $"{formattedTime}_{liveMatchId}_{Server.MapName}_{CT_TEAM_NAME.Replace(" ", "_")}_vs_{T_TEAM_NAME.Replace(" ", "_")}";
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
                Log($"[StartDemoRecoding] Starting demo recording, path: {tempDemoPath}");
                Server.ExecuteCommand($"tv_record {tempDemoPath}");
            }
            catch (Exception ex)
            {
                Log($"[StartDemoRecording - FATAL] Error: {ex.Message}. Starting demo recording with path. Name: {demoFileName}");
                // This is to avoid demo loss in any case of exception
                Server.ExecuteCommand($"tv_record {demoFileName}");
            }

        }

        public void StopDemoRecording()
        {
            Server.ExecuteCommand($"tv_stoprecord");
        }

    }
}
