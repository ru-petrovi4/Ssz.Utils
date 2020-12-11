namespace Ssz.Utils.CommandLine
{
    /// <summary>
    ///     Models a bad parsed option.
    /// </summary>
    public sealed class BadOptionInfo
    {
        #region construction and destruction

        internal BadOptionInfo() : 
            this(null, @"")
        {
        }

        internal BadOptionInfo(char? shortName, string longName)
        {
            ShortName = shortName;
            LongName = longName;
        }

        #endregion

        #region public functions

        /// <summary>
        ///     Gets the short name of the option.
        /// </summary>
        /// <value>Returns the short name of the option.</value>
        public char? ShortName { get; internal set; }

        /// <summary>
        ///     Gets the long name of the option.
        /// </summary>
        /// <value>Returns the long name of the option.</value>
        public string LongName { get; internal set; }

        #endregion
    }
}