using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using Avalonia;
using Avalonia.Markup;
using Ssz.Operator.Core.Constants;
using Ssz.Operator.Core.CustomAttributes;
using Ssz.Operator.Core.Drawings;
using Ssz.Operator.Core.Properties;
//using Ssz.Operator.Core.VisualEditors.PropertyGridTypeEditors.DsConstantsCollection;
using Ssz.Operator.Core.Utils.Serialization;
using Ssz.Utils;
using Ssz.Utils.MonitoredUndo;
//using Ssz.Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

namespace Ssz.Operator.Core.DsShapes
{
    //[ContentProperty(@"DsShapesArray")] // For XAML serialization. Content property must be of type object or string.
    public class ComplexDsShape : DsShapeBase,
        IDsContainer
    {
        #region private functions

        private void DsConstantsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            DefaultChangeFactory.Instance.OnCollectionChanged(this, @"DsConstantsCollection",
                DsConstantsCollection, e);
        }

        #endregion

        #region construction and destruction

        public ComplexDsShape() // For XAML serialization
            : this(true, true)
        {
        }

        public ComplexDsShape(bool visualDesignMode, bool loadXamlContent)
            : base(visualDesignMode, loadXamlContent)
        {
            DsShapeDrawingGuid = Guid.NewGuid();
            DsShapeDrawingWidth = double.NaN;
            DsShapeDrawingHeight = double.NaN;
            DsShapeDrawingCenterRelativePosition = new Point(0.5, 0.5);

            if (visualDesignMode)
                DsConstantsCollection.CollectionChanged += DsConstantsCollectionChanged;
        }

        public ComplexDsShape(bool isEmpty)
            : base(isEmpty)
        {
        }

        protected override void Dispose(bool disposing)
        {
            if (Disposed) return;

            if (disposing)
            {
                if (VisualDesignMode)
                    DsConstantsCollection.CollectionChanged -= DsConstantsCollectionChanged;

                foreach (DsShapeBase dsShape in _dsShapes) dsShape.Dispose();
                _dsShapes = new DsShapeBase[0];
            }

            base.Dispose(disposing);
        }

        #endregion

        #region public functions

        public static readonly Guid DsShapeTypeGuid = new(@"0C84224A-3FA4-47FF-B6A2-CCEABBF8B503");

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)] // For XAML serialization        
        public override IDsContainer? Container => this;

        public override Guid GetDsShapeTypeGuid()
        {
            return DsShapeTypeGuid;
        }

        public override string GetDsShapeTypeNameToDisplay()
        {
            if (string.IsNullOrEmpty(DsShapeDrawingName)) return Resources.ComplexDsShape;
            return Resources.ComplexDsShape + @": " + DsShapeDrawingName;
        }

        public override void SerializeOwnedData(SerializationWriter writer, object? context)
        {
            if (ReferenceEquals(context, SerializationContext.FullBytes))
            {
                base.SerializeOwnedData(writer, context);

                writer.Write(DsShapeDrawingName);
                writer.WriteListOfOwnedDataSerializable(DsConstantsCollection.OrderBy(gpi => gpi.Name).ToList(),
                    context);

                writer.WriteDsShapes(DsShapes, context);
                return;
            }

            if (ReferenceEquals(context, SerializationContext.ShortBytes))
            {
                base.SerializeOwnedData(writer, context);

                writer.WriteListOfOwnedDataSerializable(
                    DsConstantsCollection.Where(gpi => gpi.Value != @"").OrderBy(gpi => gpi.Name).ToList(), context);

                writer.WriteDsShapes(DsShapes, context);
                return;
            }

            using (writer.EnterBlock(4))
            {
                base.SerializeOwnedData(writer, context);

                writer.Write(DsShapeDrawingGuid);
                writer.Write(DsShapeDrawingName);
                writer.Write(DsShapeDrawingDesc);
                writer.Write(DsShapeDrawingGroup);
                writer.Write(DsShapeDrawingWidth);
                writer.Write(DsShapeDrawingHeight);
                writer.Write(DsShapeDrawingCenterRelativePosition);
                writer.Write(DsConstantsCollection.OrderBy(gpi => gpi.Name).ToList());
                writer.WriteDsShapes(DsShapes, context);
            }
        }

        public override void DeserializeOwnedDataAsync(SerializationReader reader, object? context)
        {
            if (reader.GetBlockVersionWithoutChangingStreamPosition() <= 2)
                throw new Exception("Unsupported old file format.");

            using (Block block = reader.EnterBlock())
            {
                switch (block.Version)
                {
                    case 4:
                    {
                        base.DeserializeOwnedDataAsync(reader, context);

                        DsShapeDrawingGuid = reader.ReadGuid();
                        DsShapeDrawingName = reader.ReadString();
                        DsShapeDrawingDesc = reader.ReadString();
                        DsShapeDrawingGroup = reader.ReadString();
                        DsShapeDrawingWidth = reader.ReadDouble();
                        DsShapeDrawingHeight = reader.ReadDouble();
                        DsShapeDrawingCenterRelativePosition = reader.ReadPoint();
                        List<DsConstant> dsConstantsCollection = reader.ReadList<DsConstant>();
                        DsConstantsCollection.Clear();
                        foreach (DsConstant dsConstant in dsConstantsCollection) DsConstantsCollection.Add(dsConstant);
                        DsShapes = reader.ReadDsShapes(context, VisualDesignMode, LoadXamlContent);
                    }
                        break;
                    default:
                        throw new BlockUnsupportedVersionException();
                }
            }
        }

        public override void FindConstants(HashSet<string> constants)
        {
            base.FindConstants(constants);

            // _dsShapes and _dsConstantsCollection have attribute [Searchable(false)]

            ConstantsHelper.FindConstants(DsConstantsCollection, constants);

            foreach (DsShapeBase dsShape in _dsShapes)
            {
                var complexDsShape = dsShape as ComplexDsShape;
                if (complexDsShape is not null)
                {
                    ConstantsHelper.FindConstantsInFields(complexDsShape, constants);
                    ConstantsHelper.FindConstants(complexDsShape.DsConstantsCollection, constants);
                    continue;
                }

                dsShape.FindConstants(constants);
            }
        }

        public override void ReplaceConstants(IDsContainer? container)
        {
            base.ReplaceConstants(this);

            var parentContainer = container?.ParentItem?.Find<IDsContainer>();

            if (parentContainer is not null)
                foreach (DsConstant dsConstant in DsConstantsCollection)
                {
                    if (dsConstant.Value != @"")
                        dsConstant.Value = ConstantsHelper.ComputeValue(parentContainer, dsConstant.Value) ?? @"";
                    else
                        dsConstant.Value = ConstantsHelper.GetConstantValue(dsConstant.Name, parentContainer, new IterationInfo());
                }

            foreach (DsShapeBase dsShape in DsShapes)
            {
                dsShape.ReplaceConstants(this);
            }
        }

        public override void RefreshForPropertyGrid(IDsContainer? container)
        {
            base.RefreshForPropertyGrid(this);

            foreach (DsShapeBase dsShape in DsShapes) dsShape.RefreshForPropertyGrid(this);
        }

        public override void GetUsedFileNames(HashSet<string> usedFileNames)
        {
            foreach (DsShapeBase dsShape in _dsShapes) dsShape.GetUsedFileNames(usedFileNames);
        }


        public override void GetDsConstants(
            CaseInsensitiveDictionary<List<ExtendedDsConstant>> dsConstantsDictionary)
        {
            foreach (DsConstant dsConstant in DsConstantsCollection)
            {
                string dsConstantValue;
                List<ExtendedDsConstant>? dsConstantsList;

                if (dsConstant.Value == @"") continue;

                dsConstantValue = ConstantsHelper.ComputeValue(ParentItem.Find<IDsContainer>(), dsConstant.Value)!;

                if (!dsConstantsDictionary.TryGetValue(dsConstantValue, out dsConstantsList))
                {
                    dsConstantsList = new List<ExtendedDsConstant>();
                    dsConstantsDictionary[dsConstantValue] = dsConstantsList;
                }

                dsConstantsList.Add(new ExtendedDsConstant(dsConstant, this));
            }

            foreach (DsShapeBase dsShape in DsShapes)
            {
                dsShape.GetDsConstants(dsConstantsDictionary);
            }
        }

        public override IEnumerable<Guid> GetUsedAddonGuids()
        {
            foreach (var guid in base.GetUsedAddonGuids()) yield return guid;

            foreach (DsShapeBase dsShape in DsShapes)
            foreach (var guid in dsShape.GetUsedAddonGuids())
                yield return guid;
        }

        public void AddDsShapes(bool verifyDsShapeNames, params DsShapeBase[] newDsShapes)
        {
            if (verifyDsShapeNames)
            {
                var dsShapes = new List<DsShapeBase>(_dsShapes);

                foreach (DsShapeBase newDsShape in newDsShapes)
                {
                    if (newDsShape is null) continue;

                    DrawingBase.VerifyDsShapeName(newDsShape, dsShapes);

                    dsShapes.Add(newDsShape);
                }

                _dsShapes = dsShapes.ToArray();
            }
            else
            {
                if (_dsShapes.Length != 0)
                {
                    var oldLen = _dsShapes.Length;
                    Array.Resize(ref _dsShapes, _dsShapes.Length + newDsShapes.Length);
                    Array.Copy(newDsShapes, 0, _dsShapes, oldLen, newDsShapes.Length);
                }
                else
                {
                    _dsShapes = newDsShapes;
                }
            }

            foreach (DsShapeBase dsShape in newDsShapes) dsShape.ParentItem = this;

            this.SetDsShapesIndexes();

            OnPropertyChanged(nameof(DsShapes));
        }

        [DsCategory(ResourceStrings.PrototypeDsShapeDrawingCategory)]
        [DsDisplayName(ResourceStrings.ComplexDsShape_DrawingGuid)]
        //[ReadOnlyInEditor]
        //[PropertyOrder(0)]
        public Guid DsShapeDrawingGuid { get; set; }

        [DsCategory(ResourceStrings.PrototypeDsShapeDrawingCategory)]
        [DsDisplayName(ResourceStrings.ComplexDsShape_DrawingName)]
        //[PropertyOrder(1)]
        public string DsShapeDrawingName
        {
            get => _dsShapeDrawingName;
            set => SetValue(ref _dsShapeDrawingName, value);
        }

        [DsCategory(ResourceStrings.PrototypeDsShapeDrawingCategory)]
        [DsDisplayName(ResourceStrings.ComplexDsShape_DrawingDesc)]
        //[ReadOnlyInEditor]
        //[PropertyOrder(2)]
        [DefaultValue(@"")] // For XAML serialization
        public string DsShapeDrawingDesc
        {
            get => _dsShapeDrawingDesc;
            set => SetValue(ref _dsShapeDrawingDesc, value);
        }

        [DsCategory(ResourceStrings.PrototypeDsShapeDrawingCategory)]
        [DsDisplayName(ResourceStrings.ComplexDsShape_DrawingGroup)]
        //[ReadOnlyInEditor]
        //[PropertyOrder(3)]
        [DefaultValue(@"")] // For XAML serialization
        public string DsShapeDrawingGroup
        {
            get => _dsShapeDrawingGroup;
            set => SetValue(ref _dsShapeDrawingGroup, value);
        }

        [DsCategory(ResourceStrings.PrototypeDsShapeDrawingCategory)]
        [DsDisplayName(ResourceStrings.ComplexDsShape_DrawingWidth)]
        //[ReadOnlyInEditor]
        //[PropertyOrder(4)]
        public double DsShapeDrawingWidth
        {
            get => _dsShapeDrawingWidth;
            set
            {
                value = Math.Round(value, 3);
                SetValue(ref _dsShapeDrawingWidth, value);
            }
        }

        [DsCategory(ResourceStrings.PrototypeDsShapeDrawingCategory)]
        [DsDisplayName(ResourceStrings.ComplexDsShape_DrawingHeight)]
        //[ReadOnlyInEditor]
        //[PropertyOrder(5)]
        public double DsShapeDrawingHeight
        {
            get => _dsShapeDrawingHeight;
            set
            {
                value = Math.Round(value, 3);
                SetValue(ref _dsShapeDrawingHeight, value);
            }
        }

        [DsCategory(ResourceStrings.PrototypeDsShapeDrawingCategory)]
        [DsDisplayName(ResourceStrings.ComplexDsShape_DrawingCenterRelativePosition)]
        //[ReadOnlyInEditor]
        //[PropertyOrder(6)]
        public Point DsShapeDrawingCenterRelativePosition
        {
            get => _dsShapeDrawingCenterRelativePosition;
            set => SetValue(ref _dsShapeDrawingCenterRelativePosition, value);
        }


        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)] // For XAML serialization
        public DsShapeBase[] DsShapes
        {
            get => _dsShapes;
            set
            {
                _dsShapes = new DsShapeBase[0];
                AddDsShapes(false, value);
            }
        }


        [Browsable(false)]
        public object? DsShapesArray
        {
            get => new ArrayList(_dsShapes);
            set
            {
                if (value is null) DsShapes = new DsShapeBase[0];
                else DsShapes = ((ArrayList) value).OfType<DsShapeBase>().ToArray();
            }
        }

        [DsCategory(ResourceStrings.DataCategory)]
        [DsDisplayName(ResourceStrings.ComplexDsShapeDsConstantsCollection)]
        [LocalizedDescription(ResourceStrings.DsConstantsCollectionDescription)]
        //[Editor(typeof(CollectionTypeEditor), typeof(CollectionTypeEditor))]
        //[PropertyOrder(1)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
        [field: Searchable(false)]
        // For XAML serialization of collections
        public ObservableCollection<DsConstant> DsConstantsCollection { get; } = new();


        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)] // For XAML serialization
        public DsConstant[]? HiddenDsConstantsCollection => null;

        [DsCategory(ResourceStrings.DataCategory)]
        [DsDisplayName(ResourceStrings.ChildWindowInfo)]
        [LocalizedDescription(ResourceStrings.ChildWindowInfoDescription)]
        //[ReadOnlyInEditor]
        //[PropertyOrder(2)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)] // For XAML serialization
        public string ChildWindowInfo
        {
            get => _childWindowInfo;
            set
            {
                if (value == _childWindowInfo) return;
                _childWindowInfo = value;
                var parentComplexDsShape = this.GetParentComplexDsShape();
                if (parentComplexDsShape is not null) parentComplexDsShape.ChildWindowInfo = value;
                OnPropertyChangedAuto();
            }
        }

        #endregion

        #region private fields

        private string _dsShapeDrawingName = @"";
        private string _dsShapeDrawingDesc = @"";
        private string _dsShapeDrawingGroup = @"";
        private double _dsShapeDrawingWidth;
        private double _dsShapeDrawingHeight;
        private Point _dsShapeDrawingCenterRelativePosition;

        [Searchable(false)] private DsShapeBase[] _dsShapes = new DsShapeBase[0];

        [Searchable(false)] private string _childWindowInfo = @"";

        #endregion
    }
}