using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace Ssz.Utils
{
    public static class TextFileHelper
    {
        /// <summary>
        ///     Output string contains only \n symbol.
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        [return: NotNullIfNotNull("text")]
        public static string? NormalizeNewLine(string? text)
        {
            if (String.IsNullOrEmpty(text))
                return text;
            return text!
                .Replace("\r\n", "\n")                
                .Replace('\r', '\n');
        }
    }
}
