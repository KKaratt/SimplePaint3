using Avalonia.Data.Converters;
using System;
using System.Globalization;

namespace Figurator.Views.Converters {
    internal class SafeToColor: IValueConverter {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) {
            if (value == null) return null;
            throw new NotSupportedException();
        }
        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) {
            throw new NotImplementedException();
        }
    }
}
