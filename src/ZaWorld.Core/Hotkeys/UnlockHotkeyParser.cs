using System.Globalization;

namespace ZaWorld.Core.Hotkeys;

public static class UnlockHotkeyParser
{
    public static UnlockChord Parse(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            throw new FormatException("Unlock hotkey cannot be empty.");
        }

        var parts = text.Split('+', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length < 2)
        {
            throw new FormatException("Unlock hotkey must include at least one modifier and a key (example: Ctrl+Alt+U).");
        }

        var ctrl = false;
        var alt = false;
        var shift = false;
        var win = false;

        for (var i = 0; i < parts.Length - 1; i++)
        {
            switch (parts[i].ToLowerInvariant())
            {
                case "ctrl":
                case "control":
                    ctrl = true;
                    break;
                case "alt":
                    alt = true;
                    break;
                case "shift":
                    shift = true;
                    break;
                case "win":
                case "windows":
                    win = true;
                    break;
                default:
                    throw new FormatException($"Unknown modifier '{parts[i]}'.");
            }
        }

        var keyToken = parts[^1];
        var vk = ParseVirtualKey(keyToken);

        return new UnlockChord(ctrl, alt, shift, win, vk);
    }

    private static ushort ParseVirtualKey(string keyToken)
    {
        if (keyToken.Length == 1)
        {
            var c = keyToken[0];
            if (c >= 'a' && c <= 'z')
            {
                return (ushort)char.ToUpperInvariant(c);
            }

            if (c >= 'A' && c <= 'Z')
            {
                return c;
            }

            if (c >= '0' && c <= '9')
            {
                return c;
            }

            throw new FormatException($"Unsupported key '{keyToken}'.");
        }

        if (keyToken.Length >= 2 && char.ToUpperInvariant(keyToken[0]) == 'F')
        {
            if (int.TryParse(keyToken.AsSpan(1), NumberStyles.None, CultureInfo.InvariantCulture, out var n)
                && n >= 1 && n <= 24)
            {
                return (ushort)(0x70 + (n - 1));
            }
        }

        throw new FormatException($"Unsupported key '{keyToken}'.");
    }
}
