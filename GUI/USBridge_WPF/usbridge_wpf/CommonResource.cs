using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using SharpDX;

namespace USBridge_WPF
{
    #region Data structers
    /// <summary>
    /// Data structure for data point
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct VectorColor
    {
        public VectorColor(Vector3 p, Color4 c)
        {
            Point = p;
            Color = c;
        }

        public Vector3 Point;

        public Color4 Color;

        public const int SIZE_IN_BYTES = (3 + 4) * 4;
    }

    /// <summary>
    /// Data structure for projection matrix
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct Projections
    {
        public Matrix World;

        public Matrix View;

        public Matrix Projection;
    }
    #endregion

    #region Data classes
    public class VoltageRange
    {
        public int Value { get; set; }
        public string Disp { get; set; }
    }
    public class TimeRange
    {
        public int Value { get; set; }
        public string Disp { get; set; }
    }
    #endregion

    #region Value Converters    
    /// <summary>
    /// Used in data plotter to leave margin for the button controls
    /// </summary>
    public class DeductMarginConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (double)value - 30.0;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class LsBtoRangeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            //throw new NotImplementedException();
            var range = (0.192 * Math.Pow(2, (int)(value) + 9));

            if (range >= 3000)
                return (range / 2000).ToString("F") + "mV";

            return (range / 2).ToString("F") + "uV";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    public class DetectionThresholdUnsignedToSigned : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            //throw new NotImplementedException();

            return (short)((short)value - 256);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (short)(System.Convert.ToInt16(value) + 256);
        }
    }
    public class IntanSelectionConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            //throw new NotImplementedException();

            return MainWindow.INTAN[(int)value];
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    #endregion

    public class SwitchModeEventArgs : EventArgs
    {
        public string Status { get; private set; }

        public SwitchModeEventArgs(string status)
        {
            Status = status;
        }
    }
}
