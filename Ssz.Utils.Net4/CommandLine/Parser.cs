#region License

// <copyright file="Parser.cs" company="Giacomo Stelluti Scala">
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
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using Ssz.Utils.Net4.CommandLine.Attributes;
using Ssz.Utils.Net4.CommandLine.Infrastructure;
using Ssz.Utils.Net4.CommandLine.Parsing;
using Ssz.Utils.Net4.CommandLine.Text;

#endregion

namespace Ssz.Utils.Net4.CommandLine
{
    /// <summary>
    ///     Provides methods to parse command line arguments.
    /// </summary>
    public sealed class Parser : IDisposable
    {
        #region construction and destruction

        /// <summary>
        ///     Initializes a new instance of the <see cref="CommandLine.Parser" /> class.
        /// </summary>
        public Parser()
        {
            _settings = new ParserSettings {Consumed = true};
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="Parser" /> class,
        ///     configurable with <see cref="ParserSettings" /> using a delegate.
        /// </summary>
        /// <param name="configuration">
        ///     The <see cref="Action&lt;ParserSettings&gt;" /> delegate used to configure
        ///     aspects and behaviors of the parser.
        /// </param>
        public Parser(Action<ParserSettings> configuration)
        {
            Assumes.NotNull(configuration, "configuration", SR.ArgumentNullException_ParserSettingsDelegateCannotBeNull);

            _settings = new ParserSettings();
            configuration.Invoke(Settings);
            _settings.Consumed = true;
        }

        [SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "singleton",
            Justification =
                "The constructor that accepts a boolean is designed to support default singleton, the parameter is ignored"
            )]
        private Parser(bool singleton)
            : this(with =>
                   {
                       with.CaseSensitive = false;
                       with.MutuallyExclusive = false;
                       with.HelpWriter = Console.Error;
                       with.ParsingCulture = CultureInfo.InvariantCulture;
                   })
        {
        }

        /// <summary>
        ///     Frees resources owned by the instance.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);

            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }

            if (disposing)
            {
                if (_settings != null)
                {
                    _settings.Dispose();
                }

                _disposed = true;
            }
        }

        /// <summary>
        ///     Finalizes an instance of the <see cref="CommandLine.Parser" /> class.
        /// </summary>
        ~Parser()
        {
            Dispose(false);
        }

        #endregion

        #region public functions

        /// <summary>
        ///     Gets the singleton instance created with basic defaults.
        /// </summary>
        public static Parser Default
        {
            get { return DefaultParser; }
        }

        /// <summary>
        ///     Default exit code (1) used by <see cref="Parser.ParseArgumentsStrict(string[],object,Action)" />
        ///     and <see cref="Parser.ParseArgumentsStrict(string[],object,Action&lt;string,object&gt;,Action)" /> overloads.
        /// </summary>
        public const int DefaultExitCodeFail = 1;

        /// <summary>
        ///     Parses a <see cref="System.String" /> array of command line arguments, setting values in
        ///     <paramref name="options" />
        ///     parameter instance's public fields decorated with appropriate attributes.
        /// </summary>
        /// <param name="args">A <see cref="System.String" /> array of command line arguments.</param>
        /// <param name="options">
        ///     An instance used to receive values.
        ///     Parsing rules are defined using <see cref="BaseOptionAttribute" /> derived types.
        /// </param>
        /// <returns>True if parsing process succeed.</returns>
        /// <exception cref="System.ArgumentNullException">Thrown if <paramref name="args" /> is null.</exception>
        /// <exception cref="System.ArgumentNullException">Thrown if <paramref name="options" /> is null.</exception>
        public bool ParseArguments(string[] args, object options)
        {
            Assumes.NotNull(args, "args", SR.ArgumentNullException_ArgsStringArrayCannotBeNull);
            Assumes.NotNull(options, "options", SR.ArgumentNullException_OptionsInstanceCannotBeNull);

            return DoParseArguments(args, options);
        }

        /// <summary>
        ///     Parses a <see cref="System.String" /> array of command line arguments with verb commands, setting values in
        ///     <paramref name="options" />
        ///     parameter instance's public fields decorated with appropriate attributes.
        ///     This overload supports verb commands.
        /// </summary>
        /// <param name="args">A <see cref="System.String" /> array of command line arguments.</param>
        /// <param name="options">
        ///     An instance used to receive values.
        ///     Parsing rules are defined using <see cref="BaseOptionAttribute" /> derived types.
        /// </param>
        /// <param name="onVerbCommand">Delegate executed to capture verb command name and instance.</param>
        /// <returns>True if parsing process succeed.</returns>
        /// <exception cref="System.ArgumentNullException">Thrown if <paramref name="args" /> is null.</exception>
        /// <exception cref="System.ArgumentNullException">Thrown if <paramref name="options" /> is null.</exception>
        /// <exception cref="System.ArgumentNullException">Thrown if <paramref name="onVerbCommand" /> is null.</exception>
        public bool ParseArguments(string[] args, object options, Action<string, object> onVerbCommand)
        {
            Assumes.NotNull(args, "args", SR.ArgumentNullException_ArgsStringArrayCannotBeNull);
            Assumes.NotNull(options, "options", SR.ArgumentNullException_OptionsInstanceCannotBeNull);
            Assumes.NotNull(options, "onVerbCommand", SR.ArgumentNullException_OnVerbDelegateCannotBeNull);

            object verbInstance = null;

            bool result = DoParseArgumentsVerbs(args, options, ref verbInstance);

            onVerbCommand(args.FirstOrDefault() ?? string.Empty, result ? verbInstance : null);

            return result;
        }

        /// <summary>
        ///     Parses a <see cref="System.String" /> array of command line arguments, setting values in
        ///     <paramref name="options" />
        ///     parameter instance's public fields decorated with appropriate attributes. If parsing fails, the method invokes
        ///     the <paramref name="onFail" /> delegate, if null exits with <see cref="Parser.DefaultExitCodeFail" />.
        /// </summary>
        /// <param name="args">A <see cref="System.String" /> array of command line arguments.</param>
        /// <param name="options">
        ///     An object's instance used to receive values.
        ///     Parsing rules are defined using <see cref="BaseOptionAttribute" /> derived types.
        /// </param>
        /// <param name="onFail">The <see cref="Action" /> delegate executed when parsing fails.</param>
        /// <returns>True if parsing process succeed.</returns>
        /// <exception cref="System.ArgumentNullException">Thrown if <paramref name="args" /> is null.</exception>
        /// <exception cref="System.ArgumentNullException">Thrown if <paramref name="options" /> is null.</exception>
        public bool ParseArgumentsStrict(string[] args, object options, Action onFail = null)
        {
            Assumes.NotNull(args, "args", SR.ArgumentNullException_ArgsStringArrayCannotBeNull);
            Assumes.NotNull(options, "options", SR.ArgumentNullException_OptionsInstanceCannotBeNull);

            if (!DoParseArguments(args, options))
            {
                InvokeAutoBuildIfNeeded(options);

                if (onFail is null)
                {
                    Environment.Exit(DefaultExitCodeFail);
                }
                else
                {
                    onFail();
                }

                return false;
            }

            return true;
        }

        /// <summary>
        ///     Parses a <see cref="System.String" /> array of command line arguments with verb commands, setting values in
        ///     <paramref name="options" />
        ///     parameter instance's public fields decorated with appropriate attributes. If parsing fails, the method invokes
        ///     the <paramref name="onFail" /> delegate, if null exits with <see cref="Parser.DefaultExitCodeFail" />.
        ///     This overload supports verb commands.
        /// </summary>
        /// <param name="args">A <see cref="System.String" /> array of command line arguments.</param>
        /// <param name="options">
        ///     An instance used to receive values.
        ///     Parsing rules are defined using <see cref="BaseOptionAttribute" /> derived types.
        /// </param>
        /// <param name="onVerbCommand">Delegate executed to capture verb command name and instance.</param>
        /// <param name="onFail">The <see cref="Action" /> delegate executed when parsing fails.</param>
        /// <returns>True if parsing process succeed.</returns>
        /// <exception cref="System.ArgumentNullException">Thrown if <paramref name="args" /> is null.</exception>
        /// <exception cref="System.ArgumentNullException">Thrown if <paramref name="options" /> is null.</exception>
        /// <exception cref="System.ArgumentNullException">Thrown if <paramref name="onVerbCommand" /> is null.</exception>
        public bool ParseArgumentsStrict(string[] args, object options, Action<string, object> onVerbCommand,
            Action onFail = null)
        {
            Assumes.NotNull(args, "args", SR.ArgumentNullException_ArgsStringArrayCannotBeNull);
            Assumes.NotNull(options, "options", SR.ArgumentNullException_OptionsInstanceCannotBeNull);
            Assumes.NotNull(options, "onVerbCommand", SR.ArgumentNullException_OnVerbDelegateCannotBeNull);

            object verbInstance = null;

            if (!DoParseArgumentsVerbs(args, options, ref verbInstance))
            {
                onVerbCommand(args.FirstOrDefault() ?? string.Empty, null);

                InvokeAutoBuildIfNeeded(options);

                if (onFail is null)
                {
                    Environment.Exit(DefaultExitCodeFail);
                }
                else
                {
                    onFail();
                }

                return false;
            }

            onVerbCommand(args.FirstOrDefault() ?? string.Empty, verbInstance);
            return true;
        }

        /// <summary>
        ///     Gets the instance that implements <see cref="CommandLine.ParserSettings" /> in use.
        /// </summary>
        public ParserSettings Settings
        {
            get { return _settings; }
        }

        #endregion

        #region internal functions

        [SuppressMessage("Microsoft.Design", "CA1021:AvoidOutParameters", MessageId = "2#", Justification = "By design")
        ]
        internal static object InternalGetVerbOptionsInstanceByName(string verb, object target, out bool found)
        {
            found = false;
            if (string.IsNullOrEmpty(verb))
            {
                return target;
            }

            Pair<PropertyInfo, VerbOptionAttribute> pair =
                ReflectionHelper.RetrieveOptionProperty<VerbOptionAttribute>(target, verb);
            found = pair != null;
            return found ? pair.Left.GetValue(target, null) : target;
        }

        #endregion

        #region private functions

        private static void SetParserStateIfNeeded(object options, IEnumerable<ParsingError> errors)
        {
            if (!options.CanReceiveParserState())
            {
                return;
            }

            PropertyInfo property = ReflectionHelper.RetrievePropertyList<ParserStateAttribute>(options)[0].Left;

            object parserState = property.GetValue(options, null);
            if (parserState != null)
            {
                if (!(parserState is IParserState))
                {
                    throw new InvalidOperationException(SR.InvalidOperationException_ParserStateInstanceBadApplied);
                }

                if (!(parserState is ParserState))
                {
                    throw new InvalidOperationException(SR.InvalidOperationException_ParserStateInstanceCannotBeNotNull);
                }
            }
            else
            {
                try
                {
                    property.SetValue(options, new ParserState(), null);
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException(SR.InvalidOperationException_ParserStateInstanceBadApplied, ex);
                }
            }

            var state = (IParserState) property.GetValue(options, null);

            foreach (ParsingError error in errors)
            {
                state.Errors.Add(error);
            }
        }

        private static StringComparison GetStringComparison(ParserSettings settings)
        {
            return settings.CaseSensitive ? StringComparison.InvariantCulture : StringComparison.InvariantCultureIgnoreCase;
        }

        private bool DoParseArguments(string[] args, object options)
        {
            Pair<MethodInfo, HelpOptionAttribute> pair = ReflectionHelper.RetrieveMethod<HelpOptionAttribute>(options);
            TextWriter helpWriter = _settings.HelpWriter;

            if (pair != null && helpWriter != null)
            {
                // If help can be handled is displayed if is requested or if parsing fails
                if (ParseHelp(args, pair.Right) || !DoParseArgumentsCore(args, options))
                {
                    string helpText;
                    HelpOptionAttribute.InvokeMethod(options, pair, out helpText);
                    helpWriter.Write(helpText);
                    return false;
                }

                return true;
            }

            return DoParseArgumentsCore(args, options);
        }

        private bool DoParseArgumentsCore(string[] args, object options)
        {
            bool hadError = false;
            OptionMap optionMap = OptionMap.Create(options, _settings);
            optionMap.SetDefaults();
            var valueMapper = new ValueMapper(options, _settings.ParsingCulture);

            var arguments = new StringArrayEnumerator(args);
            while (arguments.MoveNext())
            {
                string argument = arguments.Current;
                if (string.IsNullOrEmpty(argument))
                {
                    continue;
                }

                ArgumentParser parser = ArgumentParser.Create(argument, _settings.IgnoreUnknownArguments);
                if (parser != null)
                {
                    PresentParserState result = parser.Parse(arguments, optionMap, options);
                    if ((result & PresentParserState.Failure) == PresentParserState.Failure)
                    {
                        SetParserStateIfNeeded(options, parser.PostParsingState);
                        hadError = true;
                        continue;
                    }

                    if ((result & PresentParserState.MoveOnNextElement) == PresentParserState.MoveOnNextElement)
                    {
                        arguments.MoveNext();
                    }
                }
                else if (valueMapper.CanReceiveValues)
                {
                    if (!valueMapper.MapValueItem(argument))
                    {
                        hadError = true;
                    }
                }
            }

            hadError |= !optionMap.EnforceRules();

            return !hadError;
        }

        private bool DoParseArgumentsVerbs(string[] args, object options, ref object verbInstance)
        {
            IList<Pair<PropertyInfo, VerbOptionAttribute>> verbs =
                ReflectionHelper.RetrievePropertyList<VerbOptionAttribute>(options);
            Pair<MethodInfo, HelpVerbOptionAttribute> helpInfo =
                ReflectionHelper.RetrieveMethod<HelpVerbOptionAttribute>(options);
            if (args.Length == 0)
            {
                if (helpInfo != null && _settings.HelpWriter != null)
                {
                    DisplayHelpVerbText(options, helpInfo, null);
                }

                return false;
            }

            OptionMap optionMap = OptionMap.Create(options, verbs, _settings);

            if (TryParseHelpVerb(args, options, helpInfo, optionMap))
            {
                return false;
            }

            OptionInfo verbOption = optionMap[args.First()];

            // User invoked a bad verb name
            if (verbOption is null)
            {
                if (helpInfo != null)
                {
                    DisplayHelpVerbText(options, helpInfo, null);
                }

                return false;
            }

            verbInstance = verbOption.GetValue(options);
            if (verbInstance is null)
            {
                // Developer has not provided a default value and did not assign an instance
                verbInstance = verbOption.CreateInstance(options);
            }

            bool verbResult = DoParseArgumentsCore(args.Skip(1).ToArray(), verbInstance);
            if (!verbResult && helpInfo != null)
            {
                // Particular verb parsing failed, we try to print its help
                DisplayHelpVerbText(options, helpInfo, args.First());
            }

            return verbResult;
        }

        private bool ParseHelp(string[] args, HelpOptionAttribute helpOption)
        {
            bool caseSensitive = _settings.CaseSensitive;
            foreach (string arg in args)
            {
                if (helpOption.ShortName != null)
                {
                    if (ArgumentParser.CompareShort(arg, helpOption.ShortName, caseSensitive))
                    {
                        return true;
                    }
                }

                if (string.IsNullOrEmpty(helpOption.LongName))
                {
                    continue;
                }

                if (ArgumentParser.CompareLong(arg, helpOption.LongName, caseSensitive))
                {
                    return true;
                }
            }

            return false;
        }

        private bool TryParseHelpVerb(string[] args, object options, Pair<MethodInfo, HelpVerbOptionAttribute> helpInfo,
            OptionMap optionMap)
        {
            TextWriter helpWriter = _settings.HelpWriter;
            if (helpInfo != null && helpWriter != null)
            {
                if (string.Compare(args[0], helpInfo.Right.LongName, GetStringComparison(_settings)) == 0)
                {
                    // User explicitly requested help
                    string verb = args.FirstOrDefault();
                    if (verb != null)
                    {
                        OptionInfo verbOption = optionMap[verb];
                        if (verbOption != null)
                        {
                            if (verbOption.GetValue(options) is null)
                            {
                                // We need to create an instance also to render help
                                verbOption.CreateInstance(options);
                            }
                        }
                    }

                    DisplayHelpVerbText(options, helpInfo, verb);
                    return true;
                }
            }

            return false;
        }

        private void DisplayHelpVerbText(object options, Pair<MethodInfo, HelpVerbOptionAttribute> helpInfo, string verb)
        {
            string helpText;
            if (verb is null)
            {
                HelpVerbOptionAttribute.InvokeMethod(options, helpInfo, null, out helpText);
            }
            else
            {
                HelpVerbOptionAttribute.InvokeMethod(options, helpInfo, verb, out helpText);
            }

            if (_settings.HelpWriter != null)
            {
                _settings.HelpWriter.Write(helpText);
            }
        }

        private void InvokeAutoBuildIfNeeded(object options)
        {
            if (_settings.HelpWriter is null ||
                options.HasHelp() ||
                options.HasVerbHelp())
            {
                return;
            }

            // We print help text for the user
            _settings.HelpWriter.Write(
                HelpText.AutoBuild(
                    options,
                    current => HelpText.DefaultParsingErrorsHandler(options, current),
                    options.HasVerbs()));
        }

        #endregion

        #region private fields

        private static readonly Parser DefaultParser = new Parser(true);
        private readonly ParserSettings _settings;
        private bool _disposed;

        #endregion
    }
}