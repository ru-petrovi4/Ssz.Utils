using Ssz.Operator.Core.Addons;
using Ssz.Operator.Core.CustomAttributes;
using Ssz.Operator.Core.DsPageTypes;
using Ssz.Operator.Core.Properties;
using Ssz.Operator.Core.Utils;
using Ssz.Operator.Core.Utils.Serialization;
//using Ssz.Xceed.Wpf.Toolkit.PropertyGrid.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ssz.Operator.Core
{
    public class PlayWindowClassInfo: OwnedDataSerializableAndCloneable
    {
        #region public functions

        [DsDisplayName(ResourceStrings.PlayWindowClassInfo_WindowCategory)]
        [LocalizedDescription(ResourceStrings.PlayWindowClassInfo_WindowCategory_Description)]
        //[PropertyOrder(1)]
        public string WindowCategory { get; set; } = @"";

        [DsDisplayName(ResourceStrings.DsPageDrawing_DsPageTypeGuid)]
        //[ItemsSource(typeof(WindowDsPageTypeGuid_ItemsSource))]
        //[PropertyOrder(2)]
        public Guid WindowDsPageTypeGuid { get; set; }

        public override void SerializeOwnedData(SerializationWriter writer, object context)
        {
            using (writer.EnterBlock(1))
            {
                writer.Write(WindowCategory);
                writer.Write(WindowDsPageTypeGuid);                
            }
        }

        public override void DeserializeOwnedData(SerializationReader reader, object context)
        {
            using (Block block = reader.EnterBlock())
            {
                switch (block.Version)
                {                    
                    case 1:
                        WindowCategory = reader.ReadString();
                        WindowDsPageTypeGuid = reader.ReadGuid();                        
                        break;
                    default:
                        throw new BlockUnsupportedVersionException();
                }
            }
        }

        public override string ToString()
        {
            return @"";
        }        

        public override bool Equals(object? obj)
        {
            var other = obj as PlayWindowClassInfo;
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return other.WindowCategory == WindowCategory && other.WindowDsPageTypeGuid == WindowDsPageTypeGuid;
        }

        public override int GetHashCode()
        {
            return 0;
        }

        public bool IsForPlayWindow(IPlayWindow playWindow)
        {
            if (!String.IsNullOrEmpty(WindowCategory))
            {
                if (!String.Equals(WindowCategory, playWindow.WindowCategory))
                    return false;
            }

            if (WindowDsPageTypeGuid != Guid.Empty)
            {
                if (WindowDsPageTypeGuid != playWindow.PlayControlWrapper.CurrentDsPageTypeGuid)
                    return false;
            }

            return true;
        }

        #endregion

        //public class WindowDsPageTypeGuid_ItemsSource : IItemsSource
        //{
        //    #region public functions

        //    public ItemCollection GetValues()
        //    {
        //        var itemCollection = new ItemCollection();
        //        itemCollection.Add(Guid.Empty, @"Any");
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
}
