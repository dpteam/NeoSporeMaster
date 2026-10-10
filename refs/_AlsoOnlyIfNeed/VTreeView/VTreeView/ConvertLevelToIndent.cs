using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace VTreeView;

public class ConvertLevelToIndent : IValueConverter
{
	public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
	{
		return new Thickness((int)value * 16, 0.0, 0.0, 0.0);
	}

	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
	{
		throw new NotSupportedException("Not supported - ConvertBack should never be called in a OneWay Binding.");
	}
}
