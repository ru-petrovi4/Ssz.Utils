using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using Microsoft.VisualBasic.FileIO;
using Clipboard = System.Windows.Clipboard;
using DataFormats = System.Windows.DataFormats;
using IDataObject = System.Windows.IDataObject;

namespace Ssz.Utils
{
    public static class ClipboardHelper
    {
        #region public functions

        /// <summary>
        ///     result != null, arrays != null
        ///     Uses CultureHelper.SystemCultureInfo.
        /// </summary>
        /// <returns></returns>
        public static List<string[]> ParseClipboardData()
        {
            IDataObject dataObj = Clipboard.GetDataObject();
            if (dataObj is null) return new List<string[]>();

            object clipboardData = dataObj.GetData(DataFormats.CommaSeparatedValue);
            if (clipboardData != null)
            {
                string clipboardDataString = GetClipboardDataString(clipboardData);
                return CsvHelper.ParseCsv(CultureHelper.SystemCultureInfo.TextInfo.ListSeparator, clipboardDataString);
            }
            clipboardData = dataObj.GetData(DataFormats.Text);
            if (clipboardData != null)
            {
                string clipboardDataString = GetClipboardDataString(clipboardData);
                return CsvHelper.ParseCsv("\t", clipboardDataString);
            }

            return new List<string[]>();
        }

        /// <summary>
        ///     Uses CultureHelper.SystemCultureInfo
        /// </summary>
        /// <param name="data"></param>
        public static void SetClipboardData(List<string[]> data)
        {
            if (data is null || data.Count == 0) return;

            var sb1 = new StringBuilder();
            var sb2 = new StringBuilder();
            foreach (var row in data)
            {
                sb1.Append(CsvHelper.FormatForCsv(CultureHelper.SystemCultureInfo.TextInfo.ListSeparator, row));
                sb1.Append(Environment.NewLine);

                sb2.Append(CsvHelper.FormatForCsv("\t", row));
                sb2.Append(Environment.NewLine);
            }
            string clipboardData1 = sb1.ToString();
            string clipboardData2 = sb2.ToString();
            Clipboard.Clear();
            if (!String.IsNullOrEmpty(clipboardData1)) Clipboard.SetData(DataFormats.CommaSeparatedValue, clipboardData1);
            if (!String.IsNullOrEmpty(clipboardData2)) Clipboard.SetData(DataFormats.Text, clipboardData2);
        }

        #endregion

        #region private functions

        private static string GetClipboardDataString(object clipboardData)
        {
            var clipboardDataString = clipboardData as string;
            if (clipboardDataString != null) return clipboardDataString;
            // cannot convert to a string so try a MemoryStream
            var clipboardDataMemoryStream = clipboardData as MemoryStream;
            if (clipboardDataMemoryStream != null)
            {
                var sr = new StreamReader(clipboardDataMemoryStream);
                return sr.ReadToEnd();
            }
            return null;
        }        

        #endregion
    }
}