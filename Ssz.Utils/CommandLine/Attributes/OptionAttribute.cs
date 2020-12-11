using System;
using Ssz.Utils.CommandLine.Parsing;


namespace Ssz.Utils.CommandLine.Attributes
{
    /// <summary>
    ///     Models an option specification.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public sealed class OptionAttribute : BaseOptionAttribute
    {
        #region construction and destruction

        /// <summary>
        ///     Initializes a new instance of the <see cref="OptionAttribute" /> class.
        ///     The default long name will be inferred from target property.
        /// </summary>
        public OptionAttribute()
        {
            AutoLongName = true;
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="OptionAttribute" /> class.
        /// </summary>
        /// <param name="shortName">The short name of the option..</param>
        public OptionAttribute(char shortName)
            : base(shortName, @"")
        {
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="OptionAttribute" /> class.
        /// </summary>
        /// <param name="longName">The long name of the option.</param>
        public OptionAttribute(string longName)
            : base(null, longName)
        {
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="OptionAttribute" /> class.
        /// </summary>
        /// <param name="shortName">The short name of the option.</param>
        /// <param name="longName">The long name of the option or null if not used.</param>
        public OptionAttribute(char shortName, string longName)
            : base(shortName, longName)
        {
        }

        #endregion        
    }
}