/*************************************************************************************
   
   Toolkit for WPF

   Copyright (C) 2007-2018 Ssz.Xceed Software Inc.

   This program is provided to you under the terms of the Microsoft Public
   License (Ms-PL) as published at http://wpftoolkit.codeplex.com/license 

   For more features, controls, and fast professional support,
   pick up the Plus Edition at https://xceed.com/xceed-toolkit-plus-for-wpf/

   Stay informed: follow @datagrid on Twitter or Like http://facebook.com/datagrids

  ***********************************************************************************/

using System;
using System.Globalization;
using System.Windows;
using System.Xml;

namespace Ssz.Xceed.Wpf.AvalonDock.Layout
{
    [Serializable]
    public abstract class LayoutPositionableGroup<T> : LayoutGroup<T>, ILayoutPositionableElement,
        ILayoutPositionableElementWithActualSize where T : class, ILayoutElement
    {
        #region Members

        private static GridLengthConverter _gridLengthConverter = new();

        #endregion

        #region Constructors

        #endregion

        #region Properties

        #region DockWidth

        private GridLength _dockWidth = new(1.0, GridUnitType.Star);

        public GridLength DockWidth
        {
            get => _dockWidth;
            set
            {
                if (DockWidth != value)
                {
                    RaisePropertyChanging("DockWidth");
                    _dockWidth = value;
                    RaisePropertyChanged("DockWidth");

                    OnDockWidthChanged();
                }
            }
        }

        #endregion

        #region DockHeight

        private GridLength _dockHeight = new(1.0, GridUnitType.Star);

        public GridLength DockHeight
        {
            get => _dockHeight;
            set
            {
                if (DockHeight != value)
                {
                    RaisePropertyChanging("DockHeight");
                    _dockHeight = value;
                    RaisePropertyChanged("DockHeight");

                    OnDockHeightChanged();
                }
            }
        }

        #endregion

        #region AllowDuplicateContent

        private bool _allowDuplicateContent = true;

        /// <summary>
        ///     Gets or sets the AllowDuplicateContent property.
        ///     When this property is true, then the LayoutDocumentPane or LayoutAnchorablePane allows dropping
        ///     duplicate content (according to its Title and ContentId). When this dependency property is false,
        ///     then the LayoutDocumentPane or LayoutAnchorablePane hides the OverlayWindow.DropInto button to prevent dropping of
        ///     duplicate content.
        /// </summary>
        public bool AllowDuplicateContent
        {
            get => _allowDuplicateContent;
            set
            {
                if (_allowDuplicateContent != value)
                {
                    RaisePropertyChanging("AllowDuplicateContent");
                    _allowDuplicateContent = value;
                    RaisePropertyChanged("AllowDuplicateContent");
                }
            }
        }

        #endregion

        #region CanRepositionItems

        private bool _canRepositionItems = true;

        public bool CanRepositionItems
        {
            get => _canRepositionItems;
            set
            {
                if (_canRepositionItems != value)
                {
                    RaisePropertyChanging("CanRepositionItems");
                    _canRepositionItems = value;
                    RaisePropertyChanged("CanRepositionItems");
                }
            }
        }

        #endregion

        #region DockMinWidth

        private double _dockMinWidth = 25.0;

        public double DockMinWidth
        {
            get => _dockMinWidth;
            set
            {
                if (_dockMinWidth != value)
                {
                    MathHelper.AssertIsPositiveOrZero(value);
                    RaisePropertyChanging("DockMinWidth");
                    _dockMinWidth = value;
                    RaisePropertyChanged("DockMinWidth");
                }
            }
        }

        #endregion

        #region DockMinHeight

        private double _dockMinHeight = 25.0;

        public double DockMinHeight
        {
            get => _dockMinHeight;
            set
            {
                if (_dockMinHeight != value)
                {
                    MathHelper.AssertIsPositiveOrZero(value);
                    RaisePropertyChanging("DockMinHeight");
                    _dockMinHeight = value;
                    RaisePropertyChanged("DockMinHeight");
                }
            }
        }

        #endregion

        #region FloatingWidth

        private double _floatingWidth;

        public double FloatingWidth
        {
            get => _floatingWidth;
            set
            {
                if (_floatingWidth != value)
                {
                    RaisePropertyChanging("FloatingWidth");
                    _floatingWidth = value;
                    RaisePropertyChanged("FloatingWidth");
                }
            }
        }

        #endregion

        #region FloatingHeight

        private double _floatingHeight;

        public double FloatingHeight
        {
            get => _floatingHeight;
            set
            {
                if (_floatingHeight != value)
                {
                    RaisePropertyChanging("FloatingHeight");
                    _floatingHeight = value;
                    RaisePropertyChanged("FloatingHeight");
                }
            }
        }

        #endregion

        #region FloatingLeft

        private double _floatingLeft;

        public double FloatingLeft
        {
            get => _floatingLeft;
            set
            {
                if (_floatingLeft != value)
                {
                    RaisePropertyChanging("FloatingLeft");
                    _floatingLeft = value;
                    RaisePropertyChanged("FloatingLeft");
                }
            }
        }

        #endregion

        #region FloatingTop

        private double _floatingTop;

        public double FloatingTop
        {
            get => _floatingTop;
            set
            {
                if (_floatingTop != value)
                {
                    RaisePropertyChanging("FloatingTop");
                    _floatingTop = value;
                    RaisePropertyChanged("FloatingTop");
                }
            }
        }

        #endregion

        #region IsMaximized

        private bool _isMaximized;

        public bool IsMaximized
        {
            get => _isMaximized;
            set
            {
                if (_isMaximized != value)
                {
                    _isMaximized = value;
                    RaisePropertyChanged("IsMaximized");
                }
            }
        }

        #endregion

        #region ActualWidth

        [NonSerialized] private double _actualWidth;

        double ILayoutPositionableElementWithActualSize.ActualWidth
        {
            get => _actualWidth;
            set => _actualWidth = value;
        }

        #endregion

        #region ActualHeight

        [NonSerialized] private double _actualHeight;

        double ILayoutPositionableElementWithActualSize.ActualHeight
        {
            get => _actualHeight;
            set => _actualHeight = value;
        }

        #endregion

        #endregion

        #region Overrides

        public override void WriteXml(XmlWriter writer)
        {
            if (DockWidth.Value != 1.0 || !DockWidth.IsStar)
                writer.WriteAttributeString("DockWidth", _gridLengthConverter.ConvertToInvariantString(DockWidth));
            if (DockHeight.Value != 1.0 || !DockHeight.IsStar)
                writer.WriteAttributeString("DockHeight", _gridLengthConverter.ConvertToInvariantString(DockHeight));

            if (DockMinWidth != 25.0)
                writer.WriteAttributeString("DockMinWidth", DockMinWidth.ToString(CultureInfo.InvariantCulture));
            if (DockMinHeight != 25.0)
                writer.WriteAttributeString("DockMinHeight", DockMinHeight.ToString(CultureInfo.InvariantCulture));

            if (FloatingWidth != 0.0)
                writer.WriteAttributeString("FloatingWidth", FloatingWidth.ToString(CultureInfo.InvariantCulture));
            if (FloatingHeight != 0.0)
                writer.WriteAttributeString("FloatingHeight", FloatingHeight.ToString(CultureInfo.InvariantCulture));
            if (FloatingLeft != 0.0)
                writer.WriteAttributeString("FloatingLeft", FloatingLeft.ToString(CultureInfo.InvariantCulture));
            if (FloatingTop != 0.0)
                writer.WriteAttributeString("FloatingTop", FloatingTop.ToString(CultureInfo.InvariantCulture));
            if (IsMaximized)
                writer.WriteAttributeString("IsMaximized", IsMaximized.ToString());

            base.WriteXml(writer);
        }


        public override void ReadXml(XmlReader reader)
        {
            if (reader.MoveToAttribute("DockWidth"))
                _dockWidth = (GridLength) _gridLengthConverter.ConvertFromInvariantString(reader.Value);
            if (reader.MoveToAttribute("DockHeight"))
                _dockHeight = (GridLength) _gridLengthConverter.ConvertFromInvariantString(reader.Value);

            if (reader.MoveToAttribute("DockMinWidth"))
                _dockMinWidth = double.Parse(reader.Value, CultureInfo.InvariantCulture);
            if (reader.MoveToAttribute("DockMinHeight"))
                _dockMinHeight = double.Parse(reader.Value, CultureInfo.InvariantCulture);

            if (reader.MoveToAttribute("FloatingWidth"))
                _floatingWidth = double.Parse(reader.Value, CultureInfo.InvariantCulture);
            if (reader.MoveToAttribute("FloatingHeight"))
                _floatingHeight = double.Parse(reader.Value, CultureInfo.InvariantCulture);
            if (reader.MoveToAttribute("FloatingLeft"))
                _floatingLeft = double.Parse(reader.Value, CultureInfo.InvariantCulture);
            if (reader.MoveToAttribute("FloatingTop"))
                _floatingTop = double.Parse(reader.Value, CultureInfo.InvariantCulture);
            if (reader.MoveToAttribute("IsMaximized"))
                _isMaximized = bool.Parse(reader.Value);

            base.ReadXml(reader);
        }

        #endregion

        #region Internal Methods

        protected virtual void OnDockWidthChanged()
        {
        }

        protected virtual void OnDockHeightChanged()
        {
        }

        #endregion
    }
}