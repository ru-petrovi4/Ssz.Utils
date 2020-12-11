using System;
using Ssz.Utils.CommandLine.Extensions;
using Ssz.Utils.CommandLine.Infrastructure;

namespace Ssz.Utils.CommandLine.Attributes
{
    /// <summary>
    ///     Provides base properties for creating an attribute, used to define rules for command line parsing.
    /// </summary>
    public abstract class BaseOptionAttribute : Attribute
    {
        #region construction and destruction

        /// <summary>
        ///     Initializes a new instance of the <see cref="BaseOptionAttribute" /> class.
        /// </summary>
        protected BaseOptionAttribute() :
            this(null, @"")
        {
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="BaseOptionAttribute" /> class.
        ///     Validating <paramref name="shortName" /> and <paramref name="longName" />.
        /// </summary>
        /// <param name="shortName">Short name of the option.</param>
        /// <param name="longName">Long name of the option.</param>
        protected BaseOptionAttribute(char shortName, string longName)
        {
            _shortName = shortName;
            if (_shortName.Value.IsWhiteSpace() || _shortName.Value.IsLineTerminator())
            {
                throw new ArgumentException(SR.ArgumentException_NoWhiteSpaceOrLineTerminatorInShortName, "shortName");
            }

            UniqueName = new string(shortName, 1);
            LongName = longName;
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="BaseOptionAttribute" /> class. Validating
        ///     <paramref name="shortName" />
        ///     and <paramref name="longName" />. This constructor accepts a <see cref="Nullable&lt;Char&gt;" /> as short name.
        /// </summary>
        /// <param name="shortName">Short name of the option.</param>
        /// <param name="longName">Long name of the option.</param>
        protected BaseOptionAttribute(char? shortName, string longName)
        {
            _shortName = shortName;
            string? uniqueName = null;
            if (_shortName != null)
            {
                if (_shortName.Value.IsWhiteSpace() || _shortName.Value.IsLineTerminator())
                {
                    throw new ArgumentException(SR.ArgumentException_NoWhiteSpaceOrLineTerminatorInShortName,
                        "shortName");
                }

                uniqueName = new string(_shortName.Value, 1);
            }

            LongName = longName;
            if (uniqueName != null)
            {
                UniqueName = uniqueName;
            }
            else
            {
                UniqueName = LongName;
            } 
        }

        #endregion

        #region public functions

        /// <summary>
        ///     Gets a short name of this command line option. You can use only one character.
        /// </summary>
        public virtual char? ShortName
        {
            get { return _shortName; }
            internal set { _shortName = value; }
        }

        /// <summary>
        ///     Gets long name of this command line option. This name is usually a single english word.
        /// </summary>
        public string LongName { get; internal set; }

        /// <summary>
        ///     Gets or sets the option's mutually exclusive set.
        /// </summary>
        public string MutuallyExclusiveSet
        {
            get { return _mutuallyExclusiveSet; }

            set { _mutuallyExclusiveSet = value == @"" ? DefaultMutuallyExclusiveSet : value; }
        }

        /// <summary>
        ///     Gets or sets a value indicating whether a command line option is required.
        /// </summary>
        public virtual bool Required { get; set; }

        /// <summary>
        ///     Gets or sets mapped property default value.
        /// </summary>
        public virtual object DefaultValue
        {
            get { return _defaultValue; }

            set
            {
                _defaultValue = value;
                HasDefaultValue = true;
            }
        }

        /// <summary>
        ///     Gets or sets mapped property meta value.
        /// </summary>
        public virtual string MetaValue
        {
            get { return _metaValue; }

            set
            {
                _metaValue = value;
                _hasMetaValue = !string.IsNullOrEmpty(_metaValue);
            }
        }

        /// <summary>
        ///     Gets or sets a short description of this command line option. Usually a sentence summary.
        /// </summary>
        public string? HelpText { get; set; }

        #endregion

        #region internal functions

        internal const string DefaultMutuallyExclusiveSet = "Default";

        internal string UniqueName { get; private set; }

        internal bool HasShortName
        {
            get { return _shortName != null; }
        }

        internal bool HasLongName
        {
            get { return !string.IsNullOrEmpty(LongName); }
        }

        internal bool HasDefaultValue { get; private set; }

        internal bool HasMetaValue
        {
            get { return _hasMetaValue; }
        }

        internal bool AutoLongName { get; set; }

        #endregion

        #region private fields

        private char? _shortName;
        private object _defaultValue = @"";
        private string _metaValue = @"";
        private bool _hasMetaValue;
        private string _mutuallyExclusiveSet = @"";

        #endregion
    }
}