using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Data;
using System.IO;

namespace QuickZip.IO.PIDL.UserControls
{
    /// <summary>
    /// This class simply converts a Boolean to a Visibility
    /// with an optional invert
    /// </summary>
    [ValueConversion(typeof(FileSystemInfoEx), typeof(string))]
    public class ExToStringConverter : IValueConverter
    {
        //+ virtual AK 

        #region IValueConverter implementation
        /// <summary>
        /// Converts Boolean to Visibility
        /// </summary>
        public virtual object Convert(object value, Type targetType,
            object parameter, CultureInfo culture)
        {
            if (value is FileSystemInfoEx)
                return (value as FileSystemInfoEx).FullName;
            else return "";
        }

        /// <summary>
        /// Convert back, but its not implemented
        /// </summary>
        public virtual object ConvertBack(object value, Type targetType,
            object parameter, CultureInfo culture)
        {
           if (value is string)
                try
                {
                    return FileSystemInfoEx.FromString((string)value);
                }
                catch (FileNotFoundException ex) { MessageBox.Show(ex.Message); }
            return null;
        }
        #endregion
    }
}
