using System;
using System.Collections.Generic;
using System.ComponentModel;
using Ssz.Operator.Core.Addons;
using Ssz.Operator.Core.CustomAttributes;
using Ssz.Operator.Core.DsPageTypes;
using Ssz.Operator.Core.Properties;
using Ssz.Operator.Core.Utils;
using Ssz.Operator.Core.Utils.Serialization;
using Ssz.Utils;
//using Ssz.Utils.Wpf;
//using Ssz.Xceed.Wpf.Toolkit.PropertyGrid.Attributes;
//using Ssz.Xceed.Wpf.Toolkit.PropertyGrid.Editors;

namespace Ssz.Operator.Core
{
    public class PlayWindowClassOptions :
        ViewModelBase, IOwnedDataSerializable,
        IDsItem,
        ICloneable, IDisposable
    {
        #region construction and destruction

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (Disposed) return;

            if (disposing) ParentItem = null;

            Disposed = true;
        }

        ~PlayWindowClassOptions()
        {
            Dispose(false);
        }

        #endregion

        #region public functions

        public static PlayWindowClassOptions Default { get; } = new();

        [DsCategory(ResourceStrings.BasicCategory)]
        [DsDisplayName(ResourceStrings.PlayWindowClassOptions_PlayWindowClassInfo)]
        [LocalizedDescription(ResourceStrings.PlayWindowClassOptions_PlayWindowClassInfo_Description)]
        //[ExpandableObject]
        //[Editor(typeof(TextBlockEditor), typeof(TextBlockEditor))]
        //[PropertyOrder(1)]
        public PlayWindowClassInfo PlayWindowClassInfo { get; } = new();

        [DsCategory(ResourceStrings.BehaviourCategory)]
        [DsDisplayName(ResourceStrings.PlayWindowClassOptions_WindowsMaxCount)]
        [LocalizedDescription(ResourceStrings.PlayWindowClassOptions_WindowsMaxCount_Description)]
        //[PropertyOrder(2)]
        public int WindowsMaxCount { get; set; }

        [DsCategory(ResourceStrings.BehaviourCategory)]
        [DsDisplayName(ResourceStrings.PlayWindowClassOptions_CloseWindowsOnParentJump)]
        [LocalizedDescription(ResourceStrings.PlayWindowClassOptions_CloseWindowsOnParentJump_Description)]
        //[PropertyOrder(3)]
        public bool CloseWindowsOnParentJump { get; set; }

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

        public void SerializeOwnedData(SerializationWriter writer, object? context)
        {
            using (writer.EnterBlock(2))
            {
                writer.Write(PlayWindowClassInfo, context);
                writer.Write(WindowsMaxCount);
                writer.Write(CloseWindowsOnParentJump);                
            }
        }

        public void DeserializeOwnedData(SerializationReader reader, object? context)
        {
            using (Block block = reader.EnterBlock())
            {
                switch (block.Version)
                {                    
                    case 2:
                        try
                        {
                            reader.ReadOwnedData(PlayWindowClassInfo, context);
                            WindowsMaxCount = reader.ReadInt32();
                            CloseWindowsOnParentJump = reader.ReadBoolean();
                        }
                        catch (BlockEndingException)
                        {
                        }
                        break;
                    default:
                        throw new BlockUnsupportedVersionException();
                }
            }
        }

        public void FindConstants(HashSet<string> constants)
        {
        }

        public void ReplaceConstants(IDsContainer? container)
        {
        }

        public void RefreshForPropertyGrid(IDsContainer? container)
        {
        }

        public object Clone()
        {
            return this.CloneUsingSerialization();
        }

        public override bool Equals(object? obj)
        {
            var other = obj as PlayWindowClassOptions;
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Equals(other);
        }

        public override int GetHashCode()
        {
            return 0;
        }

        public override string ToString()
        {
            var list = new List<string>();
            if (!String.IsNullOrEmpty(PlayWindowClassInfo.WindowCategory))
                list.Add("Category: " + PlayWindowClassInfo.WindowCategory);
            if (PlayWindowClassInfo.WindowDsPageTypeGuid != Guid.Empty)
                list.Add("DsPage Type: " + AddonsHelper.GetDsPageTypeName(PlayWindowClassInfo.WindowDsPageTypeGuid));
#if NET5_0_OR_GREATER
            return String.Join(';', list);
#else
            return String.Join(";", list);
#endif            
        }

        #endregion

        #region protected functions

        protected bool Disposed { get; private set; }

        protected bool Equals(PlayWindowClassOptions other)
        {
            return Equals(PlayWindowClassInfo, other.PlayWindowClassInfo) &&
                   WindowsMaxCount == other.WindowsMaxCount &&
                   CloseWindowsOnParentJump == other.CloseWindowsOnParentJump;
        }

        #endregion
    }
}