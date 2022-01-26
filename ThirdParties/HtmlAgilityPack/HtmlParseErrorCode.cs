// Description: Html Agility Pack - HTML Parsers, selectors, traversors, manupulators.
// Website & Documentation: http://html-agility-pack.net
// Forum & Issues: https://github.com/zzzdsSolutions/html-agility-pack
// License: https://github.com/zzzdsSolutions/html-agility-pack/blob/master/LICENSE
// More dsSolutions: http://www.zzzdsSolutions.com/
// Copyright � ZZZ DsSolutions Inc. 2014 - 2017. All rights reserved.

namespace HtmlAgilityPack
{
    /// <summary>
    /// Represents the type of parsing error.
    /// </summary>
    public enum HtmlParseErrorCode
    {
        /// <summary>
        /// A tag was not closed.
        /// </summary>
        TagNotClosed,

        /// <summary>
        /// A tag was not opened.
        /// </summary>
        TagNotOpened,

        /// <summary>
        /// There is a charset mismatch between stream and declared (META) encoding.
        /// </summary>
        CharsetMismatch,

        /// <summary>
        /// An end tag was not required.
        /// </summary>
        EndTagNotRequired,

        /// <summary>
        /// An end tag is invalid at this position.
        /// </summary>
        EndTagInvalidHere
    }
}