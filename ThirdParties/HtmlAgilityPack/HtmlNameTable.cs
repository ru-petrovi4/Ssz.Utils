// Description: Html Agility Pack - HTML Parsers, selectors, traversors, manupulators.
// Website & Documentation: http://html-agility-pack.net
// Forum & Issues: https://github.com/zzzdsSolutions/html-agility-pack
// License: https://github.com/zzzdsSolutions/html-agility-pack/blob/master/LICENSE
// More dsSolutions: http://www.zzzdsSolutions.com/
// Copyright � ZZZ DsSolutions Inc. 2014 - 2017. All rights reserved.

using System.Xml;

namespace HtmlAgilityPack
{
    internal class HtmlNameTable : XmlNameTable
    {
        #region Fields

        private NameTable _nametable = new NameTable();

        #endregion

        #region Public Methods

        public override string Add(string array)
        {
            return _nametable.Add(array);
        }

        public override string Add(char[] array, int offset, int length)
        {
            return _nametable.Add(array, offset, length);
        }

        public override string Get(string array)
        {
            return _nametable.Get(array);
        }

        public override string Get(char[] array, int offset, int length)
        {
            return _nametable.Get(array, offset, length);
        }

        #endregion

        #region Internal Methods

        internal string GetOrAdd(string array)
        {
            string s = Get(array);
            if (s == null)
            {
                return Add(array);
            }

            return s;
        }

        #endregion
    }
}