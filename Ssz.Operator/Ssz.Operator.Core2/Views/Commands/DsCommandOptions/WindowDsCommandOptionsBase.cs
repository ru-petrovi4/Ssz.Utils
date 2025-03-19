using System.Collections.Generic;
using System.ComponentModel;
using Ssz.Operator.Core.Constants;
using Ssz.Operator.Core.CustomAttributes;
using Ssz.Operator.Core.Properties;
using Ssz.Operator.Core.Utils;
using Ssz.Operator.Core.Utils.Serialization;
using Ssz.Utils;
//using Ssz.Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

namespace Ssz.Operator.Core.Commands.DsCommandOptions
{
    public abstract class WindowDsCommandOptionsBase : OwnedDataSerializableAndCloneable, IDsItem
    {
        #region public functions

        [DsDisplayName(ResourceStrings.WindowDsCommandOptionsBase_TargetWindow)]
        [LocalizedDescription(ResourceStrings.WindowDsCommandOptionsBase_TargetWindow_Description)]
        //[PropertyOrder(0)]
        public virtual TargetWindow TargetWindow { get; set; }

        [DsDisplayName(ResourceStrings.WindowDsCommandOptionsBase_RootWindowNum)]
        [LocalizedDescription(ResourceStrings.WindowDsCommandOptionsBase_RootWindowNum_Description)]
        //[PropertyOrder(1)]
        public virtual string RootWindowNum { get; set; } = @"";

        [DsDisplayName(ResourceStrings.WindowDsCommandOptionsBase_CurrentFrame)]
        [LocalizedDescription(ResourceStrings.WindowDsCommandOptionsBase_CurrentFrame_Description)]
        //[PropertyOrder(2)]
        public virtual bool CurrentFrame { get; set; }

        [DsDisplayName(ResourceStrings.WindowDsCommandOptionsBase_FrameName)]
        [LocalizedDescription(ResourceStrings.WindowDsCommandOptionsBase_FrameName_Description)]
        //[PropertyOrder(3)]
        public virtual string FrameName { get; set; } = "";

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
                var targetWindow = TargetWindow;
                string rootWindowNum = RootWindowNum;
                string frameName = FrameName;
                if (CurrentFrame)
                {
                    targetWindow = TargetWindow.CurrentWindow;
                    rootWindowNum = "";
                    frameName = "";
                }
                else if (!string.IsNullOrWhiteSpace(RootWindowNum))
                {
                    var n = ObsoleteAnyHelper.ConvertTo<int>(RootWindowNum, false);
                    if (n > 0) targetWindow = TargetWindow.RootWindow;
                }

                writer.Write((int) targetWindow);
                writer.Write(rootWindowNum);
                writer.Write(frameName);
                writer.Write(CurrentFrame);
            }
        }

        public override void DeserializeOwnedData(SerializationReader reader, object? context)
        {
            using (Block block = reader.EnterBlock())
            {
                switch (block.Version)
                {
                    case 1:
                        try
                        {
                            TargetWindow = (TargetWindow) reader.ReadInt32();
                            RootWindowNum = reader.ReadString();
                            FrameName = reader.ReadString();
                            CurrentFrame = reader.ReadBoolean();
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

        public virtual void FindConstants(HashSet<string> constants)
        {
            ConstantsHelper.FindConstants(RootWindowNum,
                constants);
            ConstantsHelper.FindConstants(FrameName,
                constants);
        }

        public virtual void ReplaceConstants(IDsContainer? container)
        {
            RootWindowNum = ConstantsHelper.ComputeValue(container,
                RootWindowNum)!;
            FrameName = ConstantsHelper.ComputeValue(container,
                FrameName)!;
        }

        public virtual void RefreshForPropertyGrid(IDsContainer? container)
        {
        }

        public override string ToString()
        {
            if (CurrentFrame) return "Window: " + TargetWindow + "; Frame: <Current>";

            if (TargetWindow == TargetWindow.RootWindow)
            {
                string rootWindowNum = "<Current>";
                if (!string.IsNullOrWhiteSpace(RootWindowNum))
                {
                    var n = ObsoleteAnyHelper.ConvertTo<int>(RootWindowNum, false);
                    if (n > 0) rootWindowNum = RootWindowNum;
                }

                return "Window: " + TargetWindow + "; Root Window Num: " + rootWindowNum + "; Frame: " +
                       (!string.IsNullOrEmpty(FrameName) ? FrameName : @"<Main>");
            }

            return "Window: " + TargetWindow + "; Frame: " + (!string.IsNullOrEmpty(FrameName) ? FrameName : @"<Main>");
        }

        #endregion
    }

    public enum TargetWindow
    {
        CurrentWindow = 0,
        ParentWindow = 1,
        RootWindow = 2
    }
}