using System.Globalization;

namespace MatchZy
{
    // How saved lineups (savednades.json) store positions and angles: three numbers separated by spaces.
    // This file must not depend on CounterStrikeSharp so that it can be unit tested (see tests/).
    public static class LineupFormat
    {
        // Always with a '.' decimal separator, so the file reads the same on every server locale.
        public static string Format(float x, float y, float z)
        {
            return string.Create(CultureInfo.InvariantCulture, $"{x} {y} {z}");
        }

        // Reads "x y z". Older files written on servers with a ',' decimal separator are still read with the server's locale.
        public static bool TryParse(string? text, out float x, out float y, out float z, CultureInfo? serverCulture = null)
        {
            x = y = z = 0;
            if (string.IsNullOrWhiteSpace(text)) return false;
            string[] parts = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length != 3) return false;
            foreach (CultureInfo culture in new[] { CultureInfo.InvariantCulture, serverCulture ?? CultureInfo.CurrentCulture })
            {
                if (float.TryParse(parts[0], NumberStyles.Float, culture, out x)
                    && float.TryParse(parts[1], NumberStyles.Float, culture, out y)
                    && float.TryParse(parts[2], NumberStyles.Float, culture, out z))
                {
                    return true;
                }
            }
            x = y = z = 0;
            return false;
        }

        // Numbers in an .importnade code ("name x y z pitch yaw roll").
        public static bool IsValidNumber(string text)
        {
            return float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out float value) && float.IsFinite(value);
        }
    }
}
