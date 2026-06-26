using System;
using System.Runtime.InteropServices;

namespace ACTrigger.UI.Interop;

public static class LinuxOverlayHelper
{
    [DllImport("libX11.so.6")]
    private static extern IntPtr XOpenDisplay(string? display_name);

    [DllImport("libX11.so.6")]
    private static extern void XFlush(IntPtr display); // ADD THIS

    [DllImport("libXfixes.so.3")]
    private static extern IntPtr XFixesCreateRegion(
        IntPtr display,
        XRectangle[] rectangles,
        int n_rectangles);

    [DllImport("libXfixes.so.3")]
    private static extern void XFixesSetWindowShapeRegion(IntPtr display, IntPtr window, int shape_kind, int x_off, int y_off, IntPtr region);

    [DllImport("libXfixes.so.3")]
    private static extern void XFixesDestroyRegion(IntPtr display, IntPtr region);

    private const int ShapeInput = 2;

    [StructLayout(LayoutKind.Sequential)]
    private struct XRectangle
    {
        public short x;
        public short y;
        public ushort width;
        public ushort height;
    }

    public static void SetClickThrough(
        IntPtr xid,
        bool enabled)
    {
        //Console.WriteLine(
        //    $"X11 SetCLICKTHROUGH: {enabled}");
        var display =
            XOpenDisplay(null);

        if (display == IntPtr.Zero)
            return;

        try
        {
            IntPtr region;
            //this is the full window clickthrough original code
            if (enabled)
            {
                // Empty input region
                region =
                    XFixesCreateRegion(
                        display,
                        Array.Empty<XRectangle>(),
                        0);
            }
            else
            {
                // Restore normal input region
                region =
                    IntPtr.Zero;
            }

            
            
            XFixesSetWindowShapeRegion(
                display,
                xid,
                ShapeInput,
                0,
                0,
                region);

            if (region != IntPtr.Zero)
            {
                XFixesDestroyRegion(
                    display,
                    region);
            }

            XFlush(display);
        }
        finally
        {
        }
    }
    public static void SetDebuffInputRegion(
        IntPtr xid,
        bool enabled,
        double scale)
    {
        var display =
            XOpenDisplay(null);

        if (display == IntPtr.Zero)
            return;

        try
        {
            IntPtr region;
            if (enabled)
            {
                var rectangles =
                    new[]
                    {
                        new XRectangle
                        {
                            x = 0,
                            y = 0,
                            width = (ushort)(126 * scale),
                            height = (ushort)(348 * scale)
                        }
                    };

                region =
                    XFixesCreateRegion(
                        display,
                        rectangles,
                        rectangles.Length);

            }
            else
            {
                region =
                    IntPtr.Zero;
            }

            XFixesSetWindowShapeRegion(
                display,
                xid,
                ShapeInput,
                0,
                0,
                region);

            if (region != IntPtr.Zero)
            {
                XFixesDestroyRegion(
                    display,
                    region);
            }

            XFlush(display);
        }
        finally
        {
        }
    }
    [DllImport("libX11.so.6")]
    private static extern int XChangeProperty(IntPtr display, IntPtr window, IntPtr property, IntPtr type, int format, int mode, IntPtr[] data, int nelements);

    [DllImport("libX11.so.6")]
    private static extern int XChangeProperty(IntPtr display, IntPtr window, IntPtr property, IntPtr type, int format, int mode, IntPtr data, int nelements);

    [DllImport("libX11.so.6")]
    private static extern IntPtr XInternAtom(IntPtr display, string atom_name, bool only_if_exists);

    // Prevent overlay activation on X11.
    // Allows clickable controls without stealing focus
    // from the game window.
    public static void SetNoFocus(IntPtr xid)
    {
        var display = XOpenDisplay(null);

        if (display == IntPtr.Zero)
            return;

        try
        {
            var wmHints =
                XInternAtom(
                    display,
                    "WM_HINTS",
                    false);

            long[] hints =
            {
                1, // InputHint
                0, // input = false
                0,0,0,0,0,0,0
            };

            var hintsPtr =
                Marshal.AllocHGlobal(
                    hints.Length * 8);

            try
            {
                Marshal.Copy(
                    hints,
                    0,
                    hintsPtr,
                    hints.Length);

                XChangeProperty(
                    display,
                    xid,
                    wmHints,
                    wmHints,
                    32,
                    0,
                    hintsPtr,
                    9);
            }
            finally
            {
                Marshal.FreeHGlobal(
                    hintsPtr);
            }

            XFlush(display);
        }
        finally
        {
        }
    }
}