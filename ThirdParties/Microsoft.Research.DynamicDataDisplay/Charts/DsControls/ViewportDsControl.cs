using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.DsControls;
using System.Windows.Media;

namespace Microsoft.Research.DynamicDataDisplay.Charts
{
	/// <summary>
	/// ViewportDsControl is a base class for simple dsControls with viewport-bound coordinates.
	/// </summary>
	public abstract class ViewportDsControl : DsControl, IPlotterElement
	{
		static ViewportDsControl()
		{
			Type type = typeof(ViewportDsControl);
			DsControl.StrokeProperty.AddOwner(type, new FrameworkPropertyMetadata(Brushes.Blue));
			DsControl.StrokeThicknessProperty.AddOwner(type, new FrameworkPropertyMetadata(2.0));
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="ViewportDsControl"/> class.
		/// </summary>
		protected ViewportDsControl()
		{
		}

		protected void UpdateUIRepresentation()
		{
			if (Plotter == null)
				return;

			UpdateUIRepresentationCore();
		}
		protected virtual void UpdateUIRepresentationCore() { }

		#region IPlotterElement Members

		private Plotter2D plotter;
		void IPlotterElement.OnPlotterAttached(Plotter plotter)
		{
			plotter.CentralGrid.Children.Add(this);

			Plotter2D plotter2d = (Plotter2D)plotter;
			this.plotter = plotter2d;
			plotter2d.Viewport.PropertyChanged += Viewport_PropertyChanged;

			UpdateUIRepresentation();
		}

		private void Viewport_PropertyChanged(object sender, ExtendedPropertyChangedEventArgs e)
		{
			UpdateUIRepresentation();
		}

		void IPlotterElement.OnPlotterDetaching(Plotter plotter)
		{
			Plotter2D plotter2d = (Plotter2D)plotter;
			plotter2d.Viewport.PropertyChanged -= Viewport_PropertyChanged;
			plotter.CentralGrid.Children.Remove(this);

			this.plotter = null;
		}

		public Plotter2D Plotter
		{
			get { return plotter; }
		}

		Plotter IPlotterElement.Plotter
		{
			get { return plotter; }
		}

		#endregion
	}
}
