using System;
using System.Windows.Controls;

namespace Ssz.Operator.Core.VisualEditors
{
    public static class TextBoxExtensions
    {
        #region public functions

        public static void InsertNewLine(this TextBox textBox)
        {
            var i = textBox.CaretIndex;
            textBox.Text = textBox.Text.Insert(i, Environment.NewLine);
            textBox.CaretIndex = i + 1;
        }

        #endregion
    }
}