using System;
using System.Globalization;
using System.Windows.Data;

namespace AnomaliImportTool.WPF.Converters;

public class PercentageConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is double d)
            return d * 100.0;
        return 0;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is double d)
            return d / 100.0;
        return 0.0;
    }
} 