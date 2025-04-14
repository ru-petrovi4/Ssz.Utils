using System;

namespace Ssz.Operator.Core
{
    public partial class DsProject
    {
        private static class Utils
        {
            #region public functions

            public static string GetFormat(double min, double max)
            {
                if (double.IsNaN(min) || double.IsNaN(max)) return "G4";
                var delta = max - min;
                if (delta > 9999999 || delta < 0.000001) return "G4";
                if (delta < 1)
                {
                    var nAfter = -(int) (Math.Log10(delta) - 1);
                    switch (nAfter)
                    {
                        case 1:
                            return "0.0000";
                        case 2:
                            return "0.00000";
                        case 3:
                            return "0.000000";
                        case 4:
                            return "0.0000000";
                    }

                    return "G4";
                }
                else
                {
                    var nBefore = (int) (Math.Log10(delta) + 1);
                    var nAfter = 4 - nBefore;
                    if (nAfter <= 0) return "######0";
                    switch (nAfter)
                    {
                        case 0:
                            return "######0";
                        case 1:
                            return "######0.0";
                        case 2:
                            return "######0.00";
                        case 3:
                            return "######0.000";
                        case 4:
                            return "0.0000";
                    }

                    return "G4";
                }
            }

            #endregion
        }
    }
}