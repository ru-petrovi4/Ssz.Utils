﻿/*************************************************************************************

   Extended WPF Toolkit

   Copyright (C) 2007-2013 Xceed Software Inc.

   This program is provided to you under the terms of the Microsoft Public
   License (Ms-PL) as published at http://wpftoolkit.codeplex.com/license 

   For more features, controls, and fast professional support,
   pick up the Plus Edition at http://xceed.com/wpf_toolkit

   Stay informed: follow @datagrid on Twitter or Like http://facebook.com/datagrids

  ***********************************************************************************/

using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Ssz.Xceed.Wpf.Toolkit.Obselete
{
    [Obsolete("Legacy implementation of MaskedTextBox. Use Ssz.Xceed.Wpf.Toolkit.MaskedTextBox instead.", true)]
    public class MaskedTextBox : TextBox
    {
        #region Members

        /// <summary>
        ///     Flags if the Text and Value properties are in the process of being sync'd
        /// </summary>
        private bool _isSyncingTextAndValueProperties;

        private bool _isInitialized;
        private bool _convertExceptionOccurred;

        #endregion //Members

        #region Properties

        protected MaskedTextProvider MaskProvider { get; set; }

        #region IncludePrompt

        public static readonly DependencyProperty IncludePromptProperty = DependencyProperty.Register("IncludePrompt",
            typeof(bool), typeof(MaskedTextBox), new UIPropertyMetadata(false, OnIncludePromptPropertyChanged));

        public bool IncludePrompt
        {
            get => (bool) GetValue(IncludePromptProperty);
            set => SetValue(IncludePromptProperty, value);
        }

        private static void OnIncludePromptPropertyChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            var maskedTextBox = o as MaskedTextBox;
            if (maskedTextBox is not null)
                maskedTextBox.OnIncludePromptChanged((bool) e.OldValue, (bool) e.NewValue);
        }

        protected virtual void OnIncludePromptChanged(bool oldValue, bool newValue)
        {
            UpdateMaskProvider(Mask);
        }

        #endregion //IncludePrompt

        #region IncludeLiterals

        public static readonly DependencyProperty IncludeLiteralsProperty =
            DependencyProperty.Register("IncludeLiterals", typeof(bool), typeof(MaskedTextBox),
                new UIPropertyMetadata(true, OnIncludeLiteralsPropertyChanged));

        public bool IncludeLiterals
        {
            get => (bool) GetValue(IncludeLiteralsProperty);
            set => SetValue(IncludeLiteralsProperty, value);
        }

        private static void OnIncludeLiteralsPropertyChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            var maskedTextBox = o as MaskedTextBox;
            if (maskedTextBox is not null)
                maskedTextBox.OnIncludeLiteralsChanged((bool) e.OldValue, (bool) e.NewValue);
        }

        protected virtual void OnIncludeLiteralsChanged(bool oldValue, bool newValue)
        {
            UpdateMaskProvider(Mask);
        }

        #endregion //IncludeLiterals

        #region Mask

        public static readonly DependencyProperty MaskProperty = DependencyProperty.Register("Mask", typeof(string),
            typeof(MaskedTextBox), new UIPropertyMetadata("<>", OnMaskPropertyChanged));

        public string Mask
        {
            get => (string) GetValue(MaskProperty);
            set => SetValue(MaskProperty, value);
        }

        private static void OnMaskPropertyChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            var maskedTextBox = o as MaskedTextBox;
            if (maskedTextBox is not null)
                maskedTextBox.OnMaskChanged((string) e.OldValue, (string) e.NewValue);
        }

        protected virtual void OnMaskChanged(string oldValue, string newValue)
        {
            UpdateMaskProvider(newValue);
            UpdateText(0);
        }

        #endregion //Mask

        #region PromptChar

        public static readonly DependencyProperty PromptCharProperty = DependencyProperty.Register("PromptChar",
            typeof(char), typeof(MaskedTextBox), new UIPropertyMetadata('_', OnPromptCharChanged));

        public char PromptChar
        {
            get => (char) GetValue(PromptCharProperty);
            set => SetValue(PromptCharProperty, value);
        }

        private static void OnPromptCharChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            var maskedTextBox = o as MaskedTextBox;
            if (maskedTextBox is not null)
                maskedTextBox.OnPromptCharChanged((char) e.OldValue, (char) e.NewValue);
        }

        protected virtual void OnPromptCharChanged(char oldValue, char newValue)
        {
            UpdateMaskProvider(Mask);
        }

        #endregion //PromptChar

        #region SelectAllOnGotFocus

        public static readonly DependencyProperty SelectAllOnGotFocusProperty =
            DependencyProperty.Register("SelectAllOnGotFocus", typeof(bool), typeof(MaskedTextBox),
                new PropertyMetadata(false));

        public bool SelectAllOnGotFocus
        {
            get => (bool) GetValue(SelectAllOnGotFocusProperty);
            set => SetValue(SelectAllOnGotFocusProperty, value);
        }

        #endregion //SelectAllOnGotFocus

        #region Text

        private static void OnTextChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            var inputBase = o as MaskedTextBox;
            if (inputBase is not null)
                inputBase.OnTextChanged((string) e.OldValue, (string) e.NewValue);
        }

        protected virtual void OnTextChanged(string oldValue, string newValue)
        {
            if (_isInitialized)
                SyncTextAndValueProperties(TextProperty, newValue);
        }

        #endregion //Text

        #region Value

        public static readonly DependencyProperty ValueProperty = DependencyProperty.Register("Value", typeof(object),
            typeof(MaskedTextBox),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnValueChanged));

        public object Value
        {
            get => GetValue(ValueProperty);
            set => SetValue(ValueProperty, value);
        }

        private static void OnValueChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            var maskedTextBox = o as MaskedTextBox;
            if (maskedTextBox is not null)
                maskedTextBox.OnValueChanged(e.OldValue, e.NewValue);
        }

        protected virtual void OnValueChanged(object oldValue, object newValue)
        {
            if (_isInitialized)
                SyncTextAndValueProperties(ValueProperty, newValue);

            var args = new RoutedPropertyChangedEventArgs<object>(oldValue, newValue);
            args.RoutedEvent = ValueChangedEvent;
            RaiseEvent(args);
        }

        #endregion //Value

        #region ValueType

        public static readonly DependencyProperty ValueTypeProperty = DependencyProperty.Register("ValueType",
            typeof(Type), typeof(MaskedTextBox), new UIPropertyMetadata(typeof(string), OnValueTypeChanged));

        public Type ValueType
        {
            get => (Type) GetValue(ValueTypeProperty);
            set => SetValue(ValueTypeProperty, value);
        }

        private static void OnValueTypeChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            var maskedTextBox = o as MaskedTextBox;
            if (maskedTextBox is not null)
                maskedTextBox.OnValueTypeChanged((Type) e.OldValue, (Type) e.NewValue);
        }

        protected virtual void OnValueTypeChanged(Type oldValue, Type newValue)
        {
            if (_isInitialized)
                SyncTextAndValueProperties(TextProperty, Text);
        }

        #endregion //ValueType

        #endregion //Properties

        #region Constructors

        static MaskedTextBox()
        {
            TextProperty.OverrideMetadata(typeof(MaskedTextBox), new FrameworkPropertyMetadata(OnTextChanged));
        }

        public MaskedTextBox()
        {
            CommandBindings.Add(new CommandBinding(ApplicationCommands.Paste, Paste)); //handle paste
            CommandBindings.Add(new CommandBinding(ApplicationCommands.Cut, null, CanCut)); //surpress cut
        }

        #endregion //Constructors

        #region Base Class Overrides

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            UpdateMaskProvider(Mask);
            UpdateText(0);
        }

        protected override void OnInitialized(EventArgs e)
        {
            base.OnInitialized(e);

            if (!_isInitialized)
            {
                _isInitialized = true;
                SyncTextAndValueProperties(ValueProperty, Value);
            }
        }

        protected override void OnGotKeyboardFocus(KeyboardFocusChangedEventArgs e)
        {
            if (SelectAllOnGotFocus) SelectAll();

            base.OnGotKeyboardFocus(e);
        }

        protected override void OnPreviewKeyDown(KeyEventArgs e)
        {
            if (!e.Handled) HandlePreviewKeyDown(e);

            base.OnPreviewKeyDown(e);
        }

        protected override void OnPreviewTextInput(TextCompositionEventArgs e)
        {
            if (!e.Handled) HandlePreviewTextInput(e);

            base.OnPreviewTextInput(e);
        }

        #endregion //Base Class Overrides

        #region Events

        public static readonly RoutedEvent ValueChangedEvent = EventManager.RegisterRoutedEvent("ValueChanged",
            RoutingStrategy.Bubble, typeof(RoutedPropertyChangedEventHandler<object>), typeof(MaskedTextBox));

        public event RoutedPropertyChangedEventHandler<object> ValueChanged
        {
            add => AddHandler(ValueChangedEvent, value);
            remove => RemoveHandler(ValueChangedEvent, value);
        }

        #endregion //Events

        #region Methods

        #region Private

        private void UpdateText()
        {
            UpdateText(SelectionStart);
        }

        private void UpdateText(int position)
        {
            var provider = MaskProvider;
            if (provider is null)
                throw new InvalidOperationException();

            Text = provider.ToDisplayString();
            SelectionLength = 0;
            SelectionStart = position;
        }

        private int GetNextCharacterPosition(int startPosition)
        {
            var position = MaskProvider.FindEditPositionFrom(startPosition, true);
            return position == -1 ? startPosition : position;
        }

        private void UpdateMaskProvider(string mask)
        {
            //do not create a mask provider if the Mask is empty, which can occur if the IncludePrompt and IncludeLiterals properties
            //are set prior to the Mask.
            if (string.IsNullOrEmpty(mask))
                return;

            MaskProvider = new MaskedTextProvider(mask)
            {
                IncludePrompt = IncludePrompt,
                IncludeLiterals = IncludeLiterals,
                PromptChar = PromptChar,
                ResetOnSpace = false //should make this a property
            };
        }

        private object ConvertTextToValue(string text)
        {
            object convertedValue = null;

            var dataType = ValueType;

            var valueToConvert = MaskProvider.ToString().Trim();

            try
            {
                if (valueToConvert.GetType() == dataType || dataType.IsInstanceOfType(valueToConvert))
                    convertedValue = valueToConvert;
#if !VS2008
                else if (string.IsNullOrWhiteSpace(valueToConvert))
                    convertedValue = Activator.CreateInstance(dataType);
#else
        else if ( String.IsNullOrEmpty( valueToConvert ) )
        {
          convertedValue = Activator.CreateInstance( dataType );
        }
#endif
                else if (null == convertedValue && valueToConvert is IConvertible)
                    convertedValue = Convert.ChangeType(valueToConvert, dataType);
            }
            catch
            {
                //if an excpetion occurs revert back to original value
                _convertExceptionOccurred = true;
                return Value;
            }

            return convertedValue;
        }

        private string ConvertValueToText(object value)
        {
            if (value is null)
                value = string.Empty;

            if (_convertExceptionOccurred)
            {
                value = Value;
                _convertExceptionOccurred = false;
            }

            //I have only seen this occur while in Blend, but we need it here so the Blend designer doesn't crash.
            if (MaskProvider is null)
                return value.ToString();

            MaskProvider.Set(value.ToString());
            return MaskProvider.ToDisplayString();
        }

        private void SyncTextAndValueProperties(DependencyProperty p, object newValue)
        {
            //prevents recursive syncing properties
            if (_isSyncingTextAndValueProperties)
                return;

            _isSyncingTextAndValueProperties = true;

            //this only occures when the user typed in the value
            if (TextProperty == p)
                if (newValue is not null)
                    SetValue(ValueProperty, ConvertTextToValue(newValue.ToString()));

            SetValue(TextProperty, ConvertValueToText(newValue));

            _isSyncingTextAndValueProperties = false;
        }

        private void HandlePreviewTextInput(TextCompositionEventArgs e)
        {
            if (!IsReadOnly) InsertText(e.Text);

            e.Handled = true;
        }

        private void HandlePreviewKeyDown(KeyEventArgs e)
        {
            if (e.Key == Key.Delete)
            {
                e.Handled = IsReadOnly
                            || HandleKeyDownDelete();
            }
            else if (e.Key == Key.Back)
            {
                e.Handled = IsReadOnly
                            || HandleKeyDownBack();
            }
            else if (e.Key == Key.Space)
            {
                if (!IsReadOnly) InsertText(" ");

                e.Handled = true;
            }
            else if (e.Key == Key.Return || e.Key == Key.Enter)
            {
                if (!IsReadOnly && AcceptsReturn) InsertText("\r");

                // We don't want the OnPreviewTextInput to be triggered for the Return/Enter key
                // when it is not accepted.
                e.Handled = true;
            }
            else if (e.Key == Key.Escape)
            {
                // We don't want the OnPreviewTextInput to be triggered at all for the Escape key.
                e.Handled = true;
            }
            else if (e.Key == Key.Tab)
            {
                if (AcceptsTab)
                {
                    if (!IsReadOnly) InsertText("\t");

                    e.Handled = true;
                }
            }
        }

        private bool HandleKeyDownDelete()
        {
            var modifiers = Keyboard.Modifiers;
            var handled = true;

            if (modifiers == ModifierKeys.None)
            {
                if (!RemoveSelectedText())
                {
                    var position = SelectionStart;

                    if (position < Text.Length)
                    {
                        RemoveText(position, 1);
                        UpdateText(position);
                    }
                }
                else
                {
                    UpdateText();
                }
            }
            else if (modifiers == ModifierKeys.Control)
            {
                if (!RemoveSelectedText())
                {
                    var position = SelectionStart;

                    RemoveTextToEnd(position);
                    UpdateText(position);
                }
                else
                {
                    UpdateText();
                }
            }
            else if (modifiers == ModifierKeys.Shift)
            {
                if (RemoveSelectedText())
                    UpdateText();
                else
                    handled = false;
            }
            else
            {
                handled = false;
            }

            return handled;
        }

        private bool HandleKeyDownBack()
        {
            var modifiers = Keyboard.Modifiers;
            var handled = true;

            if (modifiers == ModifierKeys.None || modifiers == ModifierKeys.Shift)
            {
                if (!RemoveSelectedText())
                {
                    var position = SelectionStart;

                    if (position > 0)
                    {
                        var newPosition = position - 1;

                        RemoveText(newPosition, 1);
                        UpdateText(newPosition);
                    }
                }
                else
                {
                    UpdateText();
                }
            }
            else if (modifiers == ModifierKeys.Control)
            {
                if (!RemoveSelectedText())
                {
                    RemoveTextFromStart(SelectionStart);
                    UpdateText(0);
                }
                else
                {
                    UpdateText();
                }
            }
            else
            {
                handled = false;
            }

            return handled;
        }

        private void InsertText(string text)
        {
            var position = SelectionStart;
            var provider = MaskProvider;

            var textRemoved = RemoveSelectedText();

            position = GetNextCharacterPosition(position);

            if (!textRemoved && Keyboard.IsKeyToggled(Key.Insert))
            {
                if (provider.Replace(text, position)) position += text.Length;
            }
            else
            {
                if (provider.InsertAt(text, position)) position += text.Length;
            }

            position = GetNextCharacterPosition(position);

            UpdateText(position);
        }

        private void RemoveTextFromStart(int endPosition)
        {
            RemoveText(0, endPosition);
        }

        private void RemoveTextToEnd(int startPosition)
        {
            RemoveText(startPosition, Text.Length - startPosition);
        }

        private void RemoveText(int position, int length)
        {
            if (length == 0)
                return;

            MaskProvider.RemoveAt(position, position + length - 1);
        }

        private bool RemoveSelectedText()
        {
            var length = SelectionLength;

            if (length == 0)
                return false;

            var position = SelectionStart;

            return MaskProvider.RemoveAt(position, position + length - 1);
        }

        #endregion //Private

        #endregion //Methods

        #region Commands

        private void Paste(object sender, RoutedEventArgs e)
        {
            if (IsReadOnly)
                return;

            var data = Clipboard.GetData(DataFormats.Text);
            if (data is not null)
            {
                var text = data.ToString().Trim();
                if (text.Length > 0)
                {
                    var position = SelectionStart;

                    MaskProvider.Set(text);

                    UpdateText(position);
                }
            }
        }

        private void CanCut(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = false;
            e.Handled = true;
        }

        #endregion //Commands
    }
}