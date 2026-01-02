using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;

namespace Triggernometry.Utilities;

public class WindowsUtils {
    [Flags]
    public enum MouseEventFlags : uint {
        LEFTDOWN = 0x00000002,
        LEFTUP = 0x00000004,
        MIDDLEDOWN = 0x00000020,
        MIDDLEUP = 0x00000040,
        RIGHTDOWN = 0x00000008,
        RIGHTUP = 0x00000010,
        //XDOWN = 0x00000080,
        //XUP = 0x00000100,
        //WHEEL = 0x00000800,
        MOVE = 0x00000001,
        ABSOLUTE = 0x00008000
    }

    public enum MouseEventDataXButtons : uint {
        NONE = 0x00000000,
        XBUTTON1 = 0x00000001,
        XBUTTON2 = 0x00000002
    }

    private const uint WM_KEYUP = 0x101;
    private const uint WM_KEYDOWN = 0x100;

    public struct WINDOWPLACEMENT {
        public int length;
        public int flags;
        public int showCmd;
        public Point ptMinPosition;
        public Point ptMaxPosition;
        public Rectangle rcNormalPosition;
    }

    private const int SW_UNKNOWN = -1;
    private const uint SW_HIDE = 0;
    private const uint SW_SHOWNORMAL = 1;
    private const uint SW_NORMAL = 1;
    private const uint SW_SHOWMINIMIZED = 2;
    private const uint SW_SHOWMAXIMIZED = 3;
    private const uint SW_MAXIMIZE = 3;
    private const uint SW_SHOWNOACTIVATE = 4;
    private const uint SW_SHOW = 5;
    private const uint SW_MINIMIZE = 6;
    private const uint SW_SHOWMINNOACTIVE = 7;
    private const uint SW_SHOWNA = 8;
    private const uint SW_RESTORE = 9;

    private const int SM_CXSCREEN = 0x0;
    private const int SM_CYSCREEN = 0x01;

    private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
    private static extern int GetWindowTextLength(IntPtr hWnd);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

    [DllImport("user32.dll")]
    private static extern int GetSystemMetrics(int smIndex);

    [DllImport("user32.dll")]
    private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

    [DllImport("user32.dll")]
    private static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetWindowPlacement(IntPtr hWnd, ref WINDOWPLACEMENT lpwndpl);

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    private static extern void mouse_event(uint dwFlags, int dx, int dy, uint dwData, int dwExtraInfo);

    public static string GetWindowTextFromHandle(IntPtr hWnd) {
        var len = GetWindowTextLength(hWnd);
        if (len > 0) {
            var builder = new StringBuilder(len + 1);
            GetWindowText(hWnd, builder, len + 1);
            return builder.ToString();
        }
        return string.Empty;
    }

    public static void SendMouse(MouseEventFlags flags, MouseEventDataXButtons buttons, int x, int y) {
        if ((flags & MouseEventFlags.ABSOLUTE) == MouseEventFlags.ABSOLUTE) {
            var mx = GetSystemMetrics(SM_CXSCREEN);
            var my = GetSystemMetrics(SM_CYSCREEN);
            x = (int)(65536.0 / mx * x);
            y = (int)(65536.0 / my * y);
        }
        mouse_event((uint)flags, x, y, (uint)buttons, 0);
    }

    public static List<IntPtr> FindWindows(int procid, string titleRegex) {
        if (string.IsNullOrWhiteSpace(titleRegex)) {
            titleRegex = ".*";
        }
        return FindWindows(procid, new Regex(titleRegex));
    }

    public static List<IntPtr> FindWindows(int procid, Regex titleRegex) {
        // find all windows matching the title regex
        var wins = new List<IntPtr>();
        EnumWindows((hWnd, _) => {
                try {
                    var t = GetWindowTextFromHandle(hWnd);
                    var m = titleRegex.Match(t);
                    if (m.Success) {
                        wins.Add(hWnd);
                    }
                }
                catch (Exception) {
                }
                return true;
            }
            , IntPtr.Zero
        );

        // filter by process id
        if (procid < 0) // all windows
        {
            return wins;
        }
        if (procid == 0) // first window
        {
            return wins.Count > 0 ? [wins[0]] : [];
        }
        return wins.Where(win => {
            GetWindowThreadProcessId(win, out var windowProcId);
            return windowProcId == procid;
        }).ToList();
    }

    public static void SendKeycode(int procid, string titleRegex, int keycode) {
        var wins = FindWindows(procid, titleRegex);
        var key = (Keys)keycode;
        var keycodes = new List<int>();

        if (key.HasFlag(Keys.Control))
            keycodes.Add((int)Keys.ControlKey);

        if (key.HasFlag(Keys.Shift))
            keycodes.Add((int)Keys.ShiftKey);

        if (key.HasFlag(Keys.Alt))
            keycodes.Add((int)Keys.Menu);

        keycodes.Add((int)(key & Keys.KeyCode));

        SendKeycodes(procid, titleRegex, keycodes.ToArray());
    }

    public static void SendKeycodes(int procid, string titleRegex, params int[] keycodes) {
        var wins = FindWindows(procid, titleRegex);
        foreach (var keycode in keycodes) {
            foreach (var win in wins) {
                SendMessage(win, WM_KEYDOWN, keycode, IntPtr.Zero);
            }
        }
        Thread.Sleep(10);
        foreach (var keycode in keycodes.Reverse()) {
            foreach (var win in wins) {
                SendMessage(win, WM_KEYUP, keycode, IntPtr.Zero);
            }
        }
    }

    public static void SendMessageToWindow(int procid, string windowtitle, uint code, IntPtr wparam, IntPtr lparam) {
        var wins = FindWindows(procid, windowtitle);
        foreach (var win in wins) {
            SendMessage(win, code, wparam, lparam);
        }
    }

    public static bool IsInFocus(string windowtitle) {
        var hwnd = FindWindow(null, windowtitle);
        var wp = new WINDOWPLACEMENT();
        wp.showCmd = SW_UNKNOWN;
        if (hwnd != IntPtr.Zero) {
            wp.length = Marshal.SizeOf(typeof(WINDOWPLACEMENT));
            if (GetWindowPlacement(hwnd, ref wp)) {
                if (wp.showCmd == SW_SHOWMINIMIZED) {
                    return false;
                }
                var hwnd2 = GetForegroundWindow();
                return hwnd == hwnd2;
            }
        }
        return true;
    }
}