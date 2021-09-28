﻿#region License

// <copyright file="ArgumentParser.cs" company="Giacomo Stelluti Scala">
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
using System.Collections.Generic;
using Ssz.Utils.CommandLine.Extensions;

#endregion

namespace Ssz.Utils.CommandLine.Parsing
{
    internal abstract class ArgumentParser
    {
        #region construction and destruction

        protected ArgumentParser()
        {
            PostParsingState = new List<ParsingError>();
        }

        #endregion

        #region public functions

        public static bool CompareShort(string argument, char? option, bool caseSensitive)
        {
            return string.Compare(
                argument,
                ToOption(option),
                caseSensitive ? StringComparison.InvariantCulture : StringComparison.InvariantCultureIgnoreCase) == 0;
        }

        public static bool CompareLong(string argument, string option, bool caseSensitive)
        {
            return string.Compare(
                argument,
                ToOption(option),
                caseSensitive ? StringComparison.InvariantCulture : StringComparison.InvariantCultureIgnoreCase) == 0;
        }

        public static ArgumentParser Create(string argument, bool ignoreUnknownArguments = false)
        {
            if (argument.IsNumeric())
            {
                return null;
            }

            if (IsDash(argument))
            {
                return null;
            }

            if (IsLongOption(argument))
            {
                return new LongOptionParser(ignoreUnknownArguments);
            }

            if (IsShortOption(argument))
            {
                return new OptionGroupParser(ignoreUnknownArguments);
            }

            return null;
        }

        public static bool IsInputValue(string argument)
        {
            if (argument.IsNumeric())
            {
                return true;
            }

            if (argument.Length > 0)
            {
                return IsDash(argument) || !IsShortOption(argument);
            }

            return true;
        }

        public abstract PresentParserState Parse(IArgumentEnumerator argumentEnumerator, OptionMap map, object options);
        public List<ParsingError> PostParsingState { get; private set; }

        #endregion

        #region internal functions

        /// <summary>
        ///     Helper method for testing purpose.
        /// </summary>
        /// <param name="ae">An argument enumerator instance.</param>
        /// <returns>The next input value.</returns>
        internal static IList<string> InternalWrapperOfGetNextInputValues(IArgumentEnumerator ae)
        {
            return GetNextInputValues(ae);
        }

        #endregion

        #region protected functions

        protected static IList<string> GetNextInputValues(IArgumentEnumerator ae)
        {
            IList<string> list = new List<string>();

            while (ae.MoveNext())
            {
                if (IsInputValue(ae.Current))
                {
                    list.Add(ae.Current);
                }
                else
                {
                    break;
                }
            }

            if (!ae.MovePrevious())
            {
                throw new ParserException();
            }

            return list;
        }

        protected static PresentParserState BooleanToParserState(bool value)
        {
            return BooleanToParserState(value, false);
        }

        protected static PresentParserState BooleanToParserState(bool value, bool addMoveNextIfTrue)
        {
            if (value && !addMoveNextIfTrue)
            {
                return PresentParserState.Success;
            }

            if (value)
            {
                return PresentParserState.Success | PresentParserState.MoveOnNextElement;
            }

            return PresentParserState.Failure;
        }

        protected static void EnsureOptionAttributeIsArrayCompatible(OptionInfo option)
        {
            if (!option.IsAttributeArrayCompatible)
            {
                throw new ParserException();
            }
        }

        protected static void EnsureOptionArrayAttributeIsNotBoundToScalar(OptionInfo option)
        {
            if (!option.IsArray && option.IsAttributeArrayCompatible)
            {
                throw new ParserException();
            }
        }

        protected void DefineOptionThatViolatesFormat(OptionInfo option)
        {
            PostParsingState.Add(new ParsingError(option.ShortName, option.LongName, true));
        }

        #endregion

        #region private functions

        private static string ToOption(string value)
        {
            return string.Concat("--", value);
        }

        private static string ToOption(char? value)
        {
            return string.Concat("-", value);
        }

        private static bool IsDash(string value)
        {
            return value == "-";
        }

        private static bool IsShortOption(string value)
        {
            return value[0] == '-';
        }

        private static bool IsLongOption(string value)
        {
            return value[0] == '-' && value[1] == '-';
        }

        #endregion
    }
}