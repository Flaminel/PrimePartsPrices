using System;
using System.Runtime.InteropServices;

namespace PrimePartsPrices.Utils
{
    public static class ProcessWindowHelper
    {
        /// Gets the window handle of the process which is in the foreground
        /// </summary>
        /// <returns>Returns the window handle</returns>
        [DllImport("user32.dll", CharSet = CharSet.Auto, ExactSpelling = true)]
        public static extern IntPtr GetForegroundWindow();

        /// <summary>
        /// Gets the id of the process of the provided handle
        /// </summary>
        /// <param name="handle">The process window handle</param>
        /// <param name="processId">The process id</param>
        /// <returns>Returns the process id</returns>
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern int GetWindowThreadProcessId(IntPtr handle, out int processId);

        /// <summary>
        /// Sets the foreground window
        /// </summary>
        /// <param name="handle">The process window handle</param>
        /// <returns></returns>
        [DllImport("user32.dll", ExactSpelling = true, CharSet = CharSet.Auto)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool SetForegroundWindow(IntPtr handle);

        /// <summary>
        /// Gets the location information of a process window
        /// </summary>
        /// <param name="handle">The process window handle</param>
        /// <param name="location">The location of the window</param>
        /// <returns></returns>
        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GetWindowRect(IntPtr handle, out RECT location);

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int Left { get; set; }
            public int Top { get; set; }
            public int Right { get; set; }
            public int Bottom { get; set; }
        }

        [DllImport("user32.dll")]
        public static extern IntPtr GetDesktopWindow();
        [DllImport("user32.dll")]
        public static extern IntPtr GetWindowDC(IntPtr handle);
        [DllImport("user32.dll")]
        public static extern IntPtr ReleaseDC(IntPtr handle, IntPtr hDC);
    }
}
