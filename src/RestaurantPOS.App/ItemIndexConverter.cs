using System;
using System.Globalization;
using System.Windows.Data;

namespace RestaurantPOS.App;

public class ItemIndexConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values.Length < 2)
        {
            return "";
        }

        var container = values[0] as System.Windows.DependencyObject;
        var list = values[1] as System.Windows.Controls.ItemsControl;
        if (container is null || list is null)
        {
            return "";
        }

        var index = list.ItemContainerGenerator.IndexFromContainer(container);
        return index >= 0 ? index + 1 : "";
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
