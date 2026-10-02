using Ssz.Utils;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.NetworkInformation;
using System.Text;

namespace Ssz.Dcs.CentralServer.Common.Helpers;

public static class ServerUrl
{
    public static string Resolve(string boundAddress)
    {
        // Kestrel может вернуть "https://[::]:5001", "https://0.0.0.0:5001", "https://*:5001", "https://+:5001"
        int schemeEnd = boundAddress.IndexOf("://", StringComparison.Ordinal) + 3;
        int portSep = boundAddress.LastIndexOf(':');
        string scheme = boundAddress[..(schemeEnd - 3)];
        string host = boundAddress[schemeEnd..portSep].Trim('[', ']');
        string port = boundAddress[(portSep + 1)..].TrimEnd('/');

        if (host is "*" or "+" or "0.0.0.0" or "::")
            host = ConfigurationHelper.GetFqdn();

        return $"{scheme}://{host}:{port}";
    }

    public static string ReplacePort(string url, int port)
    {
        // "https://[::]:5001", "https://0.0.0.0", "https://*:5001/", "http://+:80", "https://myhost:5001/path", "https://[::1]"
        int schemeSep = url.IndexOf("://", StringComparison.Ordinal);
        int authStart = schemeSep < 0 ? 0 : schemeSep + 3;
        int authEnd = url.IndexOfAny(['/', '?', '#'], authStart);
        if (authEnd < 0) authEnd = url.Length;

        string prefix = url[..authStart];            // "https://"
        string authority = url[authStart..authEnd];  // "[::]:5001", "0.0.0.0", "*:5001"
        string rest = url[authEnd..];                // "/path", "/", ""

        string host;
        if (authority.StartsWith('['))
        {
            int close = authority.IndexOf(']');
            if (close < 0) throw new FormatException($"Invalid IPv6 host in '{url}'.");
            host = authority[..(close + 1)];         // скобки сохраняем — без них IPv6 с портом невалиден
        }
        else
        {
            int colon = authority.LastIndexOf(':');
            host = colon < 0 ? authority : authority[..colon];
        }

        return $"{prefix}{host}:{port}{rest}";
    }
}
