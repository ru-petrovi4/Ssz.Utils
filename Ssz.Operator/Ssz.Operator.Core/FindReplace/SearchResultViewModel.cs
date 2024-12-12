using System;
using System.Collections.Generic;
using System.Linq;
using Ssz.Operator.Core.Drawings;
using Ssz.Operator.Core.DsShapes;
using Ssz.Utils;
using Ssz.Utils.Wpf;

namespace Ssz.Operator.Core.FindReplace
{
    public class SearchResultViewModel : ViewModelBase
    {
        #region private fields

        private string[] _propertyValues;

        #endregion

        #region construction and destruction

        public SearchResultViewModel(DrawingInfo drawingInfo, DsShapeInfo? rootDsShapeInfo,
            DsShapeInfo? dsShapeInfo,
            string propertyPath, string[] propertyValues)
        {
            DrawingInfo = drawingInfo;
            RootDsShapeInfo = rootDsShapeInfo;
            DsShapeInfo = dsShapeInfo;
            PropertyPath = propertyPath.Split('.').LastOrDefault() ?? "";
            _propertyValues = propertyValues;
        }

        #endregion

        #region public functions

        public string Header
        {
            get
            {
                if (RootDsShapeInfo is not null)
                    return DrawingInfo.Name + " \t[" + RootDsShapeInfo.DsShapeTypeNameToDisplay + "] " +
                           PropertyPath + " = \t" +
                           string.Join(" \t", PropertyValues);
                return "Drawing " + PropertyPath + " = \t" +
                       string.Join(" \t", PropertyValues);
            }
        }

        public string DsShapeTypeNameToDisplay
        {
            get
            {
                if (RootDsShapeInfo is not null) return RootDsShapeInfo.DsShapeTypeNameToDisplay;
                return @"";
            }
        }

        public string DsShapeName
        {
            get
            {
                if (RootDsShapeInfo is not null) return RootDsShapeInfo.Name;
                return @"";
            }
        }


        public DrawingInfo DrawingInfo { get; }


        public DsShapeInfo? RootDsShapeInfo { get; }


        public DsShapeInfo? DsShapeInfo { get; }

        public string PropertyPath { get; }


        public string[] PropertyValues
        {
            get => _propertyValues;
            set
            {
                if (SetValue(ref _propertyValues, value)) OnPropertyChanged(@"Header");
            }
        }

        public SearchResultGroupViewModel? ParentGroup { get; set; }

        #endregion
    }

    public class SearchResultViewModelEqualityComparer : IEqualityComparer<SearchResultViewModel>
    {
        #region public functions

        public static readonly SearchResultViewModelEqualityComparer Instance =
            new();

        public bool Equals(SearchResultViewModel? x, SearchResultViewModel? y)
        {
            if (ReferenceEquals(x, y)) return true;
            if (x is null || y is null) return false;
            if (x.Header != y.Header) return false;
            if (x.RootDsShapeInfo is null && y.RootDsShapeInfo is null) return true;
            if (x.RootDsShapeInfo is null || y.RootDsShapeInfo is null) return false;
            return x.Header == y.Header && x.RootDsShapeInfo.Index == y.RootDsShapeInfo.Index;
        }

        public int GetHashCode(SearchResultViewModel obj)
        {
            return obj.Header.GetHashCode();
        }

        #endregion
    }
}