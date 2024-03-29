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
using System.Windows;
using Ssz.Xceed.Wpf.Toolkit.Media.Animation;

namespace Ssz.Xceed.Wpf.Toolkit.Panels
{
    public class DoubleAnimator : IterativeAnimator
    {
        #region Private Fields

        private readonly IterativeEquation<double> _equation; //null

        #endregion

        #region Constructors

        public DoubleAnimator(IterativeEquation<double> equation)
        {
            _equation = equation;
        }

        #endregion

        public override Rect GetInitialChildPlacement(UIElement child, Rect currentPlacement,
            Rect targetPlacement, AnimationPanel activeLayout, ref AnimationRate animationRate,
            out object placementArgs, out bool isDone)
        {
            isDone = animationRate.HasSpeed && animationRate.Speed <= 0 ||
                     animationRate.HasDuration && animationRate.Duration.Ticks == 0;
            if (!isDone)
            {
                var startVector = new Vector(currentPlacement.Left + currentPlacement.Width / 2,
                    currentPlacement.Top + currentPlacement.Height / 2);
                var finalVector = new Vector(targetPlacement.Left + targetPlacement.Width / 2,
                    targetPlacement.Top + targetPlacement.Height / 2);
                var distanceVector = startVector - finalVector;
                animationRate = new AnimationRate(animationRate.HasDuration
                    ? animationRate.Duration
                    : TimeSpan.FromMilliseconds(distanceVector.Length / animationRate.Speed));
            }

            placementArgs = currentPlacement;
            return currentPlacement;
        }

        public override Rect GetNextChildPlacement(UIElement child, TimeSpan currentTime,
            Rect currentPlacement, Rect targetPlacement, AnimationPanel activeLayout,
            AnimationRate animationRate, ref object placementArgs, out bool isDone)
        {
            var result = targetPlacement;
            isDone = true;
            if (_equation is not null)
            {
                var from = (Rect) placementArgs;
                var duration = animationRate.Duration;
                isDone = currentTime >= duration;
                if (!isDone)
                {
                    var x = _equation.Evaluate(currentTime, from.Left, targetPlacement.Left, duration);
                    var y = _equation.Evaluate(currentTime, from.Top, targetPlacement.Top, duration);
                    var width = Math.Max(0,
                        _equation.Evaluate(currentTime, from.Width, targetPlacement.Width, duration));
                    var height = Math.Max(0,
                        _equation.Evaluate(currentTime, from.Height, targetPlacement.Height, duration));
                    result = new Rect(x, y, width, height);
                }
            }

            return result;
        }
    }
}