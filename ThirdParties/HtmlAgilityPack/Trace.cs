// Description: Html Agility Pack - HTML Parsers, selectors, traversors, manupulators.
// Website & Documentation: http://html-agility-pack.net
// Forum & Issues: https://github.com/zzzdsSolutions/html-agility-pack
// License: https://github.com/zzzdsSolutions/html-agility-pack/blob/master/LICENSE
// More dsSolutions: http://www.zzzdsSolutions.com/
// Copyright © ZZZ DsSolutions Inc. 2014 - 2017. All rights reserved.

namespace HtmlAgilityPack
{
    internal partial class Trace
    {
        internal static Trace _current;

        internal static Trace Current
        {
            get
            {
                if (_current == null)
                    _current = new Trace();
                return _current;
            }
        }

        partial void WriteLineIntern(string message, string category);

        public static void WriteLine(string message, string category)
        {
            Current.WriteLineIntern(message, category);
        }
    }
}