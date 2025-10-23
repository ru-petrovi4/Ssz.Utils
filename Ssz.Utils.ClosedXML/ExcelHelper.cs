using ClosedXML.Excel;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Ssz.Utils.ClosedXML;

public static class ExcelHelper
{    
    public static byte[] GetAsByteArray(this XLWorkbook workbook)
    {
        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    public static string MakeValidSheetName(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return "Sheet";

        // Удаляем пробелы в начале и конце
        string name = input!.Trim();

        // Удаляем или заменяем недопустимые символы (: \ / ? * [ ])
        char[] invalidChars = { ':', '\\', '/', '?', '*', '[', ']' };
        foreach (char c in invalidChars)
            name = name.Replace(c, '_');

        // Excel не допускает, чтобы имя начиналось или заканчивалось апострофом
        name = name.Trim('\'');

        // Сжимаем множественные пробелы
        name = Regex.Replace(name, @"\s{2,}", " ");

        // Если после очистки ничего не осталось
        if (string.IsNullOrEmpty(name))
            name = "Sheet";

        // Ограничиваем длину 31 символом
        if (name.Length > 31)
            name = name.Substring(0, 31);

        return name;
    }

    public static void AdjustToContents(this IXLWorksheet worksheet)
    {
        // Включаем перенос текста для всех ячеек
        worksheet.Cells().Style.Alignment.WrapText = true;
        worksheet.Cells().Style.Alignment.Vertical = XLAlignmentVerticalValues.Top;

        // Ограничиваем максимальную ширину колонок (например, 40)
        foreach (var col in worksheet.ColumnsUsed())
        {
            col.AdjustToContents();
            if (col.Width > 80)
                col.Width = 80;
        }

        // Настраиваем высоту строк под содержимое (учитывает WrapText)        
        foreach (var row in worksheet.RowsUsed())
        {
            row.AdjustToContents();
            row.ClearHeight();
        }
    }

    public static string? GetCellValueForCsv(IXLCell cell)
    {
        if (cell.Value.IsBlank)
        {
            return null;
        }
        else if (cell.Value.IsText)
        {
            string stringValue = cell.Value.GetText();
            stringValue = stringValue.Replace('\n', ' ');
            stringValue = stringValue.Replace('\r', ' ');
            return stringValue;
        }
        else
        {
            return cell.Value.ToString(CultureInfo.InvariantCulture);
        }
    }

    public static MemoryStream GetCsvMemoryStream(IXLWorksheet worksheet)
    {
        MemoryStream memoryStream = new();

        StreamWriter sw = new StreamWriter(memoryStream, new UTF8Encoding(true)); // Does not close stream
        List<string?> rowValues = new();
        IXLRange? usedRange = worksheet.RangeUsed();
        if (usedRange is not null)
            foreach (int row in Enumerable.Range(usedRange.RangeAddress.FirstAddress.RowNumber, usedRange.RowCount()))
            {
                rowValues.Clear();

                foreach (int column in Enumerable.Range(usedRange.RangeAddress.FirstAddress.ColumnNumber, usedRange.ColumnCount()))
                {
                    var cell = worksheet.Cell(row, column);
                    rowValues.Add(GetCellValueForCsv(cell));
                }

                string line = Ssz.Utils.CsvHelper.FormatForCsv(@",", rowValues);
                sw.WriteLine(line);
            }
        sw.Flush();

        memoryStream.Position = 0;

        return memoryStream;
    }
}
