using System;
using System.Diagnostics.CodeAnalysis;

namespace Ssz.Utils.CommandLine.Attributes
{
    /// <summary>
    ///     Models an option that can accept multiple values.
    ///     Must be applied to a field compatible with an <see cref="System.Collections.Generic.IList&lt;T&gt;" /> interface
    ///     of <see cref="System.String" /> instances.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public sealed class OptionListAttribute : BaseOptionAttribute
    {
        #region construction and destruction

        /// <summary>
        ///     Initializes a new instance of the <see cref="OptionListAttribute" /> class.
        ///     The default long name will be inferred from target property.
        /// </summary>
        public OptionListAttribute()
        {
            AutoLongName = true;

            Separator = DefaultSeparator;
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="OptionListAttribute" /> class.
        /// </summary>
        /// <param name="shortName">The short name of the option.</param>
        public OptionListAttribute(char shortName)
            : base(shortName, @"")
        {
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="OptionListAttribute" /> class.
        /// </summary>
        /// <param name="longName">The long name of the option or null if not used.</param>
        public OptionListAttribute(string longName)
            : base(null, longName)
        {
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="OptionListAttribute" /> class.
        /// </summary>
        /// <param name="shortName">The short name of the option.</param>
        /// <param name="longName">The long name of the option or null if not used.</param>
        public OptionListAttribute(char shortName, string longName)
            : base(shortName, longName)
        {
            Separator = DefaultSeparator;
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="OptionListAttribute" /> class.
        /// </summary>
        /// <param name="shortName">The short name of the option or null if not used.</param>
        /// <param name="longName">The long name of the option or null if not used.</param>
        /// <param name="separator">Values separator character.</param>
        public OptionListAttribute(char shortName, string longName, char separator)
            : base(shortName, longName)
        {
            Separator = separator;
        }

        #endregion

        #region public functions

        /// <summary>
        ///     Gets or sets the values separator character.
        /// </summary>
        [SuppressMessage("Microsoft.Design", "CA1019:DefineAccessorsForAttributeArguments",
            Justification =
                "The char Separator property matches shortName char constructor argument because the ShortName property is defined in BaseOptionAttribute as nullable char"
            )]
        public char Separator { get; set; }

        #endregion

        #region private fields

        private const char DefaultSeparator = ':';

        #endregion
    }
}