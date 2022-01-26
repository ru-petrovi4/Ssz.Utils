// Description: Html Agility Pack - HTML Parsers, selectors, traversors, manupulators.
// Website & Documentation: http://html-agility-pack.net
// Forum & Issues: https://github.com/zzzdsSolutions/html-agility-pack
// License: https://github.com/zzzdsSolutions/html-agility-pack/blob/master/LICENSE
// More dsSolutions: http://www.zzzdsSolutions.com/
// Copyright © ZZZ DsSolutions Inc. 2014 - 2017. All rights reserved.

namespace HtmlAgilityPack
{
    partial class Trace
    {
        partial void WriteLineIntern(string message, string category)
        {
            System.Diagnostics.Debug.WriteLine(message, category);
        }
    }
}