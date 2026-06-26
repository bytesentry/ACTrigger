using System;
using System.Runtime.InteropServices;

namespace ACTrigger.UI.Interop;

public static class WindowsOverlayHelper
{
    private const int GWL_EXSTYLE =
        -20;

    private const int WS_EX_TRANSPARENT =
        0x00000020;

    private const int WS_EX_LAYERED =
        0x00080000;

    [DllImport("user32.dll")]
    private static extern int GetWindowLong(
        IntPtr hWnd,
        int nIndex);

    [DllImport("user32.dll")]
    private static extern int SetWindowLong(
        IntPtr hWnd,
        int nIndex,
        int dwNewLong);


    private const int WS_EX_NOACTIVATE = 0x08000000;

    public static void SetClickThrough(
        IntPtr hwnd,
        bool enabled)
    {
        int style =
            GetWindowLong(
                hwnd,
                GWL_EXSTYLE);

        if (enabled)
        {
            style |=
                WS_EX_TRANSPARENT |
                WS_EX_LAYERED;
        }
        else
        {
            style &=
                ~WS_EX_TRANSPARENT;
        }

        SetWindowLong(
            hwnd,
            GWL_EXSTYLE,
            style);
    }

    private const int WS_EX_TOOLWINDOW = 0x00000080;

    public static void SetDebuffWindowStyle(IntPtr hwnd)
    {
        var exStyle = GetWindowLong(hwnd, GWL_EXSTYLE);
        
        exStyle |= WS_EX_NOACTIVATE;
        exStyle |= WS_EX_TOOLWINDOW;
        exStyle &= ~WS_EX_TRANSPARENT;
        
        SetWindowLong(hwnd, GWL_EXSTYLE, exStyle);
    }
    
    //windows focus prevention

    public static void SetNoActivate(
        IntPtr hwnd)
    {
        var exStyle =
            GetWindowLong(
                hwnd,
                GWL_EXSTYLE);

        SetWindowLong(
            hwnd,
            GWL_EXSTYLE,
            exStyle | WS_EX_NOACTIVATE);
    }
}