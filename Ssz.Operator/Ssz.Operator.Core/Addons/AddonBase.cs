/////////////////////////////////////////////////////////////////////////////
//
//                              COPYRIGHT (c) 2021
//                                  SIMCODE
//                              ALL RIGHTS RESERVED
//
//	Legal rights of Simcode in this software is distinct from
//  ownership of any medium in which the software is embodied. Copyright notices 
//  must be reproduced in any copies authorized by Simcode
//
///////////////////////////////////////////////////////////////////////////

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using Ssz.Operator.Core.ControlsPlay;
using Ssz.Operator.Core.ControlsPlay.VirtualKeyboards;
using Ssz.Operator.Core.CustomAttributes;
using Ssz.Operator.Core.DsPageTypes;
using Ssz.Operator.Core.Properties;
using Ssz.Operator.Core.Utils;
using Ssz.Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

namespace Ssz.Operator.Core.Addons
{
    [Serializable]
    public abstract class AddonBase : OwnedDataSerializable
    {
        #region public functions

        public const string SszOperatorVersionConst = @"1";

        [Browsable(false)] public abstract Guid Guid { get; }

        /// <summary>
        ///     Must be correct file name.
        /// </summary>
        [Browsable(false)] public abstract string Name { get; }

        [DsCategory(ResourceStrings.SystemCategory)]
        [DsDisplayName(ResourceStrings.AddonBaseDescription)]
        [PropertyOrder(1)]
        public abstract string Desc { get; }

        [DsCategory(ResourceStrings.SystemCategory)]
        [DsDisplayName(ResourceStrings.AddonBaseVersion)]
        [PropertyOrder(2)]
        public abstract string Version { get; }

        [DsCategory(ResourceStrings.SystemCategory)]
        [DsDisplayName(ResourceStrings.AddonBaseSszOperatorVersion)]
        [PropertyOrder(3)]
        public abstract string SszOperatorVersion { get; }

        [DsCategory(ResourceStrings.SystemCategory)]
        [DsDisplayName(ResourceStrings.AddonBaseDllFileFullName)]
        [ReadOnlyInEditor]
        [PropertyOrder(4)]
        public string DllFileFullName { get; set; } = @"";

        [Browsable(false)] public virtual bool Is64BitProcessSupported => true;

        [Browsable(false)] public virtual bool IsAutoSwitchOnForNewDsProjects => false;

        public virtual void InitializeInPlayMode()
        {
        }

        public virtual void CloseInPlayMode()
        {
        }

        public virtual IEnumerable<DsPageTypeBase>? GetDsPageTypes()
        {
            return null;
        }

        public virtual PlayControlBase? NewPlayControl(Guid typeGuid, IPlayWindow window)
        {
            return null;
        }

        public virtual IEnumerable<VirtualKeyboardInfo>? GetVirtualKeyboardsInfo()
        {
            return null;
        }

        public virtual UIElement? NewVirtualKeyboardControl(string virtualKeyboardType)
        {
            return null;
        }        

        #endregion
    }
}