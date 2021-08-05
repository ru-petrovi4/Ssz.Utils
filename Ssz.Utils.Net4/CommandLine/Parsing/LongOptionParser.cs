﻿#region License

// <copyright file="LongOptionParser.cs" company="Giacomo Stelluti Scala">
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

using System.Collections.Generic;

namespace Ssz.Utils.CommandLine.Parsing
{
    internal sealed class LongOptionParser : ArgumentParser
    {
        #region construction and destruction

        public LongOptionParser(bool ignoreUnkwnownArguments)
        {
            _ignoreUnkwnownArguments = ignoreUnkwnownArguments;
        }

        #endregion

        #region public functions

        public override PresentParserState Parse(IArgumentEnumerator argumentEnumerator, OptionMap map, object options)
        {
            string[] parts = argumentEnumerator.Current.Substring(2).Split(new[] {'='}, 2);
            OptionInfo option = map[parts[0]];

            if (option == null)
            {
                return _ignoreUnkwnownArguments ? PresentParserState.MoveOnNextElement : PresentParserState.Failure;
            }

            option.IsDefined = true;

            EnsureOptionArrayAttributeIsNotBoundToScalar(option);

            bool valueSetting;

            if (!option.IsBoolean)
            {
                if (parts.Length == 1 && (argumentEnumerator.IsLast || !IsInputValue(argumentEnumerator.Next)))
                {
                    return PresentParserState.Failure;
                }

                if (parts.Length == 2)
                {
                    if (!option.IsArray)
                    {
                        valueSetting = option.SetValue(parts[1], options);
                        if (!valueSetting)
                        {
                            DefineOptionThatViolatesFormat(option);
                        }

                        return BooleanToParserState(valueSetting);
                    }

                    EnsureOptionAttributeIsArrayCompatible(option);

                    IList<string> items = GetNextInputValues(argumentEnumerator);
                    items.Insert(0, parts[1]);

                    valueSetting = option.SetValue(items, options);
                    if (!valueSetting)
                    {
                        DefineOptionThatViolatesFormat(option);
                    }

                    return BooleanToParserState(valueSetting);
                }
                else
                {
                    if (!option.IsArray)
                    {
                        valueSetting = option.SetValue(argumentEnumerator.Next, options);
                        if (!valueSetting)
                        {
                            DefineOptionThatViolatesFormat(option);
                        }

                        return BooleanToParserState(valueSetting, true);
                    }

                    EnsureOptionAttributeIsArrayCompatible(option);

                    IList<string> items = GetNextInputValues(argumentEnumerator);

                    valueSetting = option.SetValue(items, options);
                    if (!valueSetting)
                    {
                        DefineOptionThatViolatesFormat(option);
                    }

                    return BooleanToParserState(valueSetting);
                }
            }

            if (parts.Length == 2)
            {
                return PresentParserState.Failure;
            }

            valueSetting = option.SetValue(true, options);
            if (!valueSetting)
            {
                DefineOptionThatViolatesFormat(option);
            }

            return BooleanToParserState(valueSetting);
        }

        #endregion

        #region private fields

        private readonly bool _ignoreUnkwnownArguments;

        #endregion
    }
}