using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using Avalonia.Markup;
using Ssz.Operator.Core.Drawings;
using Ssz.Operator.Core.Utils.Serialization;
using Ssz.Utils;
using OwnedDataSerializableAndCloneable = Ssz.Operator.Core.Utils.OwnedDataSerializableAndCloneable;

namespace Ssz.Operator.Core.MultiValueConverters
{
    //[ContentProperty(@"ConstXaml")] // For XAML serialization. Content property must be of type object or string.
    public class XamlStatement : OwnedDataSerializableAndCloneable,
        IDsItem, IDisposable
    {
        #region construction and destruction

        public XamlStatement() :
            this(true)
        {
        }

        public XamlStatement(bool loadXamlContent)
        {
            LoadXamlContent = loadXamlContent;
            Condition = new Expression();
            _constDsXaml = new DsXaml();
            _constDsXaml.ParentItem = this;
        }


        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }


        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                ParentItem = null;

                Condition.Dispose();

                _constObject = null;
            }
        }


        ~XamlStatement()
        {
            Dispose(false);
        }

        #endregion

        #region public functions

        [Searchable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool LoadXamlContent { get; }

        public Expression Condition { get; set; }

        public object ConstXaml
        {
            get => _constDsXaml;
            set
            {
                if (LoadXamlContent)
                {
                    var dsXaml = value as DsXaml;
                    if (dsXaml is null)
                    {
                        _constDsXaml.Xaml = @"";
                    }
                    else
                    {
                        _constDsXaml = dsXaml;
                        _constDsXaml.ParentItem = this;
                    }
                }
            }
        }

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)] // For XAML serialization
        [field: Searchable(false)]
        public IDsItem? ParentItem { get; set; }

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)] // For XAML serialization        
        public bool RefreshForPropertyGridIsDisabled { get; set; }

        public void RefreshForPropertyGrid()
        {
            RefreshForPropertyGrid(ParentItem.Find<IDsContainer>());
        }

        public void EndEditInPropertyGrid()
        {
        }

        public async Task CreateConstObjectAsync()
        {
            try
            {
                _constObject = await XamlHelper.LoadFromXamlWithDescAsync(_constDsXaml.Xaml, ParentItem.Find<DrawingBase>()?.DrawingFilesDirectoryFullName);
            }
            catch
            {
                _constObject = null;
            }
        }

        public object? GetConstObject()
        {
            return _constObject;
        }

        public override void SerializeOwnedData(SerializationWriter writer, object? context)
        {
            if (ReferenceEquals(context, SerializationContext.FullBytes))
            {
                writer.Write(Condition, context);
                writer.Write(_constDsXaml.XamlWithRelativePaths);
                return;
            }

            if (ReferenceEquals(context, SerializationContext.ShortBytes))
            {
                writer.Write(Condition, context);

                string? drawingFilesDirectoryFullName = null;
                var drawing = ParentItem.Find<DrawingBase>();
                if (drawing is not null) drawingFilesDirectoryFullName = drawing.DrawingFilesDirectoryFullName;
                writer.WriteHashOfXaml(_constDsXaml.XamlWithRelativePaths, drawingFilesDirectoryFullName);

                return;
            }

            using (writer.EnterBlock(3))
            {
                writer.Write(Condition, context);
                writer.Write(_constDsXaml.XamlWithRelativePaths);
            }
        }

        public override void DeserializeOwnedData(SerializationReader reader, object? context)
        {
            using (Block block = reader.EnterBlock())
            {
                switch (block.Version)
                {
                    case 3:
                        reader.ReadOwnedData(Condition, context);
                        if (LoadXamlContent)
                            _constDsXaml.XamlWithRelativePaths = reader.ReadString();
                        else
                            reader.SkipString();
                        break;
                    default:
                        throw new BlockUnsupportedVersionException();
                }
            }
        }

        public void FindConstants(HashSet<string> constants)
        {
            Condition.FindConstants(constants);
        }

        public void ReplaceConstants(IDsContainer? container)
        {
            ItemHelper.ReplaceConstants(Condition, container);
        }

        public void RefreshForPropertyGrid(IDsContainer? container)
        {
        }

        public void GetUsedFileNames(HashSet<string> usedFileNames)
        {
            XamlHelper.GetUsedFileNames(_constDsXaml.XamlWithRelativePaths, usedFileNames);
        }

        #endregion

        #region private fields

        private DsXaml _constDsXaml;        
        private object? _constObject;

        #endregion
    }
}