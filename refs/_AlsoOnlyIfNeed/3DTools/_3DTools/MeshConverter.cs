using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media.Media3D;

namespace _3DTools;

public abstract class MeshConverter<TargetType> : IValueConverter
{
	public MeshConverter()
	{
	}

	object IValueConverter.Convert(object value, Type targetType, object parameter, CultureInfo culture)
	{
		if (value == null)
		{
			return DependencyProperty.UnsetValue;
		}
		if (targetType != typeof(TargetType))
		{
			throw new ArgumentException($"MeshConverter must target a {typeof(TargetType).Name}");
		}
		if (!(value is MeshGeometry3D mesh))
		{
			throw new ArgumentException("MeshConverter can only convert from a MeshGeometry3D");
		}
		return Convert(mesh, parameter);
	}

	object IValueConverter.ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
	{
		throw new NotImplementedException();
	}

	public abstract object Convert(MeshGeometry3D mesh, object parameter);
}
