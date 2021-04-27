using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media.Imaging;
using System.Reflection;

namespace Microsoft.Research.DynamicDataDisplay
{
	public static class D3IconHelper
	{
		private static BitmapFrame icon = null;
		public static BitmapFrame DynamicDataDisplayIcon
		{
			get
			{
				if (icon == null)
				{
					Assembly currentAssembly = typeof(D3IconHelper).Assembly;
				    icon =
				        BitmapFrame.Create(
				            new Uri(
				                "pack://application:,,,/Microsoft.Research.DynamicDataDisplay;component/Resources/D3-icon.ico",
				                UriKind.RelativeOrAbsolute));
				}
				return icon;
			}
		}

		private static BitmapFrame whiteIcon = null;
		public static BitmapFrame DynamicDataDisplayWhiteIcon
		{
			get
			{
				if (whiteIcon == null)
				{
					Assembly currentAssembly = typeof(D3IconHelper).Assembly;
				    whiteIcon =
				        BitmapFrame.Create(
				            new Uri(
				                "pack://application:,,,/Microsoft.Research.DynamicDataDisplay;component/Resources/D3-icon-white.ico",
				                UriKind.RelativeOrAbsolute));
				}

				return whiteIcon;
			}
		}
	}
}
