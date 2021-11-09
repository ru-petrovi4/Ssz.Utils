#region License

// <copyright file="OptionMap.cs" company="Giacomo Stelluti Scala">
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
using System.Reflection;
using Ssz.Utils.CommandLine.Attributes;
using Ssz.Utils.CommandLine.Extensions;
using Ssz.Utils.CommandLine.Infrastructure;

#endregion

namespace Ssz.Utils.CommandLine.Parsing
{
    internal sealed class OptionMap
    {
        #region construction and destruction

        /// <summary>
        ///     Initializes a new instance of the <see cref="OptionMap" /> class.
        ///     It is internal rather than private for unit testing purpose.
        /// </summary>
        /// <param name="capacity">Initial internal capacity.</param>
        /// <param name="settings">Parser settings instance.</param>
        internal OptionMap(int capacity, ParserSettings settings)
        {
            _settings = settings;

            IEqualityComparer<string> comparer =
                _settings.CaseSensitive ? StringComparer.InvariantCulture : StringComparer.InvariantCultureIgnoreCase;
            _names = new Dictionary<string, string>(capacity, comparer);
            _map = new Dictionary<string, OptionInfo>(capacity*2, comparer);

            if (_settings.MutuallyExclusive)
            {
                _mutuallyExclusiveSetMap = new Dictionary<string, MutuallyExclusiveInfo>(capacity,
                    StringComparer.InvariantCultureIgnoreCase);
            }
        }

        #endregion

        #region public functions

        public static OptionMap Create(object target, ParserSettings settings)
        {
            IList<Pair<PropertyInfo, BaseOptionAttribute>> list =
                ReflectionHelper.RetrievePropertyList<BaseOptionAttribute>(target);
            if (list is null)
            {
                return null;
            }

            var map = new OptionMap(list.Count, settings);

            foreach (Pair<PropertyInfo, BaseOptionAttribute> pair in list)
            {
                if (pair.Left != null && pair.Right != null)
                {
                    string uniqueName;
                    if (pair.Right.AutoLongName)
                    {
                        uniqueName = pair.Left.Name.ToLowerInvariant();
                        pair.Right.LongName = uniqueName;
                    }
                    else
                    {
                        uniqueName = pair.Right.UniqueName;
                    }

                    map[uniqueName] = new OptionInfo(pair.Right, pair.Left, settings.ParsingCulture);
                }
            }

            map.RawOptions = target;
            return map;
        }

        public static OptionMap Create(
            object target,
            IList<Pair<PropertyInfo, VerbOptionAttribute>> verbs,
            ParserSettings settings)
        {
            var map = new OptionMap(verbs.Count, settings);

            foreach (Pair<PropertyInfo, VerbOptionAttribute> verb in verbs)
            {
                var optionInfo = new OptionInfo(verb.Right, verb.Left, settings.ParsingCulture)
                                 {
                                     HasParameterLessCtor =
                                         verb.Left.PropertyType.GetConstructor(Type.EmptyTypes) != null
                                 };

                if (!optionInfo.HasParameterLessCtor && verb.Left.GetValue(target, null) is null)
                {
                    throw new ParserException("Type {0} must have a parameterless constructor or" +
                                              " be already initialized to be used as a verb command.".FormatInvariant(
                                                  verb.Left.PropertyType));
                }

                map[verb.Right.UniqueName] = optionInfo;
            }

            map.RawOptions = target;
            return map;
        }

        public bool EnforceRules()
        {
            return EnforceMutuallyExclusiveMap() && EnforceRequiredRule();
        }

        public void SetDefaults()
        {
            foreach (OptionInfo option in _map.Values)
            {
                option.SetDefault(RawOptions);
            }
        }

        public OptionInfo this[string key]
        {
            get
            {
                OptionInfo option = null;

                if (_map.ContainsKey(key))
                {
                    option = _map[key];
                }
                else
                {
                    if (_names.ContainsKey(key))
                    {
                        string optionKey = _names[key];
                        option = _map[optionKey];
                    }
                }

                return option;
            }

            set
            {
                _map[key] = value;

                if (value.HasBothNames)
                {
                    // ReSharper disable PossibleInvalidOperationException
                    _names[value.LongName] = new string(value.ShortName.Value, 1);
                    // ReSharper restore PossibleInvalidOperationException
                }
            }
        }

        #endregion

        #region internal functions

        internal object RawOptions { private get; set; }

        #endregion

        #region private functions

        private static void SetParserStateIfNeeded(object options, OptionInfo option, bool? required,
            bool? mutualExclusiveness)
        {
            IList<Pair<PropertyInfo, ParserStateAttribute>> list =
                ReflectionHelper.RetrievePropertyList<ParserStateAttribute>(options);
            if (list.Count == 0)
            {
                return;
            }

            PropertyInfo property = list[0].Left;

            // This method can be called when parser state is still not intialized
            if (property.GetValue(options, null) is null)
            {
                property.SetValue(options, new ParserState(), null);
            }

            var parserState = (IParserState) property.GetValue(options, null);
            if (parserState is null)
            {
                return;
            }

            var error = new ParsingError
                        {
                            BadOption =
                            {
                                ShortName = option.ShortName,
                                LongName = option.LongName
                            }
                        };

            if (required != null)
            {
                error.ViolatesRequired = required.Value;
            }

            if (mutualExclusiveness != null)
            {
                error.ViolatesMutualExclusiveness = mutualExclusiveness.Value;
            }

            parserState.Errors.Add(error);
        }

        private bool EnforceRequiredRule()
        {
            bool requiredRulesAllMet = true;

            foreach (OptionInfo option in _map.Values)
            {
                if (option.Required && !(option.IsDefined && option.ReceivedValue))
                {
                    SetParserStateIfNeeded(RawOptions, option, true, null);
                    requiredRulesAllMet = false;
                }
            }

            return requiredRulesAllMet;
        }

        private bool EnforceMutuallyExclusiveMap()
        {
            if (!_settings.MutuallyExclusive)
            {
                return true;
            }

            foreach (OptionInfo option in _map.Values)
            {
                if (option.IsDefined && option.MutuallyExclusiveSet != null)
                {
                    BuildMutuallyExclusiveMap(option);
                }
            }

            foreach (MutuallyExclusiveInfo info in _mutuallyExclusiveSetMap.Values)
            {
                if (info.Occurrence > 1)
                {
                    SetParserStateIfNeeded(RawOptions, info.BadOption, null, true);
                    return false;
                }
            }

            return true;
        }

        private void BuildMutuallyExclusiveMap(OptionInfo option)
        {
            string setName = option.MutuallyExclusiveSet;
            if (!_mutuallyExclusiveSetMap.ContainsKey(setName))
            {
                _mutuallyExclusiveSetMap.Add(setName, new MutuallyExclusiveInfo(option));
            }

            _mutuallyExclusiveSetMap[setName].IncrementOccurrence();
        }

        #endregion

        #region private fields

        private readonly ParserSettings _settings;
        private readonly Dictionary<string, string> _names;
        private readonly Dictionary<string, OptionInfo> _map;
        private readonly Dictionary<string, MutuallyExclusiveInfo> _mutuallyExclusiveSetMap;

        #endregion

        private sealed class MutuallyExclusiveInfo
        {
            #region construction and destruction

            public MutuallyExclusiveInfo(OptionInfo option)
            {
                BadOption = option;
            }

            #endregion

            #region public functions

            public void IncrementOccurrence()
            {
                ++_count;
            }

            public OptionInfo BadOption { get; private set; }

            public int Occurrence
            {
                get { return _count; }
            }

            #endregion

            #region private fields

            private int _count;

            #endregion
        }
    }
}