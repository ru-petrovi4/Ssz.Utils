using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace Ssz.Utils.Wpf
{
    public static class ClipboardHelper
    {
        #region public functions

        /// <summary>        
        ///     Uses CultureHelper.SystemCultureInfo.
        /// </summary>
        /// <returns></returns>
        public static List<List<string?>> ParseClipboardData()
        {
            IDataObject dataObj = Clipboard.GetDataObject();
            if (dataObj is null) return new List<List<string?>>();

            object clipboardData = dataObj.GetData(DataFormats.CommaSeparatedValue);
            if (clipboardData is not null)
            {
                string clipboardDataString = GetClipboardDataString(clipboardData);
                return CsvHelper.ParseCsv(CultureInfo.CurrentCulture.TextInfo.ListSeparator, clipboardDataString);
            }
            clipboardData = dataObj.GetData(DataFormats.Text);
            if (clipboardData is not null)
            {
                string clipboardDataString = GetClipboardDataString(clipboardData);
                return CsvHelper.ParseCsv("\t", clipboardDataString);
            }

            return new List<List<string?>>();
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
                sb1.Append(CsvHelper.FormatForCsv(CultureInfo.CurrentCulture.TextInfo.ListSeparator, row));
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
            if (clipboardDataString is not null) return clipboardDataString;
            // cannot convert to a string so try a MemoryStream
            var clipboardDataMemoryStream = clipboardData as MemoryStream;
            if (clipboardDataMemoryStream is not null)
            {
                var sr = new StreamReader(clipboardDataMemoryStream);
                return sr.ReadToEnd();
            }
            return @"";
        }        

        #endregion
    }
}




///// <summary>
///// 
///// </summary>
///// <param name="text"></param>
///// <param name="cancellation"></param>
///// <returns></returns>
//public static async Task SetTextAsync(string text, CancellationToken cancellation)
//{
//    await TryOpenClipboardAsync(cancellation);

//    SetInternal(text);
//}

///// <summary>
///// 
///// </summary>
///// <param name="text"></param>
//public static void SetText(string text)
//{
//    TryOpenClipboard();

//    SetInternal(text);
//}

///// <summary>
///// 
///// </summary>
///// <param name="cancellation"></param>
///// <returns></returns>
//public static async Task<string?> GetTextAsync(CancellationToken cancellation)
//{
//    if (!IsClipboardFormatAvailable(cfUnicodeText))
//    {
//        return null;
//    }
//    await TryOpenClipboardAsync(cancellation);

//    return InnerGet();
//}

///// <summary>
///// 
///// </summary>
///// <returns></returns>
//public static string? GetText()
//{
//    if (!IsClipboardFormatAvailable(cfUnicodeText))
//    {
//        return null;
//    }
//    TryOpenClipboard();

//    return InnerGet();
//}


//private static void SetInternal(string text)
//{
//    EmptyClipboard();
//    IntPtr hGlobal = default;
//    try
//    {
//        var bytes = (text.Length + 1) * 2;
//        hGlobal = Marshal.AllocHGlobal(bytes);

//        if (hGlobal == default)
//        {
//            ThrowWin32();
//        }

//        var target = GlobalLock(hGlobal);

//        if (target == default)
//        {
//            ThrowWin32();
//        }

//        try
//        {
//            Marshal.Copy(text.ToCharArray(), 0, target, text.Length);
//        }
//        finally
//        {
//            GlobalUnlock(target);
//        }

//        if (SetClipboardData(cfUnicodeText, hGlobal) == default)
//        {
//            ThrowWin32();
//        }

//        hGlobal = default;
//    }
//    finally
//    {
//        if (hGlobal != default)
//        {
//            Marshal.FreeHGlobal(hGlobal);
//        }

//        CloseClipboard();
//    }
//}

//private static async Task TryOpenClipboardAsync(CancellationToken cancellation)
//{
//    var num = 10;
//    while (true)
//    {
//        if (OpenClipboard(default))
//        {
//            break;
//        }

//        if (--num == 0)
//        {
//            ThrowWin32();
//        }

//        await Task.Delay(100, cancellation);
//    }
//}

//private static void TryOpenClipboard()
//{
//    var num = 10;
//    while (true)
//    {
//        if (OpenClipboard(default))
//        {
//            break;
//        }

//        if (--num == 0)
//        {
//            ThrowWin32();
//        }

//        Thread.Sleep(100);
//    }
//}

//private static string? InnerGet()
//{
//    IntPtr handle = default;

//    IntPtr pointer = default;
//    try
//    {
//        handle = GetClipboardData(cfUnicodeText);
//        if (handle == default)
//        {
//            return null;
//        }

//        pointer = GlobalLock(handle);
//        if (pointer == default)
//        {
//            return null;
//        }

//        var size = GlobalSize(handle);
//        var buff = new byte[size];

//        Marshal.Copy(pointer, buff, 0, size);

//        return Encoding.Unicode.GetString(buff).TrimEnd('\0');
//    }
//    finally
//    {
//        if (pointer != default)
//        {
//            GlobalUnlock(handle);
//        }

//        CloseClipboard();
//    }
//}



//private const uint cfUnicodeText = 13;

//    private static void ThrowWin32()
//    {
//        throw new Win32Exception(Marshal.GetLastWin32Error());
//    }

//    [DllImport("User32.dll", SetLastError = true)]
//    [return: MarshalAs(UnmanagedType.Bool)]
//    static extern bool IsClipboardFormatAvailable(uint format);

//    [DllImport("User32.dll", SetLastError = true)]
//    static extern IntPtr GetClipboardData(uint uFormat);

//    [DllImport("kernel32.dll", SetLastError = true)]
//    static extern IntPtr GlobalLock(IntPtr hMem);

//    [DllImport("kernel32.dll", SetLastError = true)]
//    [return: MarshalAs(UnmanagedType.Bool)]
//    static extern bool GlobalUnlock(IntPtr hMem);

//    [DllImport("user32.dll", SetLastError = true)]
//    [return: MarshalAs(UnmanagedType.Bool)]
//    static extern bool OpenClipboard(IntPtr hWndNewOwner);

//    [DllImport("user32.dll", SetLastError = true)]
//    [return: MarshalAs(UnmanagedType.Bool)]
//    static extern bool CloseClipboard();

//    [DllImport("user32.dll", SetLastError = true)]
//    static extern IntPtr SetClipboardData(uint uFormat, IntPtr data);

//    [DllImport("user32.dll")]
//    static extern bool EmptyClipboard();

//    [DllImport("Kernel32.dll", SetLastError = true)]
//    static extern int GlobalSize(IntPtr hMem);
//}