/*************************************************************************************

   Extended WPF Toolkit

   Copyright (C) 2007-2013 Xceed Software Inc.

   This program is provided to you under the terms of the Microsoft Public
   License (Ms-PL) as published at http://wpftoolkit.codeplex.com/license 

   For more features, controls, and fast professional support,
   pick up the Plus Edition at http://xceed.com/wpf_toolkit

   Stay informed: follow @datagrid on Twitter or Like http://facebook.com/datagrids

  ***********************************************************************************/

using System.Reflection;
using System.Security;
using System.Security.Permissions;
using System.Windows.Input;
using Ssz.Xceed.Wpf.Toolkit.Core.Utilities;

namespace Ssz.Xceed.Wpf.Toolkit.Zoombox
{
  public class ZoomboxCursors
  {
    #region Constructors

    static ZoomboxCursors()
    {
      try
      {
                // WARNING
        //new EnvironmentPermission( PermissionState.Unrestricted ).Demand();
        _zoom = new Cursor( ResourceHelper.LoadResourceStream( Assembly.GetExecutingAssembly(), "Zoombox/Resources/Zoom.cur" ) );
        _zoomRelative = new Cursor( ResourceHelper.LoadResourceStream( Assembly.GetExecutingAssembly(), "Zoombox/Resources/ZoomRelative.cur" ) );
      }
      catch( SecurityException )
      {
        // partial trust, so just use default cursors
      }
    }

    #endregion

    #region Zoom Static Property

    public static Cursor Zoom
    {
      get
      {
        return _zoom;
      }
      set { _zoom = value; }
    }

    private static Cursor _zoom = Cursors.Arrow;

    #endregion

    #region ZoomRelative Static Property

    public static Cursor ZoomRelative
    {
      get
      {
        return _zoomRelative;
      }
        set { _zoomRelative = value; }
    }

    private static Cursor _zoomRelative = Cursors.Arrow;

    #endregion
  }
}
