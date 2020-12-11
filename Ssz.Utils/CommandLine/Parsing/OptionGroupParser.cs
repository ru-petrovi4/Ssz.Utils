using System.Collections.Generic;

namespace Ssz.Utils.CommandLine.Parsing
{
    internal sealed class OptionGroupParser : ArgumentParser
    {
        #region construction and destruction

        public OptionGroupParser(bool ignoreUnkwnownArguments)
        {
            _ignoreUnkwnownArguments = ignoreUnkwnownArguments;
        }

        #endregion

        #region public functions

        public override PresentParserState Parse(IArgumentEnumerator argumentEnumerator, OptionMap map, object options)
        {
            var optionGroup = new OneCharStringEnumerator((argumentEnumerator.Current ?? "").Substring(1));

            while (optionGroup.MoveNext())
            {
                OptionInfo? option = map[optionGroup.Current];
                if (option == null)
                {
                    return _ignoreUnkwnownArguments ? PresentParserState.MoveOnNextElement : PresentParserState.Failure;
                }

                option.IsDefined = true;

                EnsureOptionArrayAttributeIsNotBoundToScalar(option);

                if (!option.IsBoolean)
                {
                    if (argumentEnumerator.IsLast && optionGroup.IsLast)
                    {
                        return PresentParserState.Failure;
                    }

                    bool valueSetting;
                    if (!optionGroup.IsLast)
                    {
                        if (!option.IsArray)
                        {
                            valueSetting = option.SetValue(optionGroup.GetRemainingFromNext(), options);
                            if (!valueSetting)
                            {
                                DefineOptionThatViolatesFormat(option);
                            }

                            return BooleanToParserState(valueSetting);
                        }

                        EnsureOptionAttributeIsArrayCompatible(option);

                        IList<string> items = GetNextInputValues(argumentEnumerator);
                        items.Insert(0, optionGroup.GetRemainingFromNext());

                        valueSetting = option.SetValue(items, options);
                        if (!valueSetting)
                        {
                            DefineOptionThatViolatesFormat(option);
                        }

                        return BooleanToParserState(valueSetting, true);
                    }

                    if (!argumentEnumerator.IsLast && !IsInputValue(argumentEnumerator.Next))
                    {
                        return PresentParserState.Failure;
                    }

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

                    IList<string> moreItems = GetNextInputValues(argumentEnumerator);

                    valueSetting = option.SetValue(moreItems, options);
                    if (!valueSetting)
                    {
                        DefineOptionThatViolatesFormat(option);
                    }

                    return BooleanToParserState(valueSetting);
                }

                if (!optionGroup.IsLast && map[optionGroup.Next] == null)
                {
                    return PresentParserState.Failure;
                }

                if (!option.SetValue(true, options))
                {
                    return PresentParserState.Failure;
                }
            }

            return PresentParserState.Success;
        }

        #endregion

        #region private fields

        private readonly bool _ignoreUnkwnownArguments;

        #endregion
    }
}