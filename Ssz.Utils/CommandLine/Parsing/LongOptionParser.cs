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
            string[] parts = (argumentEnumerator.Current ?? @"").Substring(2).Split(new[] {'='}, 2);
            OptionInfo? option = map[parts[0]];

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
                        valueSetting = option.SetValue(argumentEnumerator.Next ?? @"", options);
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