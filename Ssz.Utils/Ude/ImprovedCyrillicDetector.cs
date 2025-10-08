using System;
using System.Collections.Generic;
using System.Linq;
using UtfUnknown.Core;

namespace Ssz.Utils.Ude;

public class ImprovedCyrillicDetector
{
    // Характерные двухбайтовые последовательности для русского языка в Windows-1251
    private static readonly Dictionary<byte[], double> RussianBigrams = new Dictionary<byte[], double>
    {
        { new byte[] { 0xF0, 0xEE }, 2.1 }, // "ро"
        { new byte[] { 0xEF, 0xF0 }, 1.8 }, // "пр"
        { new byte[] { 0xE5, 0xED }, 1.6 }, // "ен"
        { new byte[] { 0xF1, 0xF2 }, 1.4 }, // "ст"
        { new byte[] { 0xEE, 0xE2 }, 1.3 }, // "ов"
        { new byte[] { 0xED, 0xE8 }, 1.2 }, // "ни"
        { new byte[] { 0xE0, 0xED }, 1.1 }, // "ан"
        { new byte[] { 0xF2, 0xEE }, 1.0 }, // "то"
    };

    // Высокочастотные русские символы в Windows-1251
    private static readonly Dictionary<byte, double> RussianChars = new Dictionary<byte, double>
    {
        { 0xEE, 10.97 }, // о
        { 0xE5, 8.45 },  // е
        { 0xF0, 5.53 },  // р
        { 0xED, 6.78 },  // н
        { 0xF2, 6.26 },  // т
        { 0xE0, 8.01 },  // а
        { 0xE8, 7.35 },  // и
        { 0xEB, 4.40 },  // л
        { 0xF1, 5.47 },  // с
        { 0xE2, 4.62 },  // в
    };

    public static double CalculateWindows1251Confidence(byte[] data)
    {
        if (data == null || data.Length < 10) return 0.0;

        double confidence = 0.0;
        int totalBytes = data.Length;
        int cyrillicBytes = 0;

        // 1. Подсчет кириллических символов
        foreach (byte b in data)
        {
            if (IsCyrillicByte(b))
            {
                cyrillicBytes++;
                if (RussianChars.ContainsKey(b))
                {
                    confidence += RussianChars[b] / 100.0;
                }
            }
        }

        // 2. Анализ биграмм
        for (int i = 0; i < data.Length - 1; i++)
        {
            byte[] bigram = { data[i], data[i + 1] };

            foreach (var pattern in RussianBigrams)
            {
                if (pattern.Key.SequenceEqual(bigram))
                {
                    confidence += pattern.Value;
                }
            }
        }

        // 3. Проверка на валидность UTF-8 (инвертированная логика)
        if (!IsValidUtf8(data))
        {
            confidence += 0.3; // Если не UTF-8, то скорее всего Windows-1251
        }

        // 4. Нормализация результата
        double cyrillicRatio = (double)cyrillicBytes / totalBytes;
        if (cyrillicRatio > 0.05) // Если > 5% кириллицы
        {
            confidence *= (1.0 + cyrillicRatio);
        }

        return Math.Min(confidence / 10.0, 1.0); // Нормализация к [0,1]
    }

    private static bool IsCyrillicByte(byte b)
    {
        return b >= 192 && b <= 255; // Диапазон кириллицы в Windows-1251
    }

    private static bool IsValidUtf8(byte[] data)
    {
        try
        {
            System.Text.Encoding.UTF8.GetString(data);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
