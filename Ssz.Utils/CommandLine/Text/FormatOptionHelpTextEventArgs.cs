using System;
using Ssz.Utils.CommandLine.Attributes;


namespace Ssz.Utils.CommandLine.Text
{
    /// <summary>
    ///     Provides data for the FormatOptionHelpText event.
    /// </summary>
    public class FormatOptionHelpTextEventArgs : EventArgs
    {
        #region construction and destruction

        /// <summary>
        ///     Initializes a new instance of the <see cref="CommandLine.Text.FormatOptionHelpTextEventArgs" /> class.
        /// </summary>
        /// <param name="option">Option to format.</param>
        public FormatOptionHelpTextEventArgs(BaseOptionAttribute option)
        {
            _option = option;
        }

        #endregion

        #region public functions

        /// <summary>
        ///     Gets the option to format.
        /// </summary>
        public BaseOptionAttribute Option
        {
            get { return _option; }
        }

        #endregion

        #region private fields

        private readonly BaseOptionAttribute _option;

        #endregion
    }
}