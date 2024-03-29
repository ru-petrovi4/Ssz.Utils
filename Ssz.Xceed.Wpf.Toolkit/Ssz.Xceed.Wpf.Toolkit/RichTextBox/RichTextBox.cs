﻿/*************************************************************************************

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
using System.Windows.Data;
using System.Windows.Documents;

namespace Ssz.Xceed.Wpf.Toolkit
{
    public class RichTextBox : System.Windows.Controls.RichTextBox
    {
        #region Private Members

        private bool _preventDocumentUpdate;
        private bool _preventTextUpdate;

        #endregion //Private Members

        #region Constructors

        public RichTextBox()
        {
        }

        public RichTextBox(FlowDocument document)
            : base(document)
        {
        }

        #endregion //Constructors

        #region Properties

        #region Text

        public static readonly DependencyProperty TextProperty = DependencyProperty.Register("Text", typeof(string),
            typeof(RichTextBox),
            new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                OnTextPropertyChanged, CoerceTextProperty, true, UpdateSourceTrigger.LostFocus));

        public string Text
        {
            get => (string) GetValue(TextProperty);
            set => SetValue(TextProperty, value);
        }

        private static void OnTextPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((RichTextBox) d).UpdateDocumentFromText();
        }

        private static object CoerceTextProperty(DependencyObject d, object value)
        {
            return value ?? "";
        }

        #endregion //Text

        #region TextFormatter

        public static readonly DependencyProperty TextFormatterProperty = DependencyProperty.Register("TextFormatter",
            typeof(ITextFormatter), typeof(RichTextBox),
            new FrameworkPropertyMetadata(new RtfFormatter(), OnTextFormatterPropertyChanged));

        public ITextFormatter TextFormatter
        {
            get => (ITextFormatter) GetValue(TextFormatterProperty);
            set => SetValue(TextFormatterProperty, value);
        }

        private static void OnTextFormatterPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var richTextBox = d as RichTextBox;
            if (richTextBox is not null)
                richTextBox.OnTextFormatterPropertyChanged((ITextFormatter) e.OldValue, (ITextFormatter) e.NewValue);
        }

        protected virtual void OnTextFormatterPropertyChanged(ITextFormatter oldValue, ITextFormatter newValue)
        {
            UpdateTextFromDocument();
        }

        #endregion //TextFormatter

        #endregion //Properties

        #region Methods

        protected override void OnTextChanged(TextChangedEventArgs e)
        {
            base.OnTextChanged(e);
            UpdateTextFromDocument();
        }

        private void UpdateTextFromDocument()
        {
            if (_preventTextUpdate)
                return;

            _preventDocumentUpdate = true;
            Text = TextFormatter.GetText(Document);
            _preventDocumentUpdate = false;
        }

        private void UpdateDocumentFromText()
        {
            if (_preventDocumentUpdate)
                return;

            _preventTextUpdate = true;
            TextFormatter.SetText(Document, Text);
            _preventTextUpdate = false;
        }

        /// <summary>
        ///     Clears the content of the RichTextBox.
        /// </summary>
        public void Clear()
        {
            Document.Blocks.Clear();
        }

        public override void BeginInit()
        {
            base.BeginInit();
            // Do not update anything while initializing. See EndInit
            _preventTextUpdate = true;
            _preventDocumentUpdate = true;
        }

        public override void EndInit()
        {
            base.EndInit();
            _preventTextUpdate = false;
            _preventDocumentUpdate = false;
            // Possible conflict here if the user specifies 
            // the document AND the text at the same time 
            // in XAML. Text has priority.
            if (!string.IsNullOrEmpty(Text))
                UpdateDocumentFromText();
            else
                UpdateTextFromDocument();
        }

        #endregion //Methods
    }
}