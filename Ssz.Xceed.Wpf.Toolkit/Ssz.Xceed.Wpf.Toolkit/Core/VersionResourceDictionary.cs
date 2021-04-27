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
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;

namespace Ssz.Xceed.Wpf.Toolkit.Core
{
    public class VersionResourceDictionary : ResourceDictionary, ISupportInitialize
    {
        private string _assemblyName;
        private int _initializingCount;
        private string _sourcePath;


        public VersionResourceDictionary()
        {
        }

        public VersionResourceDictionary(string assemblyName, string sourcePath)
        {
            ((ISupportInitialize) this).BeginInit();
            AssemblyName = assemblyName;
            SourcePath = sourcePath;
            ((ISupportInitialize) this).EndInit();
        }

        public string AssemblyName
        {
            get => _assemblyName;
            set
            {
                EnsureInitialization();
                _assemblyName = value;
            }
        }

        public string SourcePath
        {
            get => _sourcePath;
            set
            {
                EnsureInitialization();
                _sourcePath = value;
            }
        }

        void ISupportInitialize.BeginInit()
        {
            base.BeginInit();
            _initializingCount++;
        }

        void ISupportInitialize.EndInit()
        {
            _initializingCount--;
            Debug.Assert(_initializingCount >= 0);

            if (_initializingCount <= 0)
            {
                if (Source != null)
                    throw new InvalidOperationException(
                        "Source property cannot be initialized on the VersionResourceDictionary");

                if (string.IsNullOrEmpty(AssemblyName) || string.IsNullOrEmpty(SourcePath))
                    throw new InvalidOperationException(
                        "AssemblyName and SourcePath must be set during initialization");

                //Using an absolute path is necessary in VS2015 for themes different than Windows 8.
                //string uriStr = string.Format( @"pack://application:,,,/{0};v{1};component/{2}", this.AssemblyName, "2.1.0.0", this.SourcePath );
                var uriStr = string.Format(@"pack://application:,,,/{0};component/{1}", AssemblyName, SourcePath);
                Source = new Uri(uriStr, UriKind.Absolute);
            }

            base.EndInit();
        }

        private void EnsureInitialization()
        {
            if (_initializingCount <= 0)
                throw new InvalidOperationException(
                    "VersionResourceDictionary properties can only be set while initializing");
        }


        private enum InitState
        {
            NotInitialized,
            Initializing,
            Initialized
        }
    }
}