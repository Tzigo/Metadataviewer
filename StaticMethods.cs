using Microsoft.Win32;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Security.Principal;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Metadataviewer
{
    static class StaticMethods
    {
        private const string RegistryKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";

        private const string RegistryValueName = "AppsUseLightTheme";

        public enum WindowsTheme
        {
            Light,  //entspricht 0
            Dark    //entspricht 1
        }

        private static WindowsTheme GetWindowsTheme()
        {
            try
            {
                var currentUser = WindowsIdentity.GetCurrent();
                string query = string.Format(
                    CultureInfo.InvariantCulture,
                    @"SELECT * FROM RegistryValueChangeEvent WHERE Hive = 'HKEY_USERS' AND KeyPath = '{0}\\{1}' AND ValueName = '{2}'",
                    currentUser.User.Value,
                    RegistryKeyPath.Replace(@"\", @"\\"),
                    RegistryValueName);

                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(RegistryKeyPath))
                {
                    object registryValueObject = key?.GetValue(RegistryValueName);
                    if (registryValueObject == null) { return WindowsTheme.Light; }

                    int registryValue = (int)registryValueObject;
                    return registryValue > 0 ? WindowsTheme.Light : WindowsTheme.Dark;
                }
            }
            catch { return WindowsTheme.Light; }
        }

        // userset 0 = kein userset (versuche windowstheme zu verwenden), userset 1 = theme dark, userset 2 = theme light
        public static ColorTheme GetColorTheme(int userset)
        {
            if (userset == 0)
            {
                if (GetWindowsTheme() == WindowsTheme.Dark)
                {
                    return new DarkTheme();
                }
                else { return new LightTheme(); }
            }
            if (userset == 1) { return new DarkTheme(); }
            else { return new LightTheme(); }
        }
    }

    public class ColorTheme
    {
        public readonly SolidColorBrush BackGround;
        public readonly SolidColorBrush ForeGround;
        public readonly SolidColorBrush Decorations;

        public ColorTheme(SolidColorBrush BackGround, SolidColorBrush ForeGround, SolidColorBrush Decorations)
        {
            this.BackGround = BackGround;
            this.ForeGround = ForeGround;
            this.Decorations = Decorations;
        }
    }

    internal class LightTheme : ColorTheme
    {
        public LightTheme() : base(
            BackGround: new SolidColorBrush(Colors.White),
            ForeGround: new SolidColorBrush(Colors.Black),
            Decorations: new SolidColorBrush(Colors.Gray)
            )
        { }
    }

    internal class DarkTheme : ColorTheme
    {
        public DarkTheme() : base(
            BackGround: new SolidColorBrush(Colors.Black),
            ForeGround: new SolidColorBrush(Colors.White),
            Decorations: new SolidColorBrush(Colors.Gray)
            )
        { }
    }

}
