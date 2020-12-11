using System;

namespace Ssz.Utils.CommandLine.Attributes
{
    /// <summary>
    ///     Models an option that can accept multiple values as separated arguments.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public sealed class OptionArrayAttribute : BaseOptionAttribute
    {
        #region construction and destruction

        /// <summary>
        ///     Initializes a new instance of the <see cref="OptionArrayAttribute" /> class.
        ///     The default long name will be inferred from target property.
        /// </summary>
        public OptionArrayAttribute()
        {
            AutoLongName = true;
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="OptionArrayAttribute" /> class.
        /// </summary>
        /// <param name="shortName">The short name of the option.</param>
        public OptionArrayAttribute(char shortName)
            : base(shortName, @"")
        {
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="OptionArrayAttribute" /> class.
        /// </summary>
        /// <param name="longName">The long name of the option.</param>
        public OptionArrayAttribute(string longName)
            : base(null, longName)
        {
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="OptionArrayAttribute" /> class.
        /// </summary>
        /// <param name="shortName">The short name of the option.</param>
        /// <param name="longName">The long name of the option or null if not used.</param>
        public OptionArrayAttribute(char shortName, string longName)
            : base(shortName, longName)
        {
        }

        #endregion
    }
}