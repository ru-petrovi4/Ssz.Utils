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

namespace Ssz.Xceed.Wpf.Toolkit.Media.Animation
{
    public class PennerEquation : IterativeEquation<double>
    {
        #region Private Fields

        private readonly PennerEquationDelegate _pennerImpl;

        #endregion

        #region Constructors

        internal PennerEquation(PennerEquationDelegate pennerImpl)
        {
            _pennerImpl = pennerImpl;
        }

        #endregion

        public override double Evaluate(TimeSpan currentTime, double from, double to, TimeSpan duration)
        {
            var t = currentTime.TotalSeconds;
            var b = from;
            var c = to - from;
            var d = duration.TotalSeconds;

            return _pennerImpl(t, b, c, d);
        }

        #region PennerEquationDelegate Delegate

        internal delegate double PennerEquationDelegate(double t, double b, double c, double d);

        #endregion
    }
}