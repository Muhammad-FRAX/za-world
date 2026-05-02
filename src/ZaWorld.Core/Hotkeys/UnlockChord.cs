namespace ZaWorld.Core.Hotkeys;

public sealed record UnlockChord(bool Ctrl, bool Alt, bool Shift, bool Win, ushort VirtualKey);
