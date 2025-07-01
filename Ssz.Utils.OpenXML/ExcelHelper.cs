using OfficeOpenXml;
using OfficeOpenXml.Drawing;

namespace Ssz.Utils.OpenXML;

public static class ExcelHelper
{
    /// <summary>
    ///     <paramref name="source"> and <paramref name="target"> must be from same workbook.
    /// </summary>
    /// <param name="source"></param>
    /// <param name="target"></param>
    public static void CopySheetFully(ExcelWorksheet source, ExcelWorksheet target)
    {
        var dim = source.Dimension;
        if (dim == null) 
            return;

        // Значения, формулы и стили
        for (int row = dim.Start.Row; row <= dim.End.Row; row++)
        {
            for (int col = dim.Start.Column; col <= dim.End.Column; col++)
            {
                var srcCell = source.Cells[row, col];
                var tgtCell = target.Cells[row, col];

                tgtCell.Value = srcCell.Value;
                if (!string.IsNullOrEmpty(srcCell.Formula))
                    tgtCell.Formula = srcCell.Formula;

                tgtCell.StyleID = srcCell.StyleID;
            }
        }

        // Ширина колонок
        for (int col = dim.Start.Column; col <= dim.End.Column; col++)
        {
            target.Column(col).Width = source.Column(col).Width;
        }

        // Высота строк
        for (int row = dim.Start.Row; row <= dim.End.Row; row++)
        {
            target.Row(row).Height = source.Row(row).Height;
        }

        // Объединенные ячейки
        foreach (var merged in source.MergedCells)
        {
            target.Cells[merged].Merge = true;
        }

        // Условное форматирование
        //foreach (var cf in source.ConditionalFormatting)
        //{
        //    var addr = new ExcelAddress(cf.Address.Address);
        //    var newCf = target.ConditionalFormatting.AddRule(cf.Type, addr);
        //    newCf.Style = cf.Style;
        //    newCf.Formula = cf.Formula;
        //    newCf.Formula2 = cf.Formula2;
        //}

        // Копирование таблиц (ExcelTable)
        foreach (var table in source.Tables)
        {
            var addr = table.Address;
            var newTable = target.Tables.Add(target.Cells[addr.Address], table.Name + "_Copy");
            newTable.ShowHeader = table.ShowHeader;
            newTable.ShowFilter = table.ShowFilter;
            newTable.TableStyle = table.TableStyle;
            newTable.ShowTotal = table.ShowTotal;
        }

        // Копирование изображений (Drawings)
        foreach (var drawing in source.Drawings)
        {
            if (drawing is ExcelPicture pic)
            {
                var image = target.Drawings.AddPicture(pic.Name + "_Copy", pic.Image);
                image.SetPosition(pic.From.Row, pic.From.RowOff, pic.From.Column, pic.From.ColumnOff);
                image.SetSize(pic.Width, pic.Height);
            }
        }
    }
}
