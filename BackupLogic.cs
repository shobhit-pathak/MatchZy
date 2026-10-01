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
    }
}
