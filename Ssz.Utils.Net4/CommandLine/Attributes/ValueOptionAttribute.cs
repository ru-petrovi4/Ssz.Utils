﻿#region License

// <copyright file="ValueOptionAttribute.cs" company="Giacomo Stelluti Scala">
//   Copyright 2015-2013 Giacomo Stelluti Scala
// </copyright>
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

#endregion

#region Using Directives

using System;

#endregion

namespace Ssz.Utils.Net4.CommandLine.Attributes
{
    /// <summary>
    ///     Maps a single unnamed option to the target property. Values will be mapped in order of Index.
    ///     This attribute takes precedence over <see cref="ValueListAttribute" /> with which
    ///     can coexist.
    /// </summary>
    /// <remarks>It can handle only scalar values. Do not apply to arrays or lists.</remarks>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public sealed class ValueOptionAttribute : Attribute
    {
        #region construction and destruction

        /// <summary>
        ///     Initializes a new instance of the <see cref="ValueOptionAttribute" /> class.
        /// </summary>
        /// <param name="index">The _index of the option.</param>
        public ValueOptionAttribute(int index)
        {
            _index = index;
        }

        #endregion

        #region public functions

        /// <summary>
        ///     Gets the position this option has on the command line.
        /// </summary>
        public int Index
        {
            get { return _index; }
        }

        #endregion

        #region private fields

        private readonly int _index;

        #endregion
    }
}