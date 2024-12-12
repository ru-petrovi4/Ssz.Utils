using System;
using System.Collections.Generic;
using System.Linq;
using Ssz.Operator.Core.Constants;

using Ssz.Operator.Core.DsShapes;
using Ssz.Utils;

namespace Ssz.Operator.Core
{
    public static class DsContainerExtensions
    {
        #region public functions

        public static void TransformDsShapes(this IDsContainer container, double scaleX, double scaleY)
        {
            if (!double.IsNaN(scaleX) && !double.IsInfinity(scaleX) &&
                !double.IsNaN(scaleY) && !double.IsInfinity(scaleY) &&
                (scaleX != 1.0 || scaleY != 1.0))
                TransformDsShapesInternal(container.DsShapes, scaleX, scaleY);
        }

        public static DsConstant GetOrCreateDsConstant(this IDsContainer container, string constantName)
        {
            var dsConstant =
                container.DsConstantsCollection.FirstOrDefault(gpi =>
                    StringHelper.CompareIgnoreCase(gpi.Name, constantName));
            if (dsConstant is null)
            {
                dsConstant = new DsConstant
                {
                    Name = constantName
                };
                container.DsConstantsCollection.Add(dsConstant);
            }

            return dsConstant;
        }

        public static DsConstant? GetDsConstant(this IDsContainer container, string constantName)
        {
            return container.DsConstantsCollection.FirstOrDefault(gpi =>
                StringHelper.CompareIgnoreCase(gpi.Name, constantName));
        }

        public static IEnumerable<T> FindDsShapes<T>(this IDsContainer container,
            Func<T, bool>? additionalCheck = null)
            where T : DsShapeBase
        {
            var result = new List<T>();

            foreach (DsShapeBase dsShape in container.DsShapes)
            {
                var tDsShape = dsShape as T;
                if (tDsShape is not null)
                {
                    if (additionalCheck is null)
                        result.Add(tDsShape);
                    else if (additionalCheck(tDsShape)) result.Add(tDsShape);
                }

                var childContainer = dsShape as IDsContainer;
                if (childContainer is not null) 
                    result.AddRange(childContainer.FindDsShapes(additionalCheck));
            }

            return result;
        }

        public static DsShapeBase? FindDsShape(this IDsContainer? container, string path)
        {
            if (string.IsNullOrEmpty(path)) return null;
            var pathParts = path.Split(new[] {'/'}, StringSplitOptions.RemoveEmptyEntries);
            DsShapeBase? result = null;
            foreach (var pathPart in pathParts)
            {
                if (container is null || container.DsShapes.Length == 0) break;
                result = container.DsShapes.FirstOrDefault(s => StringHelper.CompareIgnoreCase(s.Name, pathPart));
                if (result is null) break;
                container = result as IDsContainer;
            }

            return result;
        }

        public static void SetDsShapesIndexes(this IDsContainer container)
        {
            if (container is null || container.DsShapes.Length == 0) return;
            var index = 0;
            foreach (DsShapeBase dsShape in container.DsShapes)
            {
                dsShape.Index = index;
                index += 1;
            }
        }

        public static void RefreshDsConstantsCollection(this IDsContainer container)
        {
            var constants = new HashSet<string>();
            container.FindConstants(constants);

            var newDsConstants = container.DsConstantsCollection.ToList();
            foreach (string s in constants)
            {
                string constant;
                string constantType;
                string constantDesc;
                if (!s.EndsWith(@")"))
                {
                    var i = s.IndexOf(")" + DataBindingItem.DataSourceStringSeparator);
                    if (i == -1) continue;
                    var typeStartIndex = i + DataBindingItem.DataSourceStringSeparator.Length + 1;
                    var j = s.IndexOf(DataBindingItem.DataSourceStringSeparator, typeStartIndex);
                    if (j == -1) continue;
                    var descStartIndex = j + DataBindingItem.DataSourceStringSeparator.Length;
                    constant = s.Substring(0, i + 1);
                    constantType = s.Substring(typeStartIndex, j - typeStartIndex);
                    constantDesc = s.Substring(descStartIndex);
                }
                else
                {
                    constant = s;
                    constantType = "";
                    constantDesc = "";
                }

                var dsConstant =
                    newDsConstants.FirstOrDefault(
                        gpi => StringHelper.CompareIgnoreCase(gpi.Name, constant));
                if (dsConstant is null)
                {
                    dsConstant = new DsConstant
                    {
                        Name = constant,
                        Type = constantType,
                        Desc = constantDesc
                    };
                    newDsConstants.Add(dsConstant);
                }
                else
                {
                    if (string.IsNullOrEmpty(dsConstant.Type)) dsConstant.Type = constantType;
                    if (string.IsNullOrEmpty(dsConstant.Desc)) dsConstant.Desc = constantDesc;
                }
            }

            ConstantsHelper.UpdateDsConstants(container.DsConstantsCollection,
                newDsConstants.OrderBy(gpi => gpi.Name).ToArray());
        }

        #endregion

        #region private functions

        private static void TransformDsShapesInternal(IEnumerable<DsShapeBase> dsShapes, double scaleX,
            double scaleY)
        {
            foreach (DsShapeBase dsShape in dsShapes)
            {
                dsShape.Transform(scaleX, scaleY);

                var complexDsShape = dsShape as ComplexDsShape;
                if (complexDsShape is not null)
                    TransformDsShapesInternal(complexDsShape.DsShapes, scaleX, scaleY);
            }
        }

        #endregion
    }
}