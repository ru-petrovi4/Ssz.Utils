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
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Media;
using System.Reflection;
using System.Security;
using System.Text;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using Ssz.Xceed.Wpf.Toolkit.Primitives;

namespace Ssz.Xceed.Wpf.Toolkit
{
    public class MaskedTextBox : ValueRangeTextBox
    {
        #region MaskedTextProvider Property

        public MaskedTextProvider MaskedTextProvider
        {
            get
            {
                if (!m_maskIsNull)
                    return m_maskedTextProvider.Clone() as MaskedTextProvider;

                return null;
            }
        }

        #endregion MaskedTextProvider Property

        #region PRIVATE PROPERTIES

        private bool IsOverwriteMode
        {
            get
            {
                if (!m_maskIsNull)
                    switch (InsertKeyMode)
                    {
                        case InsertKeyMode.Default:
                        {
                            return m_insertToggled;
                        }

                        case InsertKeyMode.Insert:
                        {
                            return false;
                        }

                        case InsertKeyMode.Overwrite:
                        {
                            return true;
                        }
                    }

                return false;
            }
        }

        #endregion PRIVATE PROPERTIES

        #region ISupportInitialize

        protected override void OnInitialized(EventArgs e)
        {
            InitializeMaskedTextProvider();

            SetIsMaskCompleted(m_maskedTextProvider.MaskCompleted);
            SetIsMaskFull(m_maskedTextProvider.MaskFull);

            base.OnInitialized(e);
        }

        #endregion ISupportInitialize


        #region VALUE FROM TEXT

        protected override bool QueryValueFromTextCore(string text, out object value)
        {
            var valueDataType = ValueDataType;

            if (valueDataType != null)
                if (m_unhandledLiteralsPositions != null
                    && m_unhandledLiteralsPositions.Count > 0)
                {
                    text = m_maskedTextProvider.ToString(false, false, true, 0, m_maskedTextProvider.Length);

                    for (var i = m_unhandledLiteralsPositions.Count - 1; i >= 0; i--)
                        text = text.Remove(m_unhandledLiteralsPositions[i], 1);
                }

            return base.QueryValueFromTextCore(text, out value);
        }

        #endregion VALUE FROM TEXT

        #region TEXT FROM VALUE

        protected override string QueryTextFromValueCore(object value)
        {
            if (m_valueToStringMethodInfo != null && value != null)
                try
                {
                    var text = (string) m_valueToStringMethodInfo.Invoke(value,
                        new object[] {FormatSpecifier, GetActiveFormatProvider()});
                    return text;
                }
                catch
                {
                }

            return base.QueryTextFromValueCore(value);
        }

        #endregion TEXT FROM VALUE

        #region STATIC MEMBERS

        private static readonly char[] MaskChars =
            {'0', '9', '#', 'L', '?', '&', 'C', 'A', 'a', '.', ',', ':', '/', '$', '<', '>', '|', '\\'};

        private static readonly char DefaultPasswordChar = '\0';

        private static readonly string NullMaskString = "<>";

        private static string GetRawText(MaskedTextProvider provider)
        {
            return provider.ToString(true, false, false, 0, provider.Length);
        }

        public static string GetFormatSpecifierFromMask(string mask, IFormatProvider formatProvider)
        {
            List<int> notUsed;

            return GetFormatSpecifierFromMask(
                mask,
                MaskChars,
                formatProvider,
                true,
                out notUsed);
        }

        private static string GetFormatSpecifierFromMask(
            string mask,
            char[] maskChars,
            IFormatProvider formatProvider,
            bool includeNonSeparatorLiteralsInValue,
            out List<int> unhandledLiteralsPositions)
        {
            unhandledLiteralsPositions = new List<int>();

            var numberFormatInfo = NumberFormatInfo.GetInstance(formatProvider);

            var formatSpecifierBuilder = new StringBuilder(32);

            // Space will be considered as a separator literals and will be included 
            // no matter the value of IncludeNonSeparatorLiteralsInValue.
            var lastCharIsLiteralIdentifier = false;
            var i = 0;
            var j = 0;

            while (i < mask.Length)
            {
                var currentChar = mask[i];

                if (currentChar == '\\' && !lastCharIsLiteralIdentifier)
                {
                    lastCharIsLiteralIdentifier = true;
                }
                else
                {
                    if (lastCharIsLiteralIdentifier || Array.IndexOf(maskChars, currentChar) < 0)
                    {
                        lastCharIsLiteralIdentifier = false;

                        // The currentChar was preceeded by a liteal identifier or is not part of the MaskedTextProvider mask chars.
                        formatSpecifierBuilder.Append('\\');
                        formatSpecifierBuilder.Append(currentChar);

                        if (!includeNonSeparatorLiteralsInValue && currentChar != ' ')
                            unhandledLiteralsPositions.Add(j);

                        j++;
                    }
                    else
                    {
                        // The currentChar is part of the MaskedTextProvider mask chars.  
                        if (currentChar == '0' || currentChar == '9' || currentChar == '#')
                        {
                            formatSpecifierBuilder.Append('0');
                            j++;
                        }
                        else if (currentChar == '.')
                        {
                            formatSpecifierBuilder.Append('.');
                            j += numberFormatInfo.NumberDecimalSeparator.Length;
                        }
                        else if (currentChar == ',')
                        {
                            formatSpecifierBuilder.Append(',');
                            j += numberFormatInfo.NumberGroupSeparator.Length;
                        }
                        else if (currentChar == '$')
                        {
                            var currencySymbol = numberFormatInfo.CurrencySymbol;

                            formatSpecifierBuilder.Append('"');
                            formatSpecifierBuilder.Append(currencySymbol);
                            formatSpecifierBuilder.Append('"');

                            for (var k = 0; k < currencySymbol.Length; k++)
                            {
                                if (!includeNonSeparatorLiteralsInValue)
                                    unhandledLiteralsPositions.Add(j);

                                j++;
                            }
                        }
                        else
                        {
                            formatSpecifierBuilder.Append(currentChar);

                            if (!includeNonSeparatorLiteralsInValue && currentChar != ' ')
                                unhandledLiteralsPositions.Add(j);

                            j++;
                        }
                    }
                }

                i++;
            }

            return formatSpecifierBuilder.ToString();
        }

        #endregion STATIC MEMBERS

        #region CONSTRUCTORS

        static MaskedTextBox()
        {
            TextProperty.OverrideMetadata(typeof(MaskedTextBox),
                new FrameworkPropertyMetadata(
                    null,
                    TextCoerceValueCallback));

            AutomationProperties.AutomationIdProperty.OverrideMetadata(typeof(MaskedTextBox),
                new UIPropertyMetadata("MaskedTextBox"));
        }

        public MaskedTextBox()
        {
            CommandManager.AddPreviewCanExecuteHandler(this, OnPreviewCanExecuteCommands);
            CommandManager.AddPreviewExecutedHandler(this, OnPreviewExecutedCommands);

            CommandBindings.Add(new CommandBinding(ApplicationCommands.Paste, null, CanExecutePaste));
            CommandBindings.Add(new CommandBinding(ApplicationCommands.Cut, null, CanExecuteCut));
            CommandBindings.Add(new CommandBinding(ApplicationCommands.Copy, null, CanExecuteCopy));
            CommandBindings.Add(new CommandBinding(EditingCommands.ToggleInsert, ToggleInsertExecutedCallback));

            CommandBindings.Add(new CommandBinding(EditingCommands.Delete, null, CanExecuteDelete));
            CommandBindings.Add(new CommandBinding(EditingCommands.DeletePreviousWord, null,
                CanExecuteDeletePreviousWord));
            CommandBindings.Add(new CommandBinding(EditingCommands.DeleteNextWord, null, CanExecuteDeleteNextWord));

            CommandBindings.Add(new CommandBinding(EditingCommands.Backspace, null, CanExecuteBackspace));

            DragDrop.AddPreviewQueryContinueDragHandler(this, PreviewQueryContinueDragCallback);
            AllowDrop = false;
        }

        [SuppressMessage("Microsoft.Performance", "CA1820:TestForEmptyStringsUsingStringLength")]
        private void InitializeMaskedTextProvider()
        {
            var preInitializedText = Text;

            var mask = Mask;

            if (mask == string.Empty)
            {
                m_maskedTextProvider = CreateMaskedTextProvider(NullMaskString);
                m_maskIsNull = true;
            }
            else
            {
                m_maskedTextProvider = CreateMaskedTextProvider(mask);
                m_maskIsNull = false;
            }

            if (!m_maskIsNull && preInitializedText != string.Empty)
            {
                var success = m_maskedTextProvider.Add(preInitializedText);

                if (!success && !DesignerProperties.GetIsInDesignMode(this))
                    throw new InvalidOperationException(
                        "An attempt was made to apply a new mask that cannot be applied to the current text.");
            }
        }

        #endregion CONSTRUCTORS

        #region AllowPromptAsInput Property

        public static readonly DependencyProperty AllowPromptAsInputProperty =
            DependencyProperty.Register("AllowPromptAsInput", typeof(bool), typeof(MaskedTextBox),
                new UIPropertyMetadata(
                    true,
                    AllowPromptAsInputPropertyChangedCallback));

        public bool AllowPromptAsInput
        {
            get => (bool) GetValue(AllowPromptAsInputProperty);
            set => SetValue(AllowPromptAsInputProperty, value);
        }

        private static void AllowPromptAsInputPropertyChangedCallback(object sender,
            DependencyPropertyChangedEventArgs e)
        {
            var maskedTextBox = sender as MaskedTextBox;

            if (!maskedTextBox.IsInitialized)
                return;

            if (maskedTextBox.m_maskIsNull)
                return;

            maskedTextBox.m_maskedTextProvider = maskedTextBox.CreateMaskedTextProvider(maskedTextBox.Mask);
        }

        #endregion AllowPromptAsInput Property

        #region ClipboardMaskFormat Property

        public MaskFormat ClipboardMaskFormat
        {
            get => (MaskFormat) GetValue(ClipboardMaskFormatProperty);
            set => SetValue(ClipboardMaskFormatProperty, value);
        }

        public static readonly DependencyProperty ClipboardMaskFormatProperty =
            DependencyProperty.Register("ClipboardMaskFormat", typeof(MaskFormat), typeof(MaskedTextBox),
                new UIPropertyMetadata(MaskFormat.IncludeLiterals));

        #endregion ClipboardMaskFormat Property

        #region HidePromptOnLeave Property

        public bool HidePromptOnLeave
        {
            get => (bool) GetValue(HidePromptOnLeaveProperty);
            set => SetValue(HidePromptOnLeaveProperty, value);
        }

        public static readonly DependencyProperty HidePromptOnLeaveProperty =
            DependencyProperty.Register("HidePromptOnLeave", typeof(bool), typeof(MaskedTextBox),
                new UIPropertyMetadata(false));

        #endregion HidePromptOnLeave Property

        #region IncludeLiteralsInValue Property

        public bool IncludeLiteralsInValue
        {
            get => (bool) GetValue(IncludeLiteralsInValueProperty);
            set => SetValue(IncludeLiteralsInValueProperty, value);
        }

        public static readonly DependencyProperty IncludeLiteralsInValueProperty =
            DependencyProperty.Register("IncludeLiteralsInValue", typeof(bool), typeof(MaskedTextBox),
                new UIPropertyMetadata(
                    true,
                    InlcudeLiteralsInValuePropertyChangedCallback));

        private static void InlcudeLiteralsInValuePropertyChangedCallback(object sender,
            DependencyPropertyChangedEventArgs e)
        {
            var maskedTextBox = sender as MaskedTextBox;

            if (!maskedTextBox.IsInitialized)
                return;

            maskedTextBox.RefreshConversionHelpers();
            maskedTextBox.RefreshValue();
        }

        #endregion IncludeLiteralsInValue Property

        #region IncludePromptInValue Property

        public bool IncludePromptInValue
        {
            get => (bool) GetValue(IncludePromptInValueProperty);
            set => SetValue(IncludePromptInValueProperty, value);
        }

        public static readonly DependencyProperty IncludePromptInValueProperty =
            DependencyProperty.Register("IncludePromptInValue", typeof(bool), typeof(MaskedTextBox),
                new UIPropertyMetadata(
                    false,
                    IncludePromptInValuePropertyChangedCallback));

        private static void IncludePromptInValuePropertyChangedCallback(object sender,
            DependencyPropertyChangedEventArgs e)
        {
            var maskedTextBox = sender as MaskedTextBox;

            if (!maskedTextBox.IsInitialized)
                return;

            maskedTextBox.RefreshValue();
        }

        #endregion IncludePromptInValue Property

        #region InsertKeyMode Property

        public InsertKeyMode InsertKeyMode
        {
            get => (InsertKeyMode) GetValue(InsertKeyModeProperty);
            set => SetValue(InsertKeyModeProperty, value);
        }

        public static readonly DependencyProperty InsertKeyModeProperty =
            DependencyProperty.Register("InsertKeyMode", typeof(InsertKeyMode), typeof(MaskedTextBox),
                new UIPropertyMetadata(InsertKeyMode.Default));

        #endregion InsertKeyMode Property

        #region IsMaskCompleted Read-Only Property

        private static readonly DependencyPropertyKey IsMaskCompletedPropertyKey =
            DependencyProperty.RegisterReadOnly("IsMaskCompleted", typeof(bool), typeof(MaskedTextBox),
                new PropertyMetadata(false));

        public static readonly DependencyProperty IsMaskCompletedProperty =
            IsMaskCompletedPropertyKey.DependencyProperty;


        public bool IsMaskCompleted => (bool) GetValue(IsMaskCompletedProperty);

        private void SetIsMaskCompleted(bool value)
        {
            SetValue(IsMaskCompletedPropertyKey, value);
        }

        #endregion IsMaskCompleted Read-Only Property

        #region IsMaskFull Read-Only Property

        private static readonly DependencyPropertyKey IsMaskFullPropertyKey =
            DependencyProperty.RegisterReadOnly("IsMaskFull", typeof(bool), typeof(MaskedTextBox),
                new PropertyMetadata(false));

        public static readonly DependencyProperty IsMaskFullProperty = IsMaskFullPropertyKey.DependencyProperty;

        public bool IsMaskFull => (bool) GetValue(IsMaskFullProperty);

        private void SetIsMaskFull(bool value)
        {
            SetValue(IsMaskFullPropertyKey, value);
        }

        #endregion IsMaskFull Read-Only Property

        #region Mask Property

        public static readonly DependencyProperty MaskProperty =
            DependencyProperty.Register("Mask", typeof(string), typeof(MaskedTextBox),
                new UIPropertyMetadata(
                    string.Empty,
                    MaskPropertyChangedCallback,
                    MaskCoerceValueCallback));

        public string Mask
        {
            get => (string) GetValue(MaskProperty);
            set => SetValue(MaskProperty, value);
        }

        [SuppressMessage("Microsoft.Usage", "CA2208:InstantiateArgumentExceptionsCorrectly")]
        private static object MaskCoerceValueCallback(DependencyObject sender, object value)
        {
            if (value == null)
                value = string.Empty;

            if (value.Equals(string.Empty))
                return value;

            // Validate the text against the would be new Mask.

            var maskedTextBox = sender as MaskedTextBox;

            if (!maskedTextBox.IsInitialized)
                return value;

            bool valid;

            try
            {
                var provider = maskedTextBox.CreateMaskedTextProvider((string) value);

                var rawText = maskedTextBox.GetRawText();

                valid = provider.VerifyString(rawText);
            }
            catch (Exception exception)
            {
                throw new InvalidOperationException(
                    "An error occured while testing the current text against the new mask.", exception);
            }

            if (!valid)
                throw new ArgumentException("The mask cannot be applied to the current text.", "Mask");

            return value;
        }

        [SuppressMessage("Microsoft.Performance", "CA1820:TestForEmptyStringsUsingStringLength")]
        private static void MaskPropertyChangedCallback(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            var maskedTextBox = sender as MaskedTextBox;

            if (!maskedTextBox.IsInitialized)
                return;

            MaskedTextProvider provider = null;

            var mask = (string) e.NewValue;

            if (mask == string.Empty)
            {
                provider = maskedTextBox.CreateMaskedTextProvider(NullMaskString);
                maskedTextBox.m_maskIsNull = true;
            }
            else
            {
                provider = maskedTextBox.CreateMaskedTextProvider(mask);
                maskedTextBox.m_maskIsNull = false;
            }

            maskedTextBox.m_maskedTextProvider = provider;

            maskedTextBox.RefreshConversionHelpers();

            if (maskedTextBox.ValueDataType != null)
            {
                var textFromValue = maskedTextBox.GetTextFromValue(maskedTextBox.Value);
                maskedTextBox.m_maskedTextProvider.Set(textFromValue);
            }

            maskedTextBox.RefreshCurrentText(true);
        }

        #endregion Mask Property

        #region PromptChar Property

        public static readonly DependencyProperty PromptCharProperty =
            DependencyProperty.Register("PromptChar", typeof(char), typeof(MaskedTextBox),
                new UIPropertyMetadata(
                    '_',
                    PromptCharPropertyChangedCallback,
                    PromptCharCoerceValueCallback));

        public char PromptChar
        {
            get => (char) GetValue(PromptCharProperty);
            set => SetValue(PromptCharProperty, value);
        }

        private static object PromptCharCoerceValueCallback(object sender, object value)
        {
            var maskedTextBox = sender as MaskedTextBox;

            if (!maskedTextBox.IsInitialized)
                return value;

            var provider = maskedTextBox.m_maskedTextProvider.Clone() as MaskedTextProvider;

            try
            {
                provider.PromptChar = (char) value;
            }
            catch (Exception exception)
            {
                throw new ArgumentException("The prompt character is invalid.", exception);
            }

            return value;
        }

        private static void PromptCharPropertyChangedCallback(object sender, DependencyPropertyChangedEventArgs e)
        {
            var maskedTextBox = sender as MaskedTextBox;

            if (!maskedTextBox.IsInitialized)
                return;

            if (maskedTextBox.m_maskIsNull)
                return;

            maskedTextBox.m_maskedTextProvider.PromptChar = (char) e.NewValue;

            maskedTextBox.RefreshCurrentText(true);
        }

        #endregion PromptChar Property

        #region RejectInputOnFirstFailure Property

        public bool RejectInputOnFirstFailure
        {
            get => (bool) GetValue(RejectInputOnFirstFailureProperty);
            set => SetValue(RejectInputOnFirstFailureProperty, value);
        }

        public static readonly DependencyProperty RejectInputOnFirstFailureProperty =
            DependencyProperty.Register("RejectInputOnFirstFailure", typeof(bool), typeof(MaskedTextBox),
                new UIPropertyMetadata(true));

        #endregion RejectInputOnFirstFailure Property

        #region ResetOnPrompt Property

        public bool ResetOnPrompt
        {
            get => (bool) GetValue(ResetOnPromptProperty);
            set => SetValue(ResetOnPromptProperty, value);
        }

        public static readonly DependencyProperty ResetOnPromptProperty =
            DependencyProperty.Register("ResetOnPrompt", typeof(bool), typeof(MaskedTextBox),
                new UIPropertyMetadata(
                    true,
                    ResetOnPromptPropertyChangedCallback));

        private static void ResetOnPromptPropertyChangedCallback(object sender, DependencyPropertyChangedEventArgs e)
        {
            var maskedTextBox = sender as MaskedTextBox;

            if (!maskedTextBox.IsInitialized)
                return;

            if (maskedTextBox.m_maskIsNull)
                return;

            maskedTextBox.m_maskedTextProvider.ResetOnPrompt = (bool) e.NewValue;
        }

        #endregion ResetOnPrompt Property

        #region ResetOnSpace Property

        public bool ResetOnSpace
        {
            get => (bool) GetValue(ResetOnSpaceProperty);
            set => SetValue(ResetOnSpaceProperty, value);
        }

        public static readonly DependencyProperty ResetOnSpaceProperty =
            DependencyProperty.Register("ResetOnSpace", typeof(bool), typeof(MaskedTextBox),
                new UIPropertyMetadata(
                    true,
                    ResetOnSpacePropertyChangedCallback));

        private static void ResetOnSpacePropertyChangedCallback(object sender, DependencyPropertyChangedEventArgs e)
        {
            var maskedTextBox = sender as MaskedTextBox;

            if (!maskedTextBox.IsInitialized)
                return;

            if (maskedTextBox.m_maskIsNull)
                return;

            maskedTextBox.m_maskedTextProvider.ResetOnSpace = (bool) e.NewValue;
        }

        #endregion ResetOnSpace Property

        #region RestrictToAscii Property

        public bool RestrictToAscii
        {
            get => (bool) GetValue(RestrictToAsciiProperty);
            set => SetValue(RestrictToAsciiProperty, value);
        }

        public static readonly DependencyProperty RestrictToAsciiProperty =
            DependencyProperty.Register("RestrictToAscii", typeof(bool), typeof(MaskedTextBox),
                new UIPropertyMetadata(
                    false,
                    RestrictToAsciiPropertyChangedCallback,
                    RestrictToAsciiCoerceValueCallback));

        [SuppressMessage("Microsoft.Usage", "CA2208:InstantiateArgumentExceptionsCorrectly")]
        private static object RestrictToAsciiCoerceValueCallback(object sender, object value)
        {
            var maskedTextBox = sender as MaskedTextBox;

            if (!maskedTextBox.IsInitialized)
                return value;

            if (maskedTextBox.m_maskIsNull)
                return value;

            var restrictToAscii = (bool) value;

            if (!restrictToAscii)
                return value;

            // Validate the text to make sure that it is only made of Ascii characters.

            var provider = maskedTextBox.CreateMaskedTextProvider(
                maskedTextBox.Mask,
                maskedTextBox.GetCultureInfo(),
                maskedTextBox.AllowPromptAsInput,
                maskedTextBox.PromptChar,
                DefaultPasswordChar,
                restrictToAscii);

            if (!provider.VerifyString(maskedTextBox.Text))
                throw new ArgumentException(
                    "The current text cannot be restricted to ASCII characters. The RestrictToAscii property is set to true.",
                    "RestrictToAscii");

            return restrictToAscii;
        }

        private static void RestrictToAsciiPropertyChangedCallback(object sender, DependencyPropertyChangedEventArgs e)
        {
            var maskedTextBox = sender as MaskedTextBox;

            if (!maskedTextBox.IsInitialized)
                return;

            if (maskedTextBox.m_maskIsNull)
                return;

            maskedTextBox.m_maskedTextProvider = maskedTextBox.CreateMaskedTextProvider(maskedTextBox.Mask);

            maskedTextBox.RefreshCurrentText(true);
        }

        #endregion RestrictToAscii Property

        #region SkipLiterals Property

        public bool SkipLiterals
        {
            get => (bool) GetValue(SkipLiteralsProperty);
            set => SetValue(SkipLiteralsProperty, value);
        }

        public static readonly DependencyProperty SkipLiteralsProperty =
            DependencyProperty.Register("SkipLiterals", typeof(bool), typeof(MaskedTextBox),
                new UIPropertyMetadata(
                    true,
                    SkipLiteralsPropertyChangedCallback));

        private static void SkipLiteralsPropertyChangedCallback(object sender, DependencyPropertyChangedEventArgs e)
        {
            var maskedTextBox = sender as MaskedTextBox;

            if (!maskedTextBox.IsInitialized)
                return;

            if (maskedTextBox.m_maskIsNull)
                return;

            maskedTextBox.m_maskedTextProvider.SkipLiterals = (bool) e.NewValue;
        }

        #endregion SkipLiterals Property

        #region Text Property

        private static object TextCoerceValueCallback(DependencyObject sender, object value)
        {
            var maskedTextBox = sender as MaskedTextBox;

            if (!maskedTextBox.IsInitialized)
                return DependencyProperty.UnsetValue;

            if (maskedTextBox.IsInIMEComposition)
                // In IME Composition.  We must return an uncoerced value or else the IME decorator won't disappear after text input.
                return value;

            if (value == null)
                value = string.Empty;

            if (maskedTextBox.IsForcingText || maskedTextBox.m_maskIsNull)
                return value;

            // Only direct affectation to the Text property or binding of the Text property should
            // come through here.  All other cases should pre-validate the text and affect it through the ForceText method.
            var text = maskedTextBox.ValidateText((string) value);

            return text;
        }

        private string ValidateText(string text)
        {
            var coercedText = text;

            if (RejectInputOnFirstFailure)
            {
                var provider = m_maskedTextProvider.Clone() as MaskedTextProvider;

                int notUsed;
                MaskedTextResultHint hint;

                if (provider.Set(text, out notUsed, out hint))
                {
                    coercedText = GetFormattedString(provider);
                }
                else
                {
                    // Coerce the text to remain the same.
                    coercedText = GetFormattedString(m_maskedTextProvider);

                    // The TextPropertyChangedCallback won't be called.
                    // Therefore, we must sync the maskedTextProvider.
                    m_maskedTextProvider.Set(coercedText);
                }
            }
            else
            {
                var provider = (MaskedTextProvider) m_maskedTextProvider.Clone();

                int caretIndex;

                if (CanReplace(provider, text, 0, m_maskedTextProvider.Length, RejectInputOnFirstFailure,
                    out caretIndex))
                {
                    coercedText = GetFormattedString(provider);
                }
                else
                {
                    // Coerce the text to remain the same.
                    coercedText = GetFormattedString(m_maskedTextProvider);

                    // The TextPropertyChangedCallback won't be called.
                    // Therefore, we must sync the maskedTextProvider.
                    m_maskedTextProvider.Set(coercedText);
                }
            }

            return coercedText;
        }

        protected override void OnTextChanged(TextChangedEventArgs e)
        {
            if (!m_maskIsNull)
                if (IsInValueChanged || !IsForcingText)
                {
                    var newText = Text;

                    if (m_maskIsNull)
                    {
                        CaretIndex = newText.Length;
                    }
                    else
                    {
                        m_maskedTextProvider.Set(newText);

                        var caretIndex = m_maskedTextProvider.FindUnassignedEditPositionFrom(0, true);

                        if (caretIndex == -1)
                            caretIndex = m_maskedTextProvider.Length;

                        CaretIndex = caretIndex;
                    }
                }

            // m_maskedTextProvider can be null in the designer. With WPF 3.5 SP1, sometimes, 
            // TextChanged will be triggered before OnInitialized is called.
            if (m_maskedTextProvider != null)
            {
                SetIsMaskCompleted(m_maskedTextProvider.MaskCompleted);
                SetIsMaskFull(m_maskedTextProvider.MaskFull);
            }

            base.OnTextChanged(e);
        }

        #endregion Text Property


        #region COMMANDS

        private void OnPreviewCanExecuteCommands(object sender, CanExecuteRoutedEventArgs e)
        {
            if (m_maskIsNull)
                return;

            var routedUICommand = e.Command as RoutedUICommand;

            if (routedUICommand != null
                && (routedUICommand.Name == "Space" || routedUICommand.Name == "ShiftSpace"))
            {
                if (IsReadOnly)
                {
                    e.CanExecute = false;
                }
                else
                {
                    var provider = (MaskedTextProvider) m_maskedTextProvider.Clone();
                    int caretIndex;
                    e.CanExecute = CanReplace(provider, " ", SelectionStart, SelectionLength, RejectInputOnFirstFailure,
                        out caretIndex);
                }

                e.Handled = true;
            }
            else if (e.Command == ApplicationCommands.Undo || e.Command == ApplicationCommands.Redo)
            {
                e.CanExecute = false;
                e.Handled = true;
            }
        }

        private void OnPreviewExecutedCommands(object sender, ExecutedRoutedEventArgs e)
        {
            if (m_maskIsNull)
                return;

            if (e.Command == EditingCommands.Delete)
            {
                e.Handled = true;
                Delete(SelectionStart, SelectionLength, true);
            }
            else if (e.Command == EditingCommands.DeleteNextWord)
            {
                e.Handled = true;
                EditingCommands.SelectRightByWord.Execute(null, this);
                Delete(SelectionStart, SelectionLength, true);
            }
            else if (e.Command == EditingCommands.DeletePreviousWord)
            {
                e.Handled = true;
                EditingCommands.SelectLeftByWord.Execute(null, this);
                Delete(SelectionStart, SelectionLength, false);
            }
            else if (e.Command == EditingCommands.Backspace)
            {
                e.Handled = true;
                Delete(SelectionStart, SelectionLength, false);
            }
            else if (e.Command == ApplicationCommands.Cut)
            {
                e.Handled = true;

                if (ApplicationCommands.Copy.CanExecute(null, this))
                    ApplicationCommands.Copy.Execute(null, this);

                Delete(SelectionStart, SelectionLength, true);
            }
            else if (e.Command == ApplicationCommands.Copy)
            {
                e.Handled = true;
                ExecuteCopy();
            }
            else if (e.Command == ApplicationCommands.Paste)
            {
                e.Handled = true;

                var clipboardContent = (string) Clipboard.GetDataObject().GetData("System.String");
                Replace(clipboardContent, SelectionStart, SelectionLength);
            }
            else
            {
                var routedUICommand = e.Command as RoutedUICommand;

                if (routedUICommand != null
                    && (routedUICommand.Name == "Space" || routedUICommand.Name == "ShiftSpace"))
                {
                    e.Handled = true;
                    ProcessTextInput(" ");
                }
            }
        }

        private void CanExecuteDelete(object sender, CanExecuteRoutedEventArgs e)
        {
            if (m_maskIsNull)
                return;

            e.CanExecute = CanDelete(SelectionStart, SelectionLength, true,
                MaskedTextProvider.Clone() as MaskedTextProvider);
            e.Handled = true;

            if (!e.CanExecute && BeepOnError)
                SystemSounds.Beep.Play();
        }

        private void CanExecuteDeletePreviousWord(object sender, CanExecuteRoutedEventArgs e)
        {
            if (m_maskIsNull)
                return;

            var canDeletePreviousWord = !IsReadOnly && EditingCommands.SelectLeftByWord.CanExecute(null, this);

            if (canDeletePreviousWord)
            {
                var cachedSelectionStart = SelectionStart;
                var cachedSelectionLength = SelectionLength;

                EditingCommands.SelectLeftByWord.Execute(null, this);

                canDeletePreviousWord = CanDelete(SelectionStart, SelectionLength, false,
                    MaskedTextProvider.Clone() as MaskedTextProvider);

                if (!canDeletePreviousWord)
                {
                    SelectionStart = cachedSelectionStart;
                    SelectionLength = cachedSelectionLength;
                }
            }

            e.CanExecute = canDeletePreviousWord;
            e.Handled = true;

            if (!e.CanExecute && BeepOnError)
                SystemSounds.Beep.Play();
        }

        private void CanExecuteDeleteNextWord(object sender, CanExecuteRoutedEventArgs e)
        {
            if (m_maskIsNull)
                return;

            var canDeleteNextWord = !IsReadOnly && EditingCommands.SelectRightByWord.CanExecute(null, this);

            if (canDeleteNextWord)
            {
                var cachedSelectionStart = SelectionStart;
                var cachedSelectionLength = SelectionLength;

                EditingCommands.SelectRightByWord.Execute(null, this);

                canDeleteNextWord = CanDelete(SelectionStart, SelectionLength, true,
                    MaskedTextProvider.Clone() as MaskedTextProvider);

                if (!canDeleteNextWord)
                {
                    SelectionStart = cachedSelectionStart;
                    SelectionLength = cachedSelectionLength;
                }
            }

            e.CanExecute = canDeleteNextWord;
            e.Handled = true;

            if (!e.CanExecute && BeepOnError)
                SystemSounds.Beep.Play();
        }

        private void CanExecuteBackspace(object sender, CanExecuteRoutedEventArgs e)
        {
            if (m_maskIsNull)
                return;

            e.CanExecute = CanDelete(SelectionStart, SelectionLength, false,
                MaskedTextProvider.Clone() as MaskedTextProvider);
            e.Handled = true;

            if (!e.CanExecute && BeepOnError)
                SystemSounds.Beep.Play();
        }

        private void CanExecuteCut(object sender, CanExecuteRoutedEventArgs e)
        {
            if (m_maskIsNull)
                return;

            var canCut = !IsReadOnly && SelectionLength > 0;

            if (canCut)
            {
                var endPosition = SelectionLength > 0 ? SelectionStart + SelectionLength - 1 : SelectionStart;

                var provider = m_maskedTextProvider.Clone() as MaskedTextProvider;

                canCut = provider.RemoveAt(SelectionStart, endPosition);
            }

            e.CanExecute = canCut;
            e.Handled = true;

            if (!canCut && BeepOnError)
                SystemSounds.Beep.Play();
        }

        private void CanExecutePaste(object sender, CanExecuteRoutedEventArgs e)
        {
            if (m_maskIsNull)
                return;

            var canPaste = false;

            if (!IsReadOnly)
            {
                var clipboardContent = string.Empty;

                try
                {
                    clipboardContent = (string) Clipboard.GetDataObject().GetData("System.String");

                    if (clipboardContent != null)
                    {
                        var provider = (MaskedTextProvider) m_maskedTextProvider.Clone();
                        int caretIndex;
                        canPaste = CanReplace(provider, clipboardContent, SelectionStart, SelectionLength,
                            RejectInputOnFirstFailure, out caretIndex);
                    }
                }
                catch
                {
                }
            }

            e.CanExecute = canPaste;
            e.Handled = true;

            if (!e.CanExecute && BeepOnError)
                SystemSounds.Beep.Play();
        }

        private void CanExecuteCopy(object sender, CanExecuteRoutedEventArgs e)
        {
            if (m_maskIsNull)
                return;

            e.CanExecute = !m_maskedTextProvider.IsPassword;
            e.Handled = true;

            if (!e.CanExecute && BeepOnError)
                SystemSounds.Beep.Play();
        }

        private void ExecuteCopy()
        {
            var selectedText = GetSelectedText();
            try
            {
                // WARNING
                //new UIPermission( UIPermissionClipboard.AllClipboard ).Demand();

                if (selectedText.Length == 0)
                    Clipboard.Clear();
                else
                    Clipboard.SetText(selectedText);
            }
            catch (SecurityException)
            {
            }
        }

        private void ToggleInsertExecutedCallback(object sender, ExecutedRoutedEventArgs e)
        {
            m_insertToggled = !m_insertToggled;
        }

        #endregion COMMANDS

        #region DRAG DROP

        private void PreviewQueryContinueDragCallback(object sender, QueryContinueDragEventArgs e)
        {
            if (m_maskIsNull)
                return;

            e.Action = DragAction.Cancel;
            e.Handled = true;
        }

        protected override void OnDragEnter(DragEventArgs e)
        {
            if (!m_maskIsNull)
            {
                e.Effects = DragDropEffects.None;
                e.Handled = true;
            }

            base.OnDragEnter(e);
        }

        protected override void OnDragOver(DragEventArgs e)
        {
            if (!m_maskIsNull)
            {
                e.Effects = DragDropEffects.None;
                e.Handled = true;
            }

            base.OnDragOver(e);
        }

        #endregion DRAG DROP


        #region PROTECTED METHODS

        protected virtual char[] GetMaskCharacters()
        {
            return MaskChars;
        }

        private MaskedTextProvider CreateMaskedTextProvider(string mask)
        {
            return CreateMaskedTextProvider(
                mask,
                GetCultureInfo(),
                AllowPromptAsInput,
                PromptChar,
                DefaultPasswordChar,
                RestrictToAscii);
        }

        protected virtual MaskedTextProvider CreateMaskedTextProvider(
            string mask,
            CultureInfo cultureInfo,
            bool allowPromptAsInput,
            char promptChar,
            char passwordChar,
            bool restrictToAscii)
        {
            var provider = new MaskedTextProvider(
                mask,
                cultureInfo,
                allowPromptAsInput,
                promptChar,
                passwordChar,
                restrictToAscii);

            provider.ResetOnPrompt = ResetOnPrompt;
            provider.ResetOnSpace = ResetOnSpace;
            provider.SkipLiterals = SkipLiterals;

            provider.IncludeLiterals = true;
            provider.IncludePrompt = true;

            provider.IsPassword = false;

            return provider;
        }

        internal override void OnIMECompositionEnded(CachedTextInfo cachedTextInfo)
        {
            // End of IME Composition.  Restore the critical infos.
            ForceText(cachedTextInfo.Text, false);
            CaretIndex = cachedTextInfo.CaretIndex;
            SelectionStart = cachedTextInfo.SelectionStart;
            SelectionLength = cachedTextInfo.SelectionLength;
        }

        protected override void OnTextInput(TextCompositionEventArgs e)
        {
            if (IsInIMEComposition)
                EndIMEComposition();

            if (m_maskIsNull || m_maskedTextProvider == null || IsReadOnly)
            {
                base.OnTextInput(e);
                return;
            }

            e.Handled = true;

            ProcessTextInput(e.Text);

            base.OnTextInput(e);
        }

        private void ProcessTextInput(string text)
        {
            if (text.Length == 1)
            {
                var textOutput = MaskedTextOutput;

                int caretIndex;
                if (PlaceChar(text[0], SelectionStart, SelectionLength, IsOverwriteMode, out caretIndex))
                {
                    if (MaskedTextOutput != textOutput)
                        RefreshCurrentText(false);

                    SelectionStart = caretIndex + 1;
                }
                else
                {
                    if (BeepOnError)
                        SystemSounds.Beep.Play();
                }

                if (SelectionLength > 0)
                    SelectionLength = 0;
            }
            else
            {
                Replace(text, SelectionStart, SelectionLength);
            }
        }

        protected override void ValidateValue(object value)
        {
            base.ValidateValue(value);

            // Validate if it fits in the mask
            if (!m_maskIsNull)
            {
                var representation = GetTextFromValue(value);

                var provider = m_maskedTextProvider.Clone() as MaskedTextProvider;

                if (!provider.VerifyString(representation))
                    throw new ArgumentException(
                        "The value representation '" + representation + "' does not match the mask.", "value");
            }
        }

        #endregion PROTECTED METHODS


        #region INTERNAL PROPERTIES

        internal bool IsForcingMask { get; private set; }

        internal string FormatSpecifier { get; set; }

        internal override bool IsTextReadyToBeParsed => IsMaskCompleted;

        internal override bool GetIsEditTextEmpty()
        {
            return MaskedTextProvider.AssignedEditPositionCount == 0;
        }

        #endregion INTERNAL PROPERTIES

        #region INTERNAL METHODS

        internal override string GetCurrentText()
        {
            if (m_maskIsNull)
                return base.GetCurrentText();

            var displayText = GetFormattedString(m_maskedTextProvider);

            return displayText;
        }

        internal override string GetParsableText()
        {
            if (m_maskIsNull)
                return base.GetParsableText();

            var includePrompt = false;
            var includeLiterals = true;

            if (ValueDataType == typeof(string))
            {
                includePrompt = IncludePromptInValue;
                includeLiterals = IncludeLiteralsInValue;
            }

            return m_maskedTextProvider
                .ToString(false, includePrompt, includeLiterals, 0, m_maskedTextProvider.Length);
        }

        internal override void OnFormatProviderChanged()
        {
            var provider = new MaskedTextProvider(Mask);

            m_maskedTextProvider = provider;

            RefreshConversionHelpers();
            RefreshCurrentText(true);

            base.OnFormatProviderChanged();
        }

        internal override void RefreshConversionHelpers()
        {
            var type = ValueDataType;

            if (type == null || !IsNumericValueDataType)
            {
                FormatSpecifier = null;
                m_valueToStringMethodInfo = null;
                m_unhandledLiteralsPositions = null;
                return;
            }

            m_valueToStringMethodInfo = type.GetMethod("ToString", new[] {typeof(string), typeof(IFormatProvider)});

            var mask = m_maskedTextProvider.Mask;
            var activeFormatProvider = GetActiveFormatProvider();

            var maskChars = GetMaskCharacters();

            List<int> unhandledLiteralsPositions;

            FormatSpecifier = GetFormatSpecifierFromMask(
                mask,
                maskChars,
                activeFormatProvider,
                IncludeLiteralsInValue,
                out unhandledLiteralsPositions);

            var numberFormatInfo = activeFormatProvider.GetFormat(typeof(NumberFormatInfo)) as NumberFormatInfo;

            if (numberFormatInfo != null)
            {
                var negativeSign = numberFormatInfo.NegativeSign;

                if (FormatSpecifier.Contains(negativeSign))
                {
                    // We must make sure that the value data type is numeric since we are about to 
                    // set the format specifier to its Positive,Negative,Zero format pattern.
                    // If we do not do this, the negative symbol would double itself when IncludeLiteralsInValue
                    // is set to True and a negative symbol is added to the mask as a literal.
                    Debug.Assert(IsNumericValueDataType);

                    FormatSpecifier = FormatSpecifier + ";" + FormatSpecifier + ";" + FormatSpecifier;
                }
            }

            m_unhandledLiteralsPositions = unhandledLiteralsPositions;
        }

        internal void SetValueToStringMethodInfo(MethodInfo valueToStringMethodInfo)
        {
            m_valueToStringMethodInfo = valueToStringMethodInfo;
        }

        internal void ForceMask(string mask)
        {
            IsForcingMask = true;

            try
            {
                Mask = mask;
            }
            finally
            {
                IsForcingMask = false;
            }
        }

        #endregion INTERNAL METHODS

        #region PRIVATE METHODS

        private bool PlaceChar(char ch, int startPosition, int length, bool overwrite, out int caretIndex)
        {
            return PlaceChar(m_maskedTextProvider, ch, startPosition, length, overwrite, out caretIndex);
        }


        private bool PlaceChar(MaskedTextProvider provider, char ch, int startPosition, int length, bool overwrite,
            out int caretPosition)
        {
            if (ShouldQueryAutoCompleteMask(provider.Clone() as MaskedTextProvider, ch, startPosition))
            {
                var e = new AutoCompletingMaskEventArgs(
                    m_maskedTextProvider.Clone() as MaskedTextProvider,
                    startPosition,
                    length,
                    ch.ToString());

                OnAutoCompletingMask(e);

                if (!e.Cancel && e.AutoCompleteStartPosition > -1)
                {
                    caretPosition = startPosition;

                    // AutoComplete the block.
                    for (var i = 0; i < e.AutoCompleteText.Length; i++)
                        if (!PlaceCharCore(provider, e.AutoCompleteText[i], e.AutoCompleteStartPosition + i, 0, true,
                            out caretPosition))
                            return false;

                    caretPosition = e.AutoCompleteStartPosition + e.AutoCompleteText.Length;
                    return true;
                }
            }

            return PlaceCharCore(provider, ch, startPosition, length, overwrite, out caretPosition);
        }

        private bool ShouldQueryAutoCompleteMask(MaskedTextProvider provider, char ch, int startPosition)
        {
            if (provider.IsEditPosition(startPosition))
            {
                var nextSeparatorIndex = provider.FindNonEditPositionFrom(startPosition, true);

                if (nextSeparatorIndex != -1)
                    if (provider[nextSeparatorIndex].Equals(ch))
                    {
                        var previousSeparatorIndex = provider.FindNonEditPositionFrom(startPosition, false);

                        if (provider.FindUnassignedEditPositionInRange(previousSeparatorIndex, nextSeparatorIndex,
                            true) != -1) return true;
                    }
            }

            return false;
        }

        protected virtual void OnAutoCompletingMask(AutoCompletingMaskEventArgs e)
        {
            if (AutoCompletingMask != null)
                AutoCompletingMask(this, e);
        }

        public event EventHandler<AutoCompletingMaskEventArgs> AutoCompletingMask;


        private bool PlaceCharCore(MaskedTextProvider provider, char ch, int startPosition, int length, bool overwrite,
            out int caretPosition)
        {
            caretPosition = startPosition;

            if (startPosition < m_maskedTextProvider.Length)
            {
                MaskedTextResultHint notUsed;

                if (length > 0)
                {
                    var endPosition = startPosition + length - 1;
                    return provider.Replace(ch, startPosition, endPosition, out caretPosition, out notUsed);
                }

                if (overwrite)
                    return provider.Replace(ch, startPosition, out caretPosition, out notUsed);

                return provider.InsertAt(ch, startPosition, out caretPosition, out notUsed);
            }

            return false;
        }

        internal void Replace(string text, int startPosition, int selectionLength)
        {
            var provider = (MaskedTextProvider) m_maskedTextProvider.Clone();
            int tentativeCaretIndex;

            if (CanReplace(provider, text, startPosition, selectionLength, RejectInputOnFirstFailure,
                out tentativeCaretIndex))
            {
                Debug.WriteLine("Replace caret index to: " + tentativeCaretIndex);

                var mustRefreshText = MaskedTextOutput != provider.ToString();
                m_maskedTextProvider = provider;

                if (mustRefreshText)
                    RefreshCurrentText(false);

                CaretIndex = tentativeCaretIndex + 1;
            }
            else
            {
                if (BeepOnError)
                    SystemSounds.Beep.Play();
            }
        }

        internal virtual bool CanReplace(MaskedTextProvider provider, string text, int startPosition,
            int selectionLength, bool rejectInputOnFirstFailure, out int tentativeCaretIndex)
        {
            var endPosition = startPosition + selectionLength - 1;
            tentativeCaretIndex = -1;


            var success = false;

            foreach (var ch in text)
            {
                if (!m_maskedTextProvider.VerifyEscapeChar(ch, startPosition))
                {
                    var editPositionFrom = provider.FindEditPositionFrom(startPosition, true);

                    if (editPositionFrom == MaskedTextProvider.InvalidIndex)
                        break;

                    startPosition = editPositionFrom;
                }

                var length = endPosition >= startPosition ? 1 : 0;
                var overwrite = length > 0;

                if (PlaceChar(provider, ch, startPosition, length, overwrite, out tentativeCaretIndex))
                {
                    // Only one successfully inserted character is enough to declare the replace operation successful.
                    success = true;

                    startPosition = tentativeCaretIndex + 1;
                }
                else if (rejectInputOnFirstFailure)
                {
                    return false;
                }
            }

            if (selectionLength > 0 && startPosition <= endPosition)
            {
                // Erase the remaining of the assigned edit character.
                int notUsed;
                MaskedTextResultHint notUsedHint;
                if (!provider.RemoveAt(startPosition, endPosition, out notUsed, out notUsedHint))
                    success = false;
            }

            return success;
        }

        private bool CanDelete(int startPosition, int selectionLength, bool deleteForward, MaskedTextProvider provider)
        {
            if (IsReadOnly)
                return false;


            if (selectionLength == 0)
            {
                if (!deleteForward)
                {
                    if (startPosition == 0)
                        return false;

                    startPosition--;
                }
                else if (startPosition + selectionLength == provider.Length)
                {
                    return false;
                }
            }

            MaskedTextResultHint notUsed;
            var tentativeCaretPosition = startPosition;

            var endPosition = selectionLength > 0 ? startPosition + selectionLength - 1 : startPosition;

            var success = provider.RemoveAt(startPosition, endPosition, out tentativeCaretPosition, out notUsed);

            return success;
        }

        private void Delete(int startPosition, int selectionLength, bool deleteForward)
        {
            if (IsReadOnly)
                return;


            if (selectionLength == 0)
            {
                if (!deleteForward)
                {
                    if (startPosition == 0)
                        return;

                    startPosition--;
                }
                else if (startPosition + selectionLength == m_maskedTextProvider.Length)
                {
                    return;
                }
            }

            MaskedTextResultHint hint;
            var tentativeCaretPosition = startPosition;

            var endPosition = selectionLength > 0 ? startPosition + selectionLength - 1 : startPosition;

            var oldTextOutput = MaskedTextOutput;

            var success =
                m_maskedTextProvider.RemoveAt(startPosition, endPosition, out tentativeCaretPosition, out hint);

            if (!success)
            {
                if (BeepOnError)
                    SystemSounds.Beep.Play();

                return;
            }

            if (MaskedTextOutput != oldTextOutput)
            {
                RefreshCurrentText(false);
            }
            else if (selectionLength > 0)
            {
                tentativeCaretPosition = startPosition;
            }
            else if (hint == MaskedTextResultHint.NoEffect)
            {
                if (deleteForward)
                {
                    tentativeCaretPosition = m_maskedTextProvider.FindEditPositionFrom(startPosition, true);
                }
                else
                {
                    if (m_maskedTextProvider.FindAssignedEditPositionFrom(startPosition, true) ==
                        MaskedTextProvider.InvalidIndex)
                        tentativeCaretPosition =
                            m_maskedTextProvider.FindAssignedEditPositionFrom(startPosition, false);
                    else
                        tentativeCaretPosition = m_maskedTextProvider.FindEditPositionFrom(startPosition, false);

                    if (tentativeCaretPosition != MaskedTextProvider.InvalidIndex)
                        tentativeCaretPosition++;
                }

                if (tentativeCaretPosition == MaskedTextProvider.InvalidIndex)
                    tentativeCaretPosition = startPosition;
            }
            else if (!deleteForward)
            {
                tentativeCaretPosition = startPosition;
            }

            CaretIndex = tentativeCaretPosition;
        }

        private string MaskedTextOutput
        {
            get
            {
                Debug.Assert(m_maskedTextProvider.EditPositionCount > 0);

                return m_maskedTextProvider.ToString();
            }
        }

        private string GetRawText()
        {
            if (m_maskIsNull)
                return Text;

            return GetRawText(m_maskedTextProvider);
        }

        private string GetFormattedString(MaskedTextProvider provider)
        {
            Debug.Assert(provider.EditPositionCount > 0);


            var includePrompt = IsReadOnly ? false : !HidePromptOnLeave || IsFocused;

            var displayString = provider.ToString(false, includePrompt, true, 0, m_maskedTextProvider.Length);

            return displayString;
        }

        private string GetSelectedText()
        {
            Debug.Assert(!m_maskIsNull);

            var selectionLength = SelectionLength;

            if (selectionLength == 0)
                return string.Empty;

            var includePrompt = (ClipboardMaskFormat & MaskFormat.IncludePrompt) != MaskFormat.ExcludePromptAndLiterals;
            var includeLiterals = (ClipboardMaskFormat & MaskFormat.IncludeLiterals) !=
                                  MaskFormat.ExcludePromptAndLiterals;

            return m_maskedTextProvider.ToString(true, includePrompt, includeLiterals, SelectionStart, selectionLength);
        }

        #endregion PRIVATE METHODS

        #region PRIVATE FIELDS

        private MaskedTextProvider m_maskedTextProvider; // = null;
        private bool m_insertToggled; // = false;
        private bool m_maskIsNull = true;

        private List<int> m_unhandledLiteralsPositions; // = null;
        private MethodInfo m_valueToStringMethodInfo; // = null;

        #endregion PRIVATE FIELDS
    }
}