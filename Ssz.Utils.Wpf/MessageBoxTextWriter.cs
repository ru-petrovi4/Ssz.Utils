using Ssz.Utils.Wpf.WpfMessageBox;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Ssz.Utils.Wpf
{
    public class MessageBoxTextWriter : TextWriter
    {
        #region public functions

        public override void Write(string? value)
        {
            WpfMessageBox.WpfMessageBox.Show(value ?? "", "", WpfMessageBoxButton.OK, MessageBoxImage.Information, WpfMessageBoxResult.OK);
        }

        public override Encoding Encoding
        {
            get { return Encoding.Default; }
        }

        #endregion
    }
}
