using System;
using System.Collections.Generic;
using System.ComponentModel;

using Ssz.Operator.Core.VisualEditors;
using Ssz.Operator.Core.Utils;
using Ssz.Operator.Core.Utils.Serialization;
using Ssz.Utils;

namespace Ssz.Operator.Core.MultiValueConverters
{
    public class DsBrushStatement : OwnedDataSerializableAndCloneable,
        IDsItem, IDisposable
    {
        #region construction and destruction

        public DsBrushStatement()
        {
            Condition = new Expression();
            ParamNum = null;
            ConstDsBrush = null;
        }

        public DsBrushStatement(StatementViewModel value)
        {
            Condition = new Expression(value.Condition);
            if (value.ConstDsBrush is not null) ConstDsBrush = (DsBrushBase) value.ConstDsBrush.Clone();
            else ConstDsBrush = null;
            ParamNum = value.ParamNum;
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

                ConstDsBrush = null;
            }
        }


        ~DsBrushStatement()
        {
            Dispose(false);
        }

        #endregion

        #region public functions

        public Expression Condition { get; set; }


        [DefaultValue(null)] // For XAML serialization
        public int? ParamNum { get; set; }


        public DsBrushBase? ConstDsBrush { get; set; }

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

        public override void SerializeOwnedData(SerializationWriter writer, object? context)
        {
            using (writer.EnterBlock(2))
            {
                writer.Write(Condition, context);
                writer.WriteObject(ConstDsBrush);
                writer.WriteNullable(ParamNum);
            }
        }

        public override void DeserializeOwnedDataAsync(SerializationReader reader, object? context)
        {
            using (Block block = reader.EnterBlock())
            {
                switch (block.Version)
                {
                    case 1:
                        try
                        {
                            reader.ReadOwnedData(Condition, context);
                            ConstDsBrush = reader.ReadObject() as DsBrushBase;
                        }
                        catch (BlockEndingException)
                        {
                        }

                        break;
                    case 2:
                        reader.ReadOwnedData(Condition, context);
                        ConstDsBrush = reader.ReadObject() as DsBrushBase;
                        ParamNum = reader.ReadNullableInt32();
                        break;
                    default:
                        throw new BlockUnsupportedVersionException();
                }
            }
        }

        public void FindConstants(HashSet<string> constants)
        {
            Condition.FindConstants(constants);
            if (ConstDsBrush is not null) ConstDsBrush.FindConstants(constants);
        }

        public void ReplaceConstants(IDsContainer? container)
        {
            ItemHelper.ReplaceConstants(Condition, container);
            ItemHelper.ReplaceConstants(ConstDsBrush, container);
        }

        public void RefreshForPropertyGrid(IDsContainer? container)
        {
        }

        #endregion
    }
}