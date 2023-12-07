using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ssz.Dcs.CentralServer.Common
{
    public static class OfficeHelper
    {
        public static MemoryStream GetMemoryStream(ExcelWorksheet worksheet)
        {
            MemoryStream memoryStream = new();

            StreamWriter sw = new StreamWriter(memoryStream, new UTF8Encoding(true)); // Does not close stream
            List<object?> rowValues = new();
            foreach (int row in Enumerable.Range(1, worksheet.Dimension.Rows))
            {
                rowValues.Clear();

                foreach (int column in Enumerable.Range(1, worksheet.Dimension.Columns))
                {
                    var cell = worksheet.Cells[row, column];
                    rowValues.Add(cell.Value);
                }

                string line = Ssz.Utils.CsvHelper.FormatForCsv(@",", rowValues);
                sw.WriteLine(line);
            }
            sw.Flush();

            memoryStream.Position = 0;

            return memoryStream;
        }
    }
}
