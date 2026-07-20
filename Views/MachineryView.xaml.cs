using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using NavalArchitectureSuite.ViewModels;

namespace NavalArchitectureSuite.Views
{
    public partial class MachineryView : UserControl
    {
        public MachineryView()
        {
            InitializeComponent();
            DataContext = new MachineryViewModel();
        }
    }

    /// <summary>Collapses a Border when the bound value is null; shows it when non-null.</summary>
    public class NullToVisibilityConverter : IValueConverter
    {
        public static readonly NullToVisibilityConverter Instance = new();

        public object Convert(object? value, Type targetType, object parameter, CultureInfo culture) =>
            value is null ? Visibility.Collapsed : Visibility.Visible;

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
            throw new NotSupportedException();
    }

    public class BoolComplianceToBrushConverter : IValueConverter
    {
        private static readonly SolidColorBrush Compliant = new(Color.FromRgb(0x3D, 0xDC, 0x84));
        private static readonly SolidColorBrush NonCompliant = new(Color.FromRgb(0xE0, 0x52, 0x63));

        public object Convert(object? value, Type targetType, object parameter, CultureInfo culture) =>
            value is true ? Compliant : NonCompliant;

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
            throw new NotSupportedException();
    }

    public class NoxComplianceToBrushConverter : IValueConverter
    {
        private static readonly SolidColorBrush Compliant = new(Color.FromRgb(0x3D, 0xDC, 0x84));
        private static readonly SolidColorBrush NonCompliant = new(Color.FromRgb(0xE0, 0x52, 0x63));
        private static readonly SolidColorBrush Neutral = new(Color.FromRgb(0x8F, 0xA6, 0xC9));

        public object Convert(object? value, Type targetType, object parameter, CultureInfo culture) =>
            (value as string) switch
            {
                "COMPLIANT" => Compliant,
                "NON-COMPLIANT" => NonCompliant,
                _ => Neutral
            };

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
            throw new NotSupportedException();
    }
}
