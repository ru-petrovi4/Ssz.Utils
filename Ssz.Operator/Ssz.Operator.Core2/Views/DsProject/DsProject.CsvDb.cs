using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Threading;
using Microsoft.Extensions.Logging.Abstractions;
using Ssz.Operator.Core.Constants;
using Ssz.Operator.Core.Drawings;
using Ssz.Operator.Core.Properties;
using Ssz.Operator.Core.Utils;
using Ssz.Utils;

namespace Ssz.Operator.Core
{
    public partial class DsProject
    {
        #region public functions        

        [Browsable(false)] 
        public CsvDb CsvDb { get; private set; } = new(NullLogger<CsvDb>.Instance); 

        public ConstantValueViewModel[] GetConstantValuesForDropDownList(string? constantType)
        {
            if (!IsInitialized) return new ConstantValueViewModel[0];

            if (string.IsNullOrWhiteSpace(constantType)) return new ConstantValueViewModel[0];

            constantType = constantType!.Trim();

            var constantValues = _constantValuesDictionary.TryGetValue(constantType);
            if (constantValues is null)
            {
                var constantValuesList = new List<ConstantValueViewModel>();

                switch (constantType.ToUpperInvariant())
                {
                    case "COLOR":
                        break;
                    case "DSPAGE":
                        foreach (
                            DsPageDrawing d in AllDsPagesCache.Values)
                        {
                            var gnericParamValueAndDesc = new ConstantValueViewModel
                            {
                                Value = d.Name,
                                Desc = d.Desc
                            };
                            constantValuesList.Add(gnericParamValueAndDesc);
                        }

                        break;                    
                    default:
                        if (constantType.IndexOfAny(new[] {'\\', '/', '|'}) >= 0)
                        {
                            constantValuesList.AddRange(
                                constantType.Split(new[] {'\\', '/', '|'}, StringSplitOptions.RemoveEmptyEntries)
                                    .Select(s => new ConstantValueViewModel {Value = s})
                            );
                        }
                        else
                        {
                            var fileData = CsvDb.GetData(constantType);
                            var allConstantsValues = AllDsPagesCacheGetAllConstantsValues();

                            foreach (var fields in fileData.Values)
                                if (fields.Count > 0)
                                {
                                    var constantValueAndDesc = new ConstantValueViewModel
                                    {
                                        Value = fields[0] ?? ""
                                    };
                                    if (fields.Count > 1) constantValueAndDesc.Desc = fields[1] ?? "";

                                    var pars = allConstantsValues.TryGetValue(fields[0] ?? "");
                                    if (pars is not null)
                                        foreach (var par in pars)
                                            if (StringHelper.CompareIgnoreCase(par.DsConstant.Type, constantType))
                                                constantValueAndDesc.IncrementUseCount();

                                    constantValuesList.Add(constantValueAndDesc);
                                }
                        }

                        break;
                }

                constantValues = constantValuesList.ToArray();

                _constantValuesDictionary[constantType] = constantValues;
            }

            return constantValues;
        }

        #endregion

        #region private functions

        private object? GetVariableValue(string? value)
        {
            if (string.IsNullOrEmpty(value)) return value;

            return Any.ConvertToBestType(value, false).ValueAsObject();
        }

        #endregion        
    }
}