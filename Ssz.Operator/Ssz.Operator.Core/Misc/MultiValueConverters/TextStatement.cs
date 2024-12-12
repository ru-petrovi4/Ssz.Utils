/////////////////////////////////////////////////////////////////////////////
//
//                              COPYRIGHT (c) 2021
//                                    SIMCODE.
//                              ALL RIGHTS RESERVED
//
//  This software is a copyrighted work and/or information protected as a
//  trade secret. Legal rights of Simcode. in this software is distinct
//  from ownership of any medium in which the software is embodied. Copyright
//  or trade secret notices included must be reproduced in any copies
//  authorised by Simcode.
//
/////////////////////////////////////////////////////////////////////////////

using System;
using System.Collections.Generic;
using System.ComponentModel;
using Ssz.Operator.Core.VisualEditors;
using Ssz.Operator.Core.Utils;
using Ssz.Operator.Core.Utils.Serialization;
using Ssz.Utils;

namespace Ssz.Operator.Core.MultiValueConverters
{
    public class TextStatement : OwnedDataSerializableAndCloneable,
        IDsItem, IDisposable
    {
        #region construction and destruction

        public TextStatement()
        {
            Condition = new Expression();
            Value = new Expression();
        }

        public TextStatement(int paramNum, string condition, string value)
        {
            ParamNum = paramNum;
            Condition = new Expression(condition);
            Value = new Expression(value);
        }

        public TextStatement(StatementViewModel value)
        {
            Condition = new Expression(value.Condition);
            if (value.Value is null) throw new InvalidOperationException();
            Value = new Expression(value.Value);
            ParamNum = value.ParamNum.HasValue ? value.ParamNum.Value : 0;
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

                Value.Dispose();
            }
        }

        ~TextStatement()
        {
            Dispose(false);
        }

        #endregion

        #region public functions

        public Expression Condition { get; set; }

        public Expression Value { get; set; }

        public int ParamNum { get; set; }

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
            using (writer.EnterBlock(1))
            {
                writer.Write(Condition, context);
                writer.Write(Value, context);
                writer.Write(ParamNum);
            }
        }

        public override void DeserializeOwnedData(SerializationReader reader, object? context)
        {
            using (Block block = reader.EnterBlock())
            {
                switch (block.Version)
                {
                    case 1:
                        reader.ReadOwnedData(Condition, context);
                        reader.ReadOwnedData(Value, context);
                        ParamNum = reader.ReadInt32();
                        break;
                    default:
                        throw new BlockUnsupportedVersionException();
                }
            }
        }

        public void FindConstants(HashSet<string> constants)
        {
            Condition.FindConstants(constants);
            Value.FindConstants(constants);
        }

        public void ReplaceConstants(IDsContainer? container)
        {
            ItemHelper.ReplaceConstants(Condition, container);
            ItemHelper.ReplaceConstants(Value, container);
        }

        public void RefreshForPropertyGrid(IDsContainer? container)
        {
        }

        #endregion
    }
}