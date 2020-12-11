using System;


namespace Ssz.Utils.CommandLine.Attributes
{
    /// <summary>
    ///     Maps a single unnamed option to the target property. Values will be mapped in order of Index.
    ///     This attribute takes precedence over <see cref="ValueListAttribute" /> with which
    ///     can coexist.
    /// </summary>
    /// <remarks>It can handle only scalar values. Do not apply to arrays or lists.</remarks>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public sealed class ValueOptionAttribute : Attribute
    {
        #region construction and destruction

        /// <summary>
        ///     Initializes a new instance of the <see cref="ValueOptionAttribute" /> class.
        /// </summary>
        /// <param name="index">The _index of the option.</param>
        public ValueOptionAttribute(int index)
        {
            _index = index;
        }

        #endregion

        #region public functions

        /// <summary>
        ///     Gets the position this option has on the command line.
        /// </summary>
        public int Index
        {
            get { return _index; }
        }

        #endregion

        #region private fields

        private readonly int _index;

        #endregion
    }
}