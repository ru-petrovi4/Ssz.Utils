﻿#region License

// <copyright file="HelpOptionAttribute.cs" company="Giacomo Stelluti Scala">
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
using System.Reflection;
using Ssz.Utils.Net4.CommandLine.Infrastructure;

#endregion

namespace Ssz.Utils.Net4.CommandLine.Attributes
{
    /// <summary>
    ///     Indicates the instance method that must be invoked when it becomes necessary show your help screen.
    ///     The method signature is an instance method with no parameters and <see cref="System.String" />
    ///     return value.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public sealed class HelpOptionAttribute : BaseOptionAttribute
    {
        #region construction and destruction

        /// <summary>
        ///     Initializes a new instance of the <see cref="HelpOptionAttribute" /> class.
        ///     Although it is possible, it is strongly discouraged redefine the long name for this option
        ///     not to disorient your users. It is also recommended not to define a short one.
        /// </summary>
        public HelpOptionAttribute()
            : this("help")
        {
            HelpText = DefaultHelpText;
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="HelpOptionAttribute" /> class
        ///     with the specified short name. Use parameter less constructor instead.
        /// </summary>
        /// <param name="shortName">The short name of the option.</param>
        /// <remarks>
        ///     It's highly not recommended change the way users invoke help. It may create confusion.
        /// </remarks>
        public HelpOptionAttribute(char shortName)
            : base(shortName, null)
        {
            HelpText = DefaultHelpText;
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="HelpOptionAttribute" /> class
        ///     with the specified long name. Use parameter less constructor instead.
        /// </summary>
        /// <param name="longName">The long name of the option or null if not used.</param>
        /// <remarks>
        ///     It's highly not recommended change the way users invoke help. It may create confusion.
        /// </remarks>
        public HelpOptionAttribute(string longName)
            : base(null, longName)
        {
            HelpText = DefaultHelpText;
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="HelpOptionAttribute" /> class.
        ///     Allows you to define short and long option names.
        /// </summary>
        /// <param name="shortName">The short name of the option.</param>
        /// <param name="longName">The long name of the option or null if not used.</param>
        /// <remarks>
        ///     It's highly not recommended change the way users invoke help. It may create confusion.
        /// </remarks>
        public HelpOptionAttribute(char shortName, string longName)
            : base(shortName, longName)
        {
            HelpText = DefaultHelpText;
        }

        #endregion

        #region public functions

        /// <summary>
        ///     Returns always false for this kind of option.
        ///     This behaviour can't be changed by design; if you try set <see cref="HelpOptionAttribute.Required" />
        ///     an <see cref="System.InvalidOperationException" /> will be thrown.
        /// </summary>
        public override bool Required
        {
            get { return false; }
            set { throw new InvalidOperationException(); }
        }

        #endregion

        #region internal functions

        internal static void InvokeMethod(
            object target,
            Pair<MethodInfo, HelpOptionAttribute> pair,
            out string text)
        {
            text = null;
            MethodInfo method = pair.Left;

            if (!CheckMethodSignature(method))
            {
                throw new MemberAccessException();
            }

            text = (string) method.Invoke(target, null);
        }

        #endregion

        #region private functions

        private static bool CheckMethodSignature(MethodInfo value)
        {
            return value.ReturnType == typeof (string) && value.GetParameters().Length == 0;
        }

        #endregion

        #region private fields

        private const string DefaultHelpText = "Display this help screen.";

        #endregion
    }
}