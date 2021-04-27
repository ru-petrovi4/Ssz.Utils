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
using Ssz.Xceed.Wpf.Toolkit.Core;
using Ssz.Xceed.Wpf.Toolkit.Core.Utilities;

namespace Ssz.Xceed.Wpf.Toolkit.Zoombox
{
    [TypeConverter(typeof(ZoomboxViewConverter))]
    public class ZoomboxView
    {
        private ZoomboxView(ZoomboxViewKind viewType)
        {
            _kindHeight = (int) viewType;
        }

        #region Region Property

        public Rect Region
        {
            get
            {
                // a region view has a positive _typeHeight value
                if (_kindHeight < 0)
                    throw new InvalidOperationException(ErrorMessages.GetMessage("RegionOnlyAccessibleOnRegionalView"));

                return new Rect(_x, _y, _scaleWidth, _kindHeight);
            }
            set
            {
                if (ViewKind != ZoomboxViewKind.Region && ViewKind != ZoomboxViewKind.Empty)
                    throw new InvalidOperationException(
                        string.Format(ErrorMessages.GetMessage("ZoomboxViewAlreadyInitialized"), ViewKind.ToString()));

                if (!value.IsEmpty)
                {
                    _x = value.X;
                    _y = value.Y;
                    _scaleWidth = value.Width;
                    _kindHeight = value.Height;
                }
            }
        }

        #endregion

        public override int GetHashCode()
        {
            return _x.GetHashCode() ^ _y.GetHashCode() ^ _scaleWidth.GetHashCode() ^ _kindHeight.GetHashCode();
        }

        public override bool Equals(object o)
        {
            var result = false;
            if (o is ZoomboxView)
            {
                var other = (ZoomboxView) o;
                if (ViewKind == other.ViewKind)
                    switch (ViewKind)
                    {
                        case ZoomboxViewKind.Absolute:
                            result = DoubleHelper.AreVirtuallyEqual(_scaleWidth, other._scaleWidth)
                                     && DoubleHelper.AreVirtuallyEqual(Position, other.Position);
                            break;

                        case ZoomboxViewKind.Region:
                            result = DoubleHelper.AreVirtuallyEqual(Region, other.Region);
                            break;

                        default:
                            result = true;
                            break;
                    }
            }

            return result;
        }

        public override string ToString()
        {
            switch (ViewKind)
            {
                case ZoomboxViewKind.Empty:
                    return "ZoomboxView: Empty";

                case ZoomboxViewKind.Center:
                    return "ZoomboxView: Center";

                case ZoomboxViewKind.Fill:
                    return "ZoomboxView: Fill";

                case ZoomboxViewKind.Fit:
                    return "ZoomboxView: Fit";

                case ZoomboxViewKind.Absolute:
                    return string.Format("ZoomboxView: Scale = {0}; Position = ({1}, {2})", _scaleWidth.ToString("f"),
                        _x.ToString("f"), _y.ToString("f"));

                case ZoomboxViewKind.Region:
                    return string.Format("ZoomboxView: Region = ({0}, {1}, {2}, {3})", _x.ToString("f"),
                        _y.ToString("f"), _scaleWidth.ToString("f"), _kindHeight.ToString("f"));
            }

            return base.ToString();
        }

        #region Constructors

        public ZoomboxView()
        {
        }

        public ZoomboxView(double scale)
        {
            Scale = scale;
        }

        public ZoomboxView(Point position)
        {
            Position = position;
        }

        public ZoomboxView(double scale, Point position)
        {
            Position = position;
            Scale = scale;
        }

        public ZoomboxView(Rect region)
        {
            Region = region;
        }

        public ZoomboxView(double x, double y)
            : this(new Point(x, y))
        {
        }

        public ZoomboxView(double scale, double x, double y)
            : this(scale, new Point(x, y))
        {
        }

        public ZoomboxView(double x, double y, double width, double height)
            : this(new Rect(x, y, width, height))
        {
        }

        #endregion

        #region Empty Static Property

        public static ZoomboxView Empty { get; } = new(ZoomboxViewKind.Empty);

        #endregion

        #region Fill Static Property

        public static ZoomboxView Fill { get; } = new(ZoomboxViewKind.Fill);

        #endregion

        #region Fit Static Property

        public static ZoomboxView Fit { get; } = new(ZoomboxViewKind.Fit);

        #endregion

        #region Center Static Property

        public static ZoomboxView Center { get; } = new(ZoomboxViewKind.Center);

        #endregion

        #region ViewKind Property

        public ZoomboxViewKind ViewKind
        {
            get
            {
                if (_kindHeight > 0)
                    return ZoomboxViewKind.Region;
                return (ZoomboxViewKind) (int) _kindHeight;
            }
        }

        private double _kindHeight = (int) ZoomboxViewKind.Empty;

        #endregion

        #region Position Property

        public Point Position
        {
            get
            {
                if (ViewKind != ZoomboxViewKind.Absolute)
                    throw new InvalidOperationException(ErrorMessages.GetMessage("PositionOnlyAccessibleOnAbsolute"));

                return new Point(_x, _y);
            }
            set
            {
                if (ViewKind != ZoomboxViewKind.Absolute && ViewKind != ZoomboxViewKind.Empty)
                    throw new InvalidOperationException(
                        string.Format(ErrorMessages.GetMessage("ZoomboxViewAlreadyInitialized"), ViewKind.ToString()));

                _x = value.X;
                _y = value.Y;
                _kindHeight = (int) ZoomboxViewKind.Absolute;
            }
        }

        private double _x = double.NaN;
        private double _y = double.NaN;

        #endregion

        #region Scale Property

        public double Scale
        {
            get
            {
                if (ViewKind != ZoomboxViewKind.Absolute)
                    throw new InvalidOperationException(ErrorMessages.GetMessage("ScaleOnlyAccessibleOnAbsolute"));

                return _scaleWidth;
            }
            set
            {
                if (ViewKind != ZoomboxViewKind.Absolute && ViewKind != ZoomboxViewKind.Empty)
                    throw new InvalidOperationException(
                        string.Format(ErrorMessages.GetMessage("ZoomboxViewAlreadyInitialized"), ViewKind.ToString()));

                _scaleWidth = value;
                _kindHeight = (int) ZoomboxViewKind.Absolute;
            }
        }

        private double _scaleWidth = double.NaN;

        #endregion

        #region Operators Methods

        public static bool operator ==(ZoomboxView v1, ZoomboxView v2)
        {
            if ((object) v1 == null)
                return (object) v2 == null;

            if ((object) v2 == null)
                return (object) v1 == null;

            return v1.Equals(v2);
        }

        public static bool operator !=(ZoomboxView v1, ZoomboxView v2)
        {
            return !(v1 == v2);
        }

        #endregion
    }
}