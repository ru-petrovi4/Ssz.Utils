using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using Ssz.Operator.Core.Addons;
using Ssz.Operator.Core.Constants;
using Ssz.Operator.Core.CustomAttributes;
using Ssz.Operator.Core.DsShapes;
using Ssz.Operator.Core.DsPageTypes;
using Ssz.Operator.Core.Properties;
//using Ssz.Operator.Core.VisualEditors.PropertyGridTypeEditors;
using Ssz.Operator.Core.Utils;
using Ssz.Operator.Core.Utils.Serialization;
using Ssz.Utils.MonitoredUndo;
//using Ssz.Xceed.Wpf.Toolkit.PropertyGrid.Attributes;
using Ssz.Utils;
using GuidAndName = Ssz.Operator.Core.Utils.GuidAndName;

namespace Ssz.Operator.Core.Drawings
{
    public class DsPageDrawing : DrawingBase
    {
        #region internal functions

        internal override void SetDrawingInfo(DrawingInfo drawingInfo)
        {
            base.SetDrawingInfo(drawingInfo);

            var dsPageDrawingInfo = drawingInfo as DsPageDrawingInfo;
            if (dsPageDrawingInfo is not null)
            {
                ExcludeFromTagSearch = dsPageDrawingInfo.ExcludeFromTagSearch;
                DsPageTypeGuidAndName = dsPageDrawingInfo.DsPageTypeInfo;
                _dsPageTypeObject = dsPageDrawingInfo.DsPageTypeObject;
            }
        }

        #endregion

        #region construction and destruction

        public DsPageDrawing() // For XAML serialization
            : this(true, true)
        {
        }


        public DsPageDrawing(bool visualDesignMode, bool loadXamlContent)
            : base(visualDesignMode, loadXamlContent)
        {
            var initialTypeGuid = GenericGraphicDsPageType.TypeGuid;
            DsPageTypeGuidAndName = new GuidAndName
            {
                Guid = initialTypeGuid
            };
            _dsPageTypeObject = AddonsHelper.NewDsPageTypeObject(initialTypeGuid);
            if (_dsPageTypeObject is not null)
                DsPageTypeGuidAndName.Name = _dsPageTypeObject.Name;

            _dsPageStretchMode = DsPageStretchMode.Default;
            _dsPageHorizontalAlignment = DsPageHorizontalAlignment.Default;
            _dsPageVerticalAlignment = DsPageVerticalAlignment.Default;
            _background = null;

            _underlyingDsXaml = new DsXaml();
            _underlyingDsXaml.ParentItem = this;
        }

        #endregion

        #region public functions

        public override bool IsFaceplate
        {
            get
            {
                if (_dsPageTypeObject is null) return false;
                return _dsPageTypeObject.IsFaceplate;
            }
        }

        [DsCategory(ResourceStrings.DrawingCategory)]
        [DsDisplayName(ResourceStrings.DsPageDrawing_Name)]
        [LocalizedDescription(ResourceStrings.DsPageDrawing_NameDescription)]
        //[ReadOnlyInEditor]
        //[PropertyOrder(1)]
        public override string Name
        {
            get => base.Name;
            set => base.Name = value;
        }

        [DsCategory(ResourceStrings.DrawingCategory)]
        [DsDisplayName(ResourceStrings.DsPageDrawing_Desc)]
        [LocalizedDescription(ResourceStrings.DsPageDrawing_DescDescription)]
        //[PropertyOrder(2)]
        public override string Desc
        {
            get => base.Desc;
            set => base.Desc = value;
        }

        [DsCategory(ResourceStrings.DrawingCategory)]
        [DsDisplayName(ResourceStrings.DsPageDrawing_Group)]
        [LocalizedDescription(ResourceStrings.DsPageDrawing_GroupDescription)]
        //[PropertyOrder(3)]
        public override string Group
        {
            get => base.Group;
            set => base.Group = value;
        }

        [DsCategory(ResourceStrings.DrawingCategory)]
        [DsDisplayName(ResourceStrings.DsPageStretchMode)]
        [LocalizedDescription(ResourceStrings.DsPageStretchModeDescription)]
        //[PropertyOrder(6)]
        public DsPageStretchMode StretchMode
        {
            get => _dsPageStretchMode;
            set => SetValue(ref _dsPageStretchMode, value);
        }

        [DsCategory(ResourceStrings.DrawingCategory)]
        [DsDisplayName(ResourceStrings.DsPageDrawing_HorizontalAlignment)]
        [LocalizedDescription(ResourceStrings.DsPageDrawing_HorizontalAlignment_Description)]
        //[PropertyOrder(7)]
        public DsPageHorizontalAlignment HorizontalAlignment
        {
            get => _dsPageHorizontalAlignment;
            set => SetValue(ref _dsPageHorizontalAlignment, value);
        }

        [DsCategory(ResourceStrings.DrawingCategory)]
        [DsDisplayName(ResourceStrings.DsPageDrawing_VerticalAlignment)]
        [LocalizedDescription(ResourceStrings.DsPageDrawing_VerticalAlignment_Description)]
        //[PropertyOrder(8)]
        public DsPageVerticalAlignment VerticalAlignment
        {
            get => _dsPageVerticalAlignment;
            set => SetValue(ref _dsPageVerticalAlignment, value);
        }

        [DsCategory(ResourceStrings.DrawingCategory)]
        [DsDisplayName(ResourceStrings.DsPageBackground)]
        [LocalizedDescription(ResourceStrings.DsPageBackgroundDescription)]
        //[Editor(typeof(SolidBrushOrNullTypeEditor), typeof(SolidBrushOrNullTypeEditor))]
        //[PropertyOrder(9)]
        public SolidDsBrush? Background
        {
            get => _background;
            set => SetValue(ref _background, value);
        }

        [DsCategory(ResourceStrings.DrawingCategory)]
        [DsDisplayName(ResourceStrings.DsPageDrawing_ExcludeFromTagSearch)]
        [LocalizedDescription(ResourceStrings.DsPageDrawing_ExcludeFromTagSearchDescription)]
        //[PropertyOrder(10)]
        public bool ExcludeFromTagSearch
        {
            get => _excludeFromTagSearch;
            set => SetValue(ref _excludeFromTagSearch, value);
        }

        [DsCategory(ResourceStrings.AppearanceCategory)]
        [DsDisplayName(ResourceStrings.DsPageDrawing_UnderlyingXaml)]
        //[Editor(typeof(XamlTypeEditor), typeof(XamlTypeEditor))]
        public DsXaml UnderlyingXaml
        {
            get => _underlyingDsXaml;
            set
            {
                if (LoadXamlContent)
                {
                    if (value is null) value = new DsXaml();
                    SetValue(ref _underlyingDsXaml, value);
                }
            }
        }

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)] // For XAML serialization
        public GuidAndName DsPageTypeGuidAndName { get; private set; }

        [DsCategory(ResourceStrings.DsPageTypeCategory)]
        [DsDisplayName(ResourceStrings.DsPageDrawing_DsPageTypeGuid)]
        //[ItemsSource(typeof(DsPageTypeGuid_ItemsSource))]
        public Guid DsPageTypeGuid
        {
            get => DsPageTypeGuidAndName.Guid;
            set
            {
                if (value == DsPageTypeGuidAndName.Guid) return;

                if (VisualDesignMode)
                    DefaultChangeFactory.Instance.OnChanging(this, @"TypeGuid", DsPageTypeGuidAndName.Guid,
                        value);

                if (DsPageTypeObject is not null)
                    _cacheDsPageTypeObjectsDictionary[DsPageTypeGuidAndName.Guid] = DsPageTypeObject;

                DsPageTypeGuidAndName.Guid = value;

                DsPageTypeBase? existingStyleObject;
                if (_cacheDsPageTypeObjectsDictionary.TryGetValue(DsPageTypeGuidAndName.Guid,
                    out existingStyleObject))
                {
                    DsPageTypeObject = existingStyleObject;
                    _cacheDsPageTypeObjectsDictionary.Remove(DsPageTypeGuidAndName.Guid);
                }
                else
                {
                    DsPageTypeObject = AddonsHelper.NewDsPageTypeObject(DsPageTypeGuidAndName.Guid);
                }

                if (DsPageTypeObject is not null)
                    DsPageTypeGuidAndName.Name = DsPageTypeObject.Name;
                else
                    DsPageTypeGuidAndName.Name = null;

                OnDrawingPositionInTreeChanged();
            }
        }

        [DsCategory(ResourceStrings.DsPageTypeCategory)]
        [DsDisplayName(ResourceStrings.DsPageDrawing_DsPageTypeObject)]
        //[Editor(typeof(CloneableObjectTypeEditor), typeof(CloneableObjectTypeEditor))]
        public DsPageTypeBase? DsPageTypeObject
        {
            get => _dsPageTypeObject;
            set
            {
                if (SetValue(ref _dsPageTypeObject, value)) OnDrawingHeaderChanged();
            }
        }

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)] // For XAML serialization
        public override DsConstant[]? HiddenDsConstantsCollection
        {
            get
            {
                _hiddenDsConstantsCollection[0] = new DsConstant(@"%(PageName)", Name);
                _hiddenDsConstantsCollection[1] =
                    new DsConstant(@"%(PageDesc)", !string.IsNullOrEmpty(Desc) ? Desc : " ");
                _hiddenDsConstantsCollection[2] =
                    new DsConstant(@"%(PageGroup)", !string.IsNullOrEmpty(Group) ? Group : " ");
                return _hiddenDsConstantsCollection;
            }
        }

        public override string ToString()
        {
            return Resources.DsPageDrawing;
        }

        public override byte[] GetBytes(bool full)
        {
            using (var memoryStream = new MemoryStream(1024 * 1024))
            {
                using (var writer = new SerializationWriter(memoryStream))
                {
                    writer.Write((int) StretchMode);
                    writer.Write((int) HorizontalAlignment);
                    writer.Write((int) VerticalAlignment);
                    writer.Write(ExcludeFromTagSearch);

                    if (full)
                    {
                        if (Background is not null) writer.Write(Background, SerializationContext.FullBytes);

                        writer.Write(Desc);
                        writer.Write(Group);
                        writer.WriteListOfOwnedDataSerializable(DsConstantsCollection.OrderBy(gpi => gpi.Name).ToList(),
                            SerializationContext.FullBytes);
                        writer.Write(DsPageTypeGuidAndName.Guid);
                        writer.WriteNullableOwnedData(DsPageTypeObject, SerializationContext.FullBytes);

                        writer.Write(Mark);

                        writer.Write(_underlyingDsXaml.XamlWithRelativePaths);

                        writer.WriteDsShapes(DsShapes, SerializationContext.FullBytes);
                    }
                    else
                    {
                        if (Background is not null) writer.Write(Background, SerializationContext.ShortBytes);

                        writer.WriteHashOfXaml(_underlyingDsXaml.XamlWithRelativePaths, DrawingFilesDirectoryFullName);

                        writer.WriteDsShapes(DsShapes, SerializationContext.ShortBytes);
                    }

                    writer.Write(Math.Round(Width, 1));
                    writer.Write(Math.Round(Height, 1));
                }

                return memoryStream.ToArray();
            }
        }

        public override void SerializeOwnedData(SerializationWriter writer, object? context)
        {
            using (writer.EnterBlock(2))
            {
                base.SerializeOwnedData(writer, context);

                writer.Write((int) StretchMode);
                writer.Write((int) HorizontalAlignment);
                writer.Write((int) VerticalAlignment);
                writer.WriteObject(Background);
                writer.Write(ExcludeFromTagSearch);
                writer.Write(_underlyingDsXaml.XamlWithRelativePaths);

                writer.Write(DsPageTypeGuidAndName, context);
                writer.WriteNullableOwnedData(DsPageTypeObject, context);
            }
        }

        public override void DeserializeOwnedDataAsync(SerializationReader reader, object? context)
        {
            using (Block block = reader.EnterBlock())
            {
                switch (block.Version)
                {
                    case 2:
                        base.DeserializeOwnedDataAsync(reader, context);

                        StretchMode = (DsPageStretchMode) reader.ReadInt32();
                        HorizontalAlignment = (DsPageHorizontalAlignment) reader.ReadInt32();
                        VerticalAlignment = (DsPageVerticalAlignment) reader.ReadInt32();
                        Background = reader.ReadObject<SolidDsBrush>();
                        ExcludeFromTagSearch = reader.ReadBoolean();
                        if (LoadXamlContent) _underlyingDsXaml.XamlWithRelativePaths = reader.ReadString();
                        else reader.SkipString();

                        reader.ReadOwnedData(DsPageTypeGuidAndName, context);
                        DsPageTypeObject = AddonsHelper.NewDsPageTypeObject(DsPageTypeGuidAndName.Guid);
                        reader.ReadNullableOwnedData(DsPageTypeObject, context);
                        break;
                    default:
                        throw new BlockUnsupportedVersionException();
                }
            }
        }

        public override void RefreshForPropertyGrid(IDsContainer? container)
        {
            base.RefreshForPropertyGrid(this);

            foreach (DsShapeBase dsShape in DsShapes) dsShape.RefreshForPropertyGrid(this);
        }

        public override void FindConstants(HashSet<string> constants)
        {
            base.FindConstants(constants);

            var item = DsPageTypeObject as IDsItem;
            if (item is not null) 
                item.FindConstants(constants);
        }

        public override void ReplaceConstants(IDsContainer? container)
        {
            base.ReplaceConstants(container);

            var item = DsPageTypeObject as IDsItem;
            if (item is not null) item.ReplaceConstants(container);
        }

        public override DrawingInfo GetDrawingInfo()
        {
            return new DsPageDrawingInfo(
                FileFullName, 
                Guid, 
                Desc,
                Group, PreviewImageBytes,
                SerializationVersionDateTime,
                DsConstantsCollection.ToArray(), 
                Mark, 
                ActuallyUsedAddonsInfo, 
                ExcludeFromTagSearch,
                DsPageTypeGuidAndName,
                _dsPageTypeObject);
        }

        public override object Clone()
        {
            var clone = this.CloneUsingSerialization(() => new DsPageDrawing(VisualDesignMode, LoadXamlContent));
            clone.SetDrawingInfo(GetDrawingInfo());
            return clone;
        }

        #endregion

        #region private fields

        private readonly DsConstant[] _hiddenDsConstantsCollection = new DsConstant[3];

        private DsPageTypeBase? _dsPageTypeObject;

        private readonly Dictionary<Guid, DsPageTypeBase> _cacheDsPageTypeObjectsDictionary =
            new();

        private DsXaml _underlyingDsXaml;

        private DsPageStretchMode _dsPageStretchMode;
        private DsPageHorizontalAlignment _dsPageHorizontalAlignment;
        private DsPageVerticalAlignment _dsPageVerticalAlignment;
        private SolidDsBrush? _background;
        private bool _excludeFromTagSearch;

        #endregion
    }

    public enum DsPageStretchMode
    {
        Default = -1,
        None = 0,
        Fill = 1,
        Uniform = 2,
        UniformToFill = 3
    }

    public enum DsPageHorizontalAlignment
    {
        Default = -1,        
        Left = 0,
        Center = 1,
        Right = 2
    }

    public enum DsPageVerticalAlignment
    {
        Default = -1,        
        Top = 0,
        Center = 1,
        Bottom = 2
    }

    //public class DsPageTypeGuid_ItemsSource : IItemsSource
    //{
    //    #region public functions

    //    public ItemCollection GetValues()
    //    {
    //        var itemCollection = new ItemCollection();
    //        foreach (AddonBase addon in AddonsHelper.AddonsCollection.ObservableCollection)
    //        {
    //            var dsPageTypes = addon.GetDsPageTypes();
    //            if (dsPageTypes is not null)
    //                foreach (DsPageTypeBase dsPageType in dsPageTypes)
    //                    itemCollection.Add(dsPageType.Guid, dsPageType.Name);
    //        }

    //        return itemCollection;
    //    }

    //    #endregion
    //}
}