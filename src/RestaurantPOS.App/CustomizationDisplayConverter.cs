using System;
using System.Globalization;
using System.Windows.Data;

namespace RestaurantPOS.App;

public class CustomizationDisplayConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object? parameter, CultureInfo culture)
    {
        var name = values.Length > 0 ? values[0]?.ToString() ?? "" : "";
        var priceCents = values.Length > 1 && values[1] is int cents ? cents : 0;
        if (priceCents == 0)
        {
            return name;
        }

        var priceText = string.Format("${0:0.00}", priceCents / 100.0);
        return $"{name} ({priceText})";
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
