namespace MatchZy
{
    // Helpers for round backups. This file must not depend on CounterStrikeSharp so that it can be unit tested (see tests/).
    public static class BackupLogic
    {
        // A Valve round backup (mp_backup_round_file) is a KeyValues file: complete when it has content, its braces balance
        // (ignoring braces inside quoted strings, e.g. player names) and it ends with the closing brace. The file is written at
        // round start, so a copy taken at the same moment can be cut off half way.
        public static bool IsCompleteValveBackup(string? content)
        {
            if (string.IsNullOrWhiteSpace(content)) return false;
            int depth = 0;
            bool opened = false;
            bool inQuotes = false;
            for (int i = 0; i < content.Length; i++)
            {
                char c = content[i];
                if (inQuotes)
                {
                    if (c == '\\') i++; // Escaped character inside a string.
                    else if (c == '"') inQuotes = false;
                    continue;
                }
                if (c == '"') inQuotes = true;
                else if (c == '{') { depth++; opened = true; }
                else if (c == '}')
                {
                    depth--;
                    if (depth < 0) return false;
                }
            }
            return opened && depth == 0 && !inQuotes && content.TrimEnd().EndsWith('}');
        }

        // Get5 (RestoreFromBackup): pause counts stored in a backup are only used when the backup is for another match or map, or
        // the match is not live (e.g. after a server restart). Restoring a round of the live map keeps the current counts, so
        // a team does not get its pauses back.
        public static bool IsForDifferentMatch(bool isLive, long currentMatchId, int currentMapNumber, string currentMap,
            string? backupMatchId, string? backupMapNumber, string? backupMap)
        {
            return !isLive
                || backupMapNumber != currentMapNumber.ToString()
                || !string.Equals(backupMap, currentMap, StringComparison.OrdinalIgnoreCase)
                || backupMatchId != currentMatchId.ToString();
        }

        // A count stored in a backup; missing or invalid values (e.g. backups from older versions) are 0.
        public static int ParseCount(string? value)
        {
            return int.TryParse(value, out int count) && count > 0 ? count : 0;
        }
    }
}
