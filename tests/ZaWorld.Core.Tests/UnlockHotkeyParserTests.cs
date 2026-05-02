using ZaWorld.Core.Hotkeys;

namespace ZaWorld.Core.Tests;

public class UnlockHotkeyParserTests
{
    [Fact]
    public void Parse_DefaultChord()
    {
        var chord = UnlockHotkeyParser.Parse("Ctrl+Alt+Shift+U");
        Assert.True(chord.Ctrl);
        Assert.True(chord.Alt);
        Assert.True(chord.Shift);
        Assert.False(chord.Win);
        Assert.Equal((ushort)0x55, chord.VirtualKey);
    }

    [Fact]
    public void Parse_IsCaseInsensitive_AndTrims()
    {
        var chord = UnlockHotkeyParser.Parse("  ctrl + alt + u  ");
        Assert.True(chord.Ctrl);
        Assert.True(chord.Alt);
        Assert.False(chord.Shift);
        Assert.Equal((ushort)0x55, chord.VirtualKey);
    }

    [Fact]
    public void Parse_F12()
    {
        var chord = UnlockHotkeyParser.Parse("Ctrl+Shift+F12");
        Assert.True(chord.Ctrl);
        Assert.True(chord.Shift);
        Assert.False(chord.Alt);
        Assert.Equal((ushort)0x7B, chord.VirtualKey);
    }

    [Fact]
    public void Parse_Digit5()
    {
        var chord = UnlockHotkeyParser.Parse("Alt+5");
        Assert.True(chord.Alt);
        Assert.Equal((ushort)0x35, chord.VirtualKey);
    }

    [Fact]
    public void Parse_Throws_OnEmpty()
    {
        Assert.Throws<FormatException>(() => UnlockHotkeyParser.Parse(""));
    }

    [Fact]
    public void Parse_Throws_OnMissingKey()
    {
        Assert.Throws<FormatException>(() => UnlockHotkeyParser.Parse("Ctrl+Alt"));
    }
}

public class UnlockChordMatcherTests
{
    [Fact]
    public void Matches_WhenAllRequiredModifiersAndKeyMatch()
    {
        var chord = UnlockHotkeyParser.Parse("Ctrl+Alt+Shift+U");
        Assert.True(UnlockChordMatcher.MatchesKeyDown(chord, 0x55, ctrl: true, alt: true, shift: true, win: false));
    }

    [Fact]
    public void DoesNotMatch_WhenModifierMissing()
    {
        var chord = UnlockHotkeyParser.Parse("Ctrl+Alt+Shift+U");
        Assert.False(UnlockChordMatcher.MatchesKeyDown(chord, 0x55, ctrl: true, alt: true, shift: false, win: false));
    }

    [Fact]
    public void DoesNotMatch_WhenExtraWinHeld()
    {
        var chord = UnlockHotkeyParser.Parse("Ctrl+U");
        Assert.False(UnlockChordMatcher.MatchesKeyDown(chord, 0x55, ctrl: true, alt: false, shift: false, win: true));
    }
}
