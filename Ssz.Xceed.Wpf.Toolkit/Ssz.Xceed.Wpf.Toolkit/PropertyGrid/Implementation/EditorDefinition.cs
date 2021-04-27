/*************************************************************************************

   Extended WPF Toolkit

   Copyright (C) 2007-2013 Xceed Software Inc.

   This program is provided to you under the terms of the Microsoft Public
   License (Ms-PL) as published at http://wpftoolkit.codeplex.com/license 

   For more features, controls, and fast professional support,
   pick up the Plus Edition at http://xceed.com/wpf_toolkit

   Stay informed: follow @datagrid on Twitter or Like http://facebook.com/datagrids

  ***********************************************************************************/

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows;

namespace Ssz.Xceed.Wpf.Toolkit.PropertyGrid
{
    [Obsolete(@"Use EditorTemplateDefinition instead of EditorDefinition. " + UsageEx)]
    public class EditorDefinition : EditorTemplateDefinition
    {
        private const string UsageEx =
            " (XAML Ex: <t:EditorTemplateDefinition TargetProperties=\"FirstName,LastName\" .../> OR <t:EditorTemplateDefinition TargetProperties=\"{x:Type l:MyType}\" .../> )";


        public EditorDefinition()
        {
            const string usageErr = "{0} is obsolete. Instead use {1}.";
            Trace.TraceWarning(string.Format(usageErr, typeof(EditorDefinition), typeof(EditorTemplateDefinition)) +
                               UsageEx);
        }

        /// <summary>
        ///     Gets or sets the template of the editor.
        ///     This Property is part of the obsolete EditorDefinition class.
        ///     Use EditorTemplateDefinition class and the Edit<b>ing</b>Template property.
        /// </summary>
        public DataTemplate EditorTemplate { get; set; }

        /// <summary>
        ///     List the PropertyDefinitions that identify the properties targeted by the EditorTemplate.
        ///     This Property is part of the obsolete EditorDefinition class.
        ///     Use "EditorTemplateDefinition" class and the "TargetProperties" property<br />
        ///     XAML Ex.: &lt;t:EditorTemplateDefinition TargetProperties="FirstName,LastName" .../&gt;
        /// </summary>
        public PropertyDefinitionCollection PropertiesDefinitions { get; set; } = new();

        public Type TargetType { get; set; }

        internal override void Lock()
        {
            const string usageError =
                @"Use a EditorTemplateDefinition instead of EditorDefinition in order to use the '{0}' property.";
            if (EditingTemplate != null)
                throw new InvalidOperationException(string.Format(usageError, "EditingTemplate"));

            if (TargetProperties != null && TargetProperties.Count > 0)
                throw new InvalidOperationException(string.Format(usageError, "TargetProperties"));

            var properties = new List<object>();
            if (PropertiesDefinitions != null)
                foreach (var def in PropertiesDefinitions)
                    if (def.TargetProperties != null)
                        properties.AddRange(def.TargetProperties.Cast<object>());

            TargetProperties = properties;
            EditingTemplate = EditorTemplate;

            base.Lock();
        }
    }
}