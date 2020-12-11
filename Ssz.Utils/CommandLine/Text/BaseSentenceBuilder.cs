
namespace Ssz.Utils.CommandLine.Text
{
    /// <summary>
    ///     Models an abstract sentence builder.
    /// </summary>
    public abstract class BaseSentenceBuilder
    {
        #region public functions

        /// <summary>
        ///     Creates the built in sentence builder.
        /// </summary>
        /// <returns>The built in sentence builder.</returns>
        public static BaseSentenceBuilder CreateBuiltIn()
        {
            return new EnglishSentenceBuilder();
        }

        /// <summary>
        ///     Gets a string containing word 'option'.
        /// </summary>
        /// <value>The word 'option'.</value>
        public abstract string OptionWord { get; }

        /// <summary>
        ///     Gets a string containing the word 'and'.
        /// </summary>
        /// <value>The word 'and'.</value>
        public abstract string AndWord { get; }

        /// <summary>
        ///     Gets a string containing the sentence 'required option missing'.
        /// </summary>
        /// <value>The sentence 'required option missing'.</value>
        public abstract string RequiredOptionMissingText { get; }

        /// <summary>
        ///     Gets a string containing the sentence 'violates format'.
        /// </summary>
        /// <value>The sentence 'violates format'.</value>
        public abstract string ViolatesFormatText { get; }

        /// <summary>
        ///     Gets a string containing the sentence 'violates mutual exclusiveness'.
        /// </summary>
        /// <value>The sentence 'violates mutual exclusiveness'.</value>
        public abstract string ViolatesMutualExclusivenessText { get; }

        /// <summary>
        ///     Gets a string containing the error heading text.
        /// </summary>
        /// <value>The error heading text.</value>
        public abstract string ErrorsHeadingText { get; }

        #endregion
    }
}