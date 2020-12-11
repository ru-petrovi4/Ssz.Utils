using System;
using Ssz.Utils.CommandLine.Infrastructure;


namespace Ssz.Utils.CommandLine.Attributes
{
    /// <summary>
    ///     Models a verb command specification.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public sealed class VerbOptionAttribute : BaseOptionAttribute
    {
        #region construction and destruction

        /// <summary>
        ///     Initializes a new instance of the <see cref="VerbOptionAttribute" /> class.
        /// </summary>
        /// <param name="longName">The long name of the verb command.</param>
        public VerbOptionAttribute(string longName)
            : base(null, longName)
        {            
        }

        #endregion

        #region public functions

        /// <summary>
        ///     Verb commands do not support short name by design.
        /// </summary>
        public override char? ShortName
        {
            get { return null; }
            internal set
            {
                throw new InvalidOperationException(SR.InvalidOperationException_DoNotUseShortNameForVerbCommands);
            }
        }

        /// <summary>
        ///     Verb commands cannot be mandatory since are mutually exclusive by design.
        /// </summary>
        public override bool Required
        {
            get { return false; }
            set
            {
                throw new InvalidOperationException(SR.InvalidOperationException_DoNotSetRequiredPropertyForVerbCommands);
            }
        }

        #endregion
    }
}