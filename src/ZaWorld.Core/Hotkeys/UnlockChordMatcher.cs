namespace ZaWorld.Core.Hotkeys;

public static class UnlockChordMatcher
{
    public static bool MatchesKeyDown(UnlockChord chord, ushort vkCode, bool ctrl, bool alt, bool shift, bool win)
    {
        if (vkCode != chord.VirtualKey)
        {
            return false;
        }

        if (ctrl != chord.Ctrl || alt != chord.Alt || shift != chord.Shift || win != chord.Win)
        {
            return false;
        }

        return true;
    }
}
