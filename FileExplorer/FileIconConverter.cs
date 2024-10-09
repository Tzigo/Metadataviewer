using System;
using System.Globalization;
using System.Windows.Data;

namespace Metadataviewer
{
    public class FileIconConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length < 2 || !(values[0] is bool) || !(values[1] is string))
            {
                return null;
            }

            bool isDirectory = (bool)values[0];
            string path = values[1].ToString();

            if (isDirectory)
            {
                // Standard-Ordner-Icon abrufen
                return IconHelper.GetIcon(path, false);
            }
            else
            {
                // Standard-Datei-Icon abrufen
                return IconHelper.GetIcon(path, false);
            }
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
