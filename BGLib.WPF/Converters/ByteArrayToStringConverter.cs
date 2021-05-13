using System;
using System.Globalization;
using System.Windows.Data;

namespace BGLib.WPF.Converters
{
    class ByteArrayToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var byteArray = (byte[])value;
            return BitConverter.ToString(byteArray);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
