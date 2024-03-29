﻿using System;
using System.Globalization;
using System.Windows.Data;

namespace BGLib.WPF.Converters
{
    class AdvertisementTypeToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var byteType = (byte)value;
            return $"0x{byteType:X2}({value})";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
