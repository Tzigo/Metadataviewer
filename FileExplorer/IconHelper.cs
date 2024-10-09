using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Metadataviewer
{
    public static class IconHelper
    {
        private const int SHGFI_ICON = 0x000000100;      // Flag für das Abrufen eines Icons
        private const int SHGFI_SMALLICON = 0x000000001; // Flag für kleines Icon
        private const int SHGFI_LARGEICON = 0x000000000; // Flag für großes Icon

        [DllImport("shell32.dll")]
        private static extern IntPtr SHGetFileInfo(string pszPath, uint dwFileAttributes, out SHFILEINFO psfi, uint cbFileInfo, uint uFlags);

        [StructLayout(LayoutKind.Sequential)]
        private struct SHFILEINFO
        {
            public IntPtr hIcon;  // Handle zum Icon
            public int iIcon;
            public uint dwAttributes;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
            public string szDisplayName;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)]
            public string szTypeName;
        };

        public static ImageSource GetIcon(string path, bool isLarge)
        {
            // Explizite Umwandlung der Flags in uint
            uint flags = (uint)(SHGFI_ICON | (isLarge ? SHGFI_LARGEICON : SHGFI_SMALLICON));

            SHGetFileInfo(path, 0, out SHFILEINFO shinfo, (uint)Marshal.SizeOf(typeof(SHFILEINFO)), flags);

            // Umwandeln des Icons in ein ImageSource-Objekt
            ImageSource img = Imaging.CreateBitmapSourceFromHIcon(
                shinfo.hIcon,
                Int32Rect.Empty,
                BitmapSizeOptions.FromEmptyOptions());

            // Zerstören des Icon-Handles, um Speicherlecks zu vermeiden
            DestroyIcon(shinfo.hIcon);

            return img;
        }

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool DestroyIcon(IntPtr hIcon);
    }
}
