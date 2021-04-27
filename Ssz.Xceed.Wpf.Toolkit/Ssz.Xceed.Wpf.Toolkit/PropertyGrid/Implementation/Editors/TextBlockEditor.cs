/*************************************************************************************

   Extended WPF Toolkit

   Copyright (C) 2007-2013 Xceed Software Inc.

   This program is provided to you under the terms of the Microsoft Public
   License (Ms-PL) as published at http://wpftoolkit.codeplex.com/license 

   For more features, controls, and fast professional support,
   pick up the Plus Edition at http://xceed.com/wpf_toolkit

   Stay informed: follow @datagrid on Twitter or Like http://facebook.com/datagrids

  ***********************************************************************************/

using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Ssz.Xceed.Wpf.Toolkit.PropertyGrid.Editors
{
    public class TextBlockEditor : TypeEditor<TextBox>
    {
        protected override TextBox CreateEditor()
        {
            return new PropertyGridEditorTextBlock();
        }

        protected override void SetValueDependencyProperty()
        {
            ValueProperty = TextBox.TextProperty;
        }

        protected override void SetControlProperties()
        {
            Editor.BorderThickness = new Thickness(0);
            Editor.IsReadOnly = true;
            Editor.Foreground = Brushes.Gray;
        }
    }

    public class PropertyGridEditorTextBlock : TextBox
    {
        static PropertyGridEditorTextBlock()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(PropertyGridEditorTextBlock),
                new FrameworkPropertyMetadata(typeof(PropertyGridEditorTextBlock)));
        }
    }
}