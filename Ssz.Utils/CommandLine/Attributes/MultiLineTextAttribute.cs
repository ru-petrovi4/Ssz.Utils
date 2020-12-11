using System;
using System.Text;
using Ssz.Utils.CommandLine.Infrastructure;
using Ssz.Utils.CommandLine.Text;

namespace Ssz.Utils.CommandLine.Attributes
{
    /// <summary>
    ///     Provides base properties for creating an attribute, used to define multiple lines of text.
    /// </summary>
    public abstract class MultilineTextAttribute : Attribute
    {
        #region construction and destruction

        /// <summary>
        ///     Initializes a new instance of the <see cref="MultilineTextAttribute" /> class. Used in derived type
        ///     using one line of text.
        /// </summary>
        /// <param name="line1">The first line of text.</param>
        protected MultilineTextAttribute(string line1)
        {
            _line1 = line1;
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="MultilineTextAttribute" /> class. Used in  type
        ///     using two lines of text.
        /// </summary>
        /// <param name="line1">The first line of text.</param>
        /// <param name="line2">The second line of text.</param>
        protected MultilineTextAttribute(string line1, string line2)
            : this(line1)
        {
            _line2 = line2;
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="MultilineTextAttribute" /> class. Used in  type
        ///     using three lines of text.
        /// </summary>
        /// <param name="line1">The first line of text.</param>
        /// <param name="line2">The second line of text.</param>
        /// <param name="line3">The third line of text.</param>
        protected MultilineTextAttribute(string line1, string line2, string line3)
            : this(line1, line2)
        {
            _line3 = line3;
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="MultilineTextAttribute" /> class. Used in type
        ///     using four lines of text.
        /// </summary>
        /// <param name="line1">The first line of text.</param>
        /// <param name="line2">The second line of text.</param>
        /// <param name="line3">The third line of text.</param>
        /// <param name="line4">The fourth line of text.</param>
        protected MultilineTextAttribute(string line1, string line2, string line3, string line4)
            : this(line1, line2, line3)
        {
            _line4 = line4;
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="MultilineTextAttribute" /> class. Used in type
        ///     using five lines of text.
        /// </summary>
        /// <param name="line1">The first line of text.</param>
        /// <param name="line2">The second line of text.</param>
        /// <param name="line3">The third line of text.</param>
        /// <param name="line4">The fourth line of text.</param>
        /// <param name="line5">The fifth line of text.</param>
        protected MultilineTextAttribute(string line1, string line2, string line3, string line4, string line5)
            : this(line1, line2, line3, line4)
        {
            _line5 = line5;
        }

        #endregion

        #region public functions

        /// <summary>
        ///     Gets the all non-blank lines as string.
        /// </summary>
        /// <value>A string of all non-blank lines.</value>
        public virtual string Value
        {
            get
            {
                var value = new StringBuilder(string.Empty);
                var strArray = new[] {_line1, _line2, _line3, _line4, _line5};

                for (int i = 0; i < GetLastLineWithText(strArray); i++)
                {
                    value.AppendLine(strArray[i]);
                }

                return value.ToString();
            }
        }

        /// <summary>
        ///     Gets the first line of text.
        /// </summary>
        public string Line1
        {
            get { return _line1; }
        }

        /// <summary>
        ///     Gets the second line of text.
        /// </summary>
        public string Line2
        {
            get { return _line2; }
        }

        /// <summary>
        ///     Gets third line of text.
        /// </summary>
        public string Line3
        {
            get { return _line3; }
        }

        /// <summary>
        ///     Gets the fourth line of text.
        /// </summary>
        public string Line4
        {
            get { return _line4; }
        }

        /// <summary>
        ///     Gets the fifth line of text.
        /// </summary>
        public string Line5
        {
            get { return _line5; }
        }

        #endregion

        #region internal functions

        internal void AddToHelpText(Action<string> action)
        {
            var strArray = new[] {_line1, _line2, _line3, _line4, _line5};
            Array.ForEach(
                strArray,
                line =>
                {
                    if (!string.IsNullOrEmpty(line))
                    {
                        action(line);
                    }
                });
        }

        internal void AddToHelpText(HelpText helpText, bool before)
        {
            // before flag only distinguishes which action is called, 
            // so refactor common code and call with appropriate action
            if (before)
            {
                AddToHelpText(helpText.AddPreOptionsLine);
            }
            else
            {
                AddToHelpText(helpText.AddPostOptionsLine);
            }
        }

        #endregion

        #region protected functions

        /// <summary>
        ///     Returns the last line with text. Preserves blank lines if user intended by skipping a line.
        /// </summary>
        /// <returns>
        ///     The last index of line of the non-blank line.
        /// </returns>
        /// <param name='value'>The string array to process.</param>
        protected virtual int GetLastLineWithText(string[] value)
        {
            int index = Array.FindLastIndex(value, str => !string.IsNullOrEmpty(str));

            // remember FindLastIndex returns zero-based index
            return index + 1;
        }

        #endregion

        #region private fields

        private readonly string _line1;
        private readonly string _line2 = @"";
        private readonly string _line3 = @"";
        private readonly string _line4 = @"";
        private readonly string _line5 = @"";

        #endregion
    }
}