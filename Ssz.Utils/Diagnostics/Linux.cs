﻿using System;
using System.IO;
using System.Linq;

namespace Ssz.Utils.Diagnostics
{
    internal static class Linux {
        public static UInt64 GetTotalPhysicalMemory()     => GetBytesFromLine("MemTotal:");
        public static UInt64 GetAvailablePhysicalMemory() => GetBytesFromLine("MemFree:");
        public static UInt64 GetTotalVirtualMemory()      => GetBytesFromLine("SwapTotal:");
        public static UInt64 GetAvailableVirtualMemory()  => GetBytesFromLine("SwapFree:");

        private static String[] GetProcMemInfoLines() => File.ReadAllLines("/proc/meminfo");

        private static UInt64 GetBytesFromLine(String token)
        {
            const String KbToken = "kB";
            var memTotalLine = GetProcMemInfoLines().FirstOrDefault(x => x.StartsWith(token))?.Substring(token.Length);
            if (memTotalLine != null && memTotalLine.EndsWith(KbToken) && UInt64.TryParse(memTotalLine.Substring(0, memTotalLine.Length - KbToken.Length).Trim(), out var memKb))
                return memKb * 1024;
            throw new Exception();
        }
    }
}
