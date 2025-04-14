using System;
using System.Collections;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using Avalonia;
using Ssz.Operator.Core.Constants;
using Ssz.Operator.Core.CustomAttributes;
using Ssz.Operator.Core.DsShapes;
using Ssz.Operator.Core.Properties;
using Ssz.Operator.Core.Utils;
using Ssz.Operator.Core.Utils.Serialization;
using Ssz.Utils;
//using Ssz.Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

namespace Ssz.Operator.Core.Drawings
{
    public class DsShapeDrawing : DrawingBase
    {
        #region private fields

        private readonly ComplexDsShapeCenterPointDsShape _complexDsShapeCenterPointDsShape;

        #endregion

        #region construction and destruction

        public DsShapeDrawing()
            : this(true, true)
        {
        }


        public DsShapeDrawing(bool visualDesignMode, bool loadXamlContent)
            : base(visualDesignMode, loadXamlContent)
        {
            _complexDsShapeCenterPointDsShape =
                new ComplexDsShapeCenterPointDsShape(visualDesignMode, loadXamlContent);
            SystemDsShapes.Add(_complexDsShapeCenterPointDsShape);
            _complexDsShapeCenterPointDsShape.Index = -1;

            if (visualDesignMode)
            {
                _complexDsShapeCenterPointDsShape.PropertyChanged +=
                    ComplexDsShapeCenterPointDsShapeOnPropertyChanged;
                PropertyChanged += OnPropertyChanged;
            }

            CenterRelativePosition = new Point(0.5, 0.5);
        }

        #endregion

        #region public functions

        public override bool IsFaceplate => false;

        [DsCategory(ResourceStrings.DrawingCategory)]
        [DsDisplayName(ResourceStrings.ComplexDsShapeName)]
        //[ReadOnlyInEditor]
        //[PropertyOrder(1)]
        public override string Name
        {
            get => base.Name;
            set => base.Name = value;
        }

        [DsCategory(ResourceStrings.DrawingCategory)]
        [DsDisplayName(ResourceStrings.ComplexDsShapeDesc)]
        //[PropertyOrder(2)]
        public override string Desc
        {
            get => base.Desc;
            set => base.Desc = value;
        }

        [DsCategory(ResourceStrings.DrawingCategory)]
        [DsDisplayName(ResourceStrings.ComplexDsShapeGroup)]
        //[PropertyOrder(3)]
        public override string Group
        {
            get => base.Group;
            set => base.Group = value;
        }

        [DsCategory(ResourceStrings.DrawingCategory)]
        [DsDisplayName(ResourceStrings.DrawingWidth)]
        //[PropertyOrder(4)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)] // For XAML serialization
        public double WidthForUser
        {
            get => base.Width;
            set
            {
                base.Width = value;
                VerifyCenterPointPosition();
            }
        }

        [DsCategory(ResourceStrings.DrawingCategory)]
        [DsDisplayName(ResourceStrings.DrawingHeight)]
        //[PropertyOrder(5)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)] // For XAML serialization
        public double HeightForUser
        {
            get => base.Height;
            set
            {
                base.Height = value;
                VerifyCenterPointPosition();
            }
        }

        [Browsable(false)]
        public override double Width
        {
            get => base.Width;
            set => base.Width = value;
        }

        [Browsable(false)]
        public override double Height
        {
            get => base.Height;
            set => base.Height = value;
        }

        [DsCategory(ResourceStrings.GeometryCategory)]
        [DsDisplayName(ResourceStrings.DsShapeCenterRelativePosition)]
        //[PropertyOrder(1)]
        public Point CenterRelativePosition
        {
            get
            {
                var p = _complexDsShapeCenterPointDsShape.CenterInitialPositionNotRounded;
                return new Point(p.X / Width, p.Y / Height);
            }
            set => _complexDsShapeCenterPointDsShape.CenterInitialPosition =
                new Point(value.X * Width, value.Y * Height);
        }

        public override string ToString()
        {
            return Resources.DsShapeDrawing;
        }

        public override byte[] GetBytes(bool full)
        {
            using (var memoryStream = new MemoryStream(1024 * 1024))
            {
                using (var writer = new SerializationWriter(memoryStream))
                {
                    if (full)
                    {
                        writer.Write(Desc);
                        writer.Write(Group);
                        writer.WriteListOfOwnedDataSerializable(DsConstantsCollection.OrderBy(gpi => gpi.Name).ToList(),
                            SerializationContext.FullBytes);
                        writer.Write(CenterRelativePosition);

                        writer.Write(Mark);

                        writer.Write(Math.Round(Width, 1));
                        writer.Write(Math.Round(Height, 1));
                        writer.WriteDsShapes(DsShapes, SerializationContext.FullBytes);
                    }
                    else
                    {
                        writer.WriteListOfOwnedDataSerializable(
                            DsConstantsCollection.Where(gpi => gpi.Value != @"").OrderBy(gpi => gpi.Name).ToList(),
                            SerializationContext.ShortBytes);

                        writer.WriteDsShapes(DsShapes, SerializationContext.ShortBytes);
                    }
                }

                return memoryStream.ToArray();
            }
        }

        public override void SerializeOwnedData(SerializationWriter writer, object? context)
        {
            using (writer.EnterBlock(4))
            {
                base.SerializeOwnedData(writer, context);

                writer.Write(CenterRelativePosition);
            }
        }

        public override void DeserializeOwnedData(SerializationReader reader, object? context)
        {
            using (Block block = reader.EnterBlock())
            {
                switch (block.Version)
                {                    
                    case 4:
                        base.DeserializeOwnedData(reader, context);

                        CenterRelativePosition = reader.ReadPoint();
                        break;
                    default:
                        throw new BlockUnsupportedVersionException();
                }
            }
        }

        //public void CreatePreviewImage(Control frameworkElement)
        //{
        //    PreviewImageBytes = XamlHelper.CreatePreviewImageBytes(frameworkElement, 64, 64);
        //}

        public override void ResizeHorizontalFromLeft(double horizontalChange)
        {
            base.ResizeHorizontalFromLeft(horizontalChange);
            VerifyCenterPointPosition();
        }

        public override void ResizeVerticalFromTop(double verticalChange)
        {
            base.ResizeVerticalFromTop(verticalChange);
            VerifyCenterPointPosition();
        }

        public override void ResizeHorizontalFromRight(double horizontalChange)
        {
            base.ResizeHorizontalFromRight(horizontalChange);
            VerifyCenterPointPosition();
        }

        public override void ResizeVerticalFromBottom(double verticalChange)
        {
            base.ResizeVerticalFromBottom(verticalChange);
            VerifyCenterPointPosition();
        }

        public ComplexDsShape GetComplexDsShape(bool visualDesignMode, double? widthInitial = null,
            double? heightInitial = null)
        {
            var complexDsShape = new ComplexDsShape(visualDesignMode, true);
            complexDsShape.Name = Name;

            this.CropUnusedSpace();

            if (widthInitial.HasValue) complexDsShape.WidthInitial = widthInitial.Value;
            else complexDsShape.WidthInitial = Width;
            if (heightInitial.HasValue) complexDsShape.HeightInitial = heightInitial.Value;
            else complexDsShape.HeightInitial = Height;
            complexDsShape.CenterRelativePosition = CenterRelativePosition;

            FillInComplexDsShape(complexDsShape);

            return complexDsShape;
        }

        public void FillInComplexDsShape(ComplexDsShape complexDsShape)
        {
            if (complexDsShape is null) return;

            complexDsShape.DsShapeDrawingGuid = Guid;
            complexDsShape.DsShapeDrawingName = Name;
            complexDsShape.DsShapeDrawingDesc = Desc;
            complexDsShape.DsShapeDrawingGroup = Group;
            complexDsShape.DsShapeDrawingWidth = Width;
            complexDsShape.DsShapeDrawingHeight = Height;
            complexDsShape.DsShapeDrawingCenterRelativePosition = CenterRelativePosition;

            string xaml = XamlHelper.Save(DsShapesArray ?? new ArrayList());
            complexDsShape.DsShapesArray = XamlHelper.Load(xaml);

            complexDsShape.TransformDsShapes(complexDsShape.WidthInitialNotRounded / Width,
                complexDsShape.HeightInitialNotRounded / Height);

            DsConstant[] oldDsConstantsCollection =
                complexDsShape.DsConstantsCollection.ToArray();

            complexDsShape.DsConstantsCollection.Clear();
            foreach (
                DsConstant dsConstant in
                DsConstantsCollection)
            {
                var dsConstantCopy = new DsConstant(dsConstant);
                dsConstantCopy.DefaultValue = dsConstant.Value;
                complexDsShape.DsConstantsCollection.Add(dsConstantCopy);
            }

            if (oldDsConstantsCollection.Length > 0)
                foreach (
                    DsConstant oldDsConstant in
                    oldDsConstantsCollection)
                {
                    if (oldDsConstant.Value == oldDsConstant.DefaultValue) continue;
                    var gpi =
                        complexDsShape.DsConstantsCollection.FirstOrDefault(
                            g =>
                                StringHelper.CompareIgnoreCase(g.Name, oldDsConstant.Name));
                    if (gpi is not null) gpi.Value = oldDsConstant.Value;
                }

            complexDsShape.RefreshForPropertyGrid(complexDsShape);
        }

        public override void RefreshForPropertyGrid(IDsContainer? container)
        {
            base.RefreshForPropertyGrid(this);

            foreach (DsShapeBase dsShape in DsShapes) dsShape.RefreshForPropertyGrid();
        }

        public override DrawingInfo GetDrawingInfo()
        {
            using (HashAlgorithm hashAlgorithm = SHA512.Create())
            {
                byte[] shortBytes = GetBytes(false);
                return new DsShapeDrawingInfo(
                    FileFullName,
                    Guid,
                    Desc, Group, PreviewImageBytes,
                    SerializationVersionDateTime,
                    DsConstantsCollection.OrderBy(gpi => gpi.Name).ToArray(), Mark, ActuallyUsedAddonsInfo,
                    hashAlgorithm.ComputeHash(shortBytes), shortBytes.Length);
            }
        }

        public override object Clone()
        {
            var clone = this.CloneUsingSerialization(() =>
                new DsShapeDrawing(VisualDesignMode, LoadXamlContent));
            clone.SetDrawingInfo(GetDrawingInfo());
            return clone;
        }

        #endregion

        #region private functions

        private void VerifyCenterPointPosition()
        {
            var c = _complexDsShapeCenterPointDsShape.CenterInitialPosition;
            if (c.X < 0 || c.X > Width ||
                c.Y < 0 || c.Y > Height)
            {
                var rect = GetBoundingRectOfAllDsShapes();
                _complexDsShapeCenterPointDsShape.CenterInitialPosition =
                    new Point(rect.X + rect.Width / 2, rect.Y + rect.Height / 2);
            }
        }

        private void ComplexDsShapeCenterPointDsShapeOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e is not null && e.PropertyName == @"CenterInitialPosition") OnPropertyChanged(@"CenterRelativePosition");
        }

        private void OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e is not null &&
                (e.PropertyName == @"Width" ||
                 e.PropertyName == @"Height"))
                OnPropertyChanged(@"CenterRelativePosition");
        }

        #endregion
    }
}