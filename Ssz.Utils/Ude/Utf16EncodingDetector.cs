using System;
using System.Collections.Generic;
using System.Text;

namespace Ssz.Utils.Ude;

public static class Utf16EncodingDetector
{
    // Простая эвристика для файлов без BOM (асимметрия 0 байтов на четных/нечетных)
    public static string? HeuristicDetectUtf16(byte[] data, out double score)
    {
        score = 0.0;
        if (data.Length < 6) return null;

        int evenZero = 0, oddZero = 0, total = Math.Min(data.Length, 1000);
        for (int i = 0; i < total - 1; i += 2)
        {
            if (data[i] == 0x00) evenZero++;
            if (data[i + 1] == 0x00) oddZero++;
        }
        double evenRate = (double)evenZero / (total / 2);
        double oddRate = (double)oddZero / (total / 2);

        // Типичная эвристика:
        // - для LE: много нулей на четных позициях встречается редко
        // - для BE: много нулей на нечетных позициях редко
        // - если хоть одна из этих величин > 0.5 — файл похож на UTF-16

        if (evenRate > 0.5 && oddRate < 0.1)
        {
            score = evenRate;
            return "utf-16BE";
        }
        if (oddRate > 0.5 && evenRate < 0.1)
        {
            score = oddRate;
            return "utf-16LE";
        }
        return null;
    }
}


//// Возвращает "utf-16le", "utf-16be" или null
//    public static string DetectUtf16Bom(byte[] data)
//    {
//        if (data.Length >= 2)
//        {
//            // UTF-16LE BOM: FF FE
//            if (data[0] == 0xFF && data[1] == 0xFE)
//                return "utf-16le";
//            // UTF-16BE BOM: FE FF
//            if (data[0] == 0xFE && data[1] == 0xFF)
//                return "utf-16be";
//        }
//        return null;
//    }