using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace GraphDigitizer.Models
{
    public static class MouseUtils
    {
        [DllImport("user32.dll", EntryPoint = "SetCursorPos")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool SetCursorPos(int x, int y);

        [DllImport("user32.dll", EntryPoint = "GetCursorPos")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GetCursorPos(out Point p);

        [DllImport("user32.dll", EntryPoint = "ClipCursor")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool ClipCursor(ref Rect r);

        public struct Point
        {
            public int X;

            public int Y;
        }

        public struct Rect
        {
            public int Left;
            
            public int Top;

            public int Right;
            
            public int Bottom;
        }
    }
}
