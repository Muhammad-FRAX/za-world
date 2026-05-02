using System.Collections.Generic;
using System.Runtime.InteropServices;
using ZaWorld.Core.Hotkeys;

namespace ZaWorld.App.Services;

public sealed class InputLockService : IDisposable
{
    private const int WhKeyboardLl = 13;
    private const int WhMouseLl = 14;
    private const int WmKeydown = 0x0100;
    private const int WmKeyup = 0x0101;
    private const int WmSyskeydown = 0x0104;
    private const int WmSyskeyup = 0x0105;
    /// <summary>WM_MOUSEMOVE — allow through so cursor moves and hover updates.</summary>
    private const int WmMousemove = 0x0200;
    /// <summary>WM_NCMOUSEMOVE — non-client area mouse move.</summary>
    private const int WmNcmousemove = 0x00A0;

    private IntPtr _keyboardHook;
    private IntPtr _mouseHook;
    private UnlockChord? _chord;
    private bool _enabled;
    private DateTime _lastUnlockUtc;
    private HookProc? _keyboardProc;
    private HookProc? _mouseProc;

    /// <summary>
    /// Keys we have seen down via WH_KEYBOARD_LL. Do not use GetAsyncKeyState for unlock:
    /// swallowed input often does not update async key state, so modifiers look "up" and unlock never fires.
    /// </summary>
    private readonly HashSet<uint> _keysDown = new();

    public event Action? UnlockRequested;
    public event Action? IntrusionDetected;

    public void Start(UnlockChord chord)
    {
        Stop();
        _chord = chord;
        _enabled = true;

        var moduleHandle = GetModuleHandle(null);
        _keyboardProc ??= KeyboardHookCallback;
        _mouseProc ??= MouseHookCallback;

        _keyboardHook = SetWindowsHookEx(WhKeyboardLl, _keyboardProc, moduleHandle, 0);
        _mouseHook = SetWindowsHookEx(WhMouseLl, _mouseProc, moduleHandle, 0);
    }

    public void Stop()
    {
        _enabled = false;
        _chord = null;
        _keysDown.Clear();

        if (_keyboardHook != IntPtr.Zero)
        {
            UnhookWindowsHookEx(_keyboardHook);
            _keyboardHook = IntPtr.Zero;
        }

        if (_mouseHook != IntPtr.Zero)
        {
            UnhookWindowsHookEx(_mouseHook);
            _mouseHook = IntPtr.Zero;
        }
    }

    public void Dispose()
    {
        Stop();
    }

    private IntPtr KeyboardHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode < 0)
        {
            return CallNextHookEx(IntPtr.Zero, nCode, wParam, lParam);
        }

        if (!_enabled || _chord is null)
        {
            return CallNextHookEx(IntPtr.Zero, nCode, wParam, lParam);
        }

        var msg = unchecked((int)(nint)wParam);
        if (msg is WmKeydown or WmSyskeydown or WmKeyup or WmSyskeyup)
        {
            var kb = Marshal.PtrToStructure<Kbdllhookstruct>(lParam);
            ApplyKeyboardTransition(msg, kb);

            if (msg is WmKeydown or WmSyskeydown)
            {
                if (IsUnlockChordFromTrackedKeys(kb))
                {
                    MaybeInvokeUnlock();
                }
                else
                {
                    IntrusionDetected?.Invoke();
                }
            }
        }

        return (IntPtr)1;
    }

    private IntPtr MouseHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode < 0)
        {
            return CallNextHookEx(IntPtr.Zero, nCode, wParam, lParam);
        }

        if (!_enabled)
        {
            return CallNextHookEx(IntPtr.Zero, nCode, wParam, lParam);
        }

        // Let cursor move and hover work; block clicks, wheels, and other button actions.
        if (IsMouseMoveOnly(wParam))
        {
            IntrusionDetected?.Invoke();
            return CallNextHookEx(IntPtr.Zero, nCode, wParam, lParam);
        }

        IntrusionDetected?.Invoke();
        return (IntPtr)1;
    }

    private static bool IsMouseMoveOnly(IntPtr wParam)
    {
        var wp = unchecked((int)(nint)wParam);
        return wp is WmMousemove or WmNcmousemove;
    }

    private void ApplyKeyboardTransition(int msg, Kbdllhookstruct kb)
    {
        var vk = kb.VkCode;
        if (msg is WmKeydown or WmSyskeydown)
        {
            _keysDown.Add(vk);
        }
        else if (msg is WmKeyup or WmSyskeyup)
        {
            _keysDown.Remove(vk);
        }
    }

    private bool IsUnlockChordFromTrackedKeys(Kbdllhookstruct kb)
    {
        if (_chord is null)
        {
            return false;
        }

        var ctrl = IsCtrlTracked();
        var alt = IsAltTracked();
        var shift = IsShiftTracked();
        var win = IsWinTracked();

        return UnlockChordMatcher.MatchesKeyDown(_chord, (ushort)kb.VkCode, ctrl, alt, shift, win);
    }

    private bool IsCtrlTracked() =>
        _keysDown.Contains(0x11) || _keysDown.Contains(0xA2) || _keysDown.Contains(0xA3);

    private bool IsShiftTracked() =>
        _keysDown.Contains(0x10) || _keysDown.Contains(0xA0) || _keysDown.Contains(0xA1);

    private bool IsAltTracked() =>
        _keysDown.Contains(0x12) || _keysDown.Contains(0xA4) || _keysDown.Contains(0xA5);

    private bool IsWinTracked() =>
        _keysDown.Contains(0x5B) || _keysDown.Contains(0x5C);

    private void MaybeInvokeUnlock()
    {
        var now = DateTime.UtcNow;
        if ((now - _lastUnlockUtc).TotalMilliseconds < 400)
        {
            return;
        }

        _lastUnlockUtc = now;
        UnlockRequested?.Invoke();
    }

    private delegate IntPtr HookProc(int nCode, IntPtr wParam, IntPtr lParam);

    [StructLayout(LayoutKind.Sequential)]
    private struct Kbdllhookstruct
    {
        public uint VkCode;
        public uint ScanCode;
        public uint Flags;
        public uint Time;
        public UIntPtr DwExtraInfo;
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr SetWindowsHookEx(int idHook, HookProc lpfn, IntPtr hMod, uint dwThreadId);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool UnhookWindowsHookEx(IntPtr hhk);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern IntPtr GetModuleHandle(string? lpModuleName);
}
