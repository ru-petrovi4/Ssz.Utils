﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Plot.cs" company="OxyPlot">
//   Copyright (c) 2014 OxyPlot contributors
// </copyright>
// <summary>
//   Represents a control that displays a <see cref="PlotModel" />.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

using Avalonia;

namespace OxyPlot.Avalonia
{
    using global::Avalonia.Controls;
    using global::Avalonia.LogicalTree;
    using global::Avalonia.VisualTree;
    using System.Collections.ObjectModel;
    using System.Collections.Specialized;
    using System.Linq;

    /// <summary>
    /// Represents a control that displays a <see cref="PlotModel" />.
    /// </summary>
    public partial class Plot : PlotBase, IPlot
    {
        /// <summary>
        /// The internal model.
        /// </summary>
        private readonly PlotModel internalModel;

        /// <summary>
        /// The default controller.
        /// </summary>
        private readonly IPlotController defaultController;

        /// <summary>
        /// Initializes a new instance of the <see cref="Plot" /> class.
        /// </summary>
        public Plot()
        {
            series = new ObservableCollection<Series>();
            axes = new ObservableCollection<Axis>();
            annotations = new ObservableCollection<Annotation>();
            legends = new ObservableCollection<Legend>();

            series.CollectionChanged += OnSeriesChanged;
            axes.CollectionChanged += OnAxesChanged;
            annotations.CollectionChanged += OnAnnotationsChanged;
            legends.CollectionChanged += this.OnAnnotationsChanged;

            defaultController = new PlotController();
            internalModel = new PlotModel();
            ((IPlotModel)internalModel).AttachPlotView(this);
        }

        /// <summary>
        /// Gets the annotations.
        /// </summary>
        /// <value>The annotations.</value>
        public ObservableCollection<Annotation> Annotations
        {
            get
            {
                return annotations;
            }
        }

        /// <summary>
        /// Gets the actual model.
        /// </summary>
        /// <value>The actual model.</value>
        public override PlotModel ActualModel
        {
            get
            {
                return internalModel;
            }
        }

        /// <summary>
        /// Gets the actual Plot controller.
        /// </summary>
        /// <value>The actual Plot controller.</value>
        public override IPlotController ActualController
        {
            get
            {
                return defaultController;
            }
        }

        /// <summary>
        /// Updates the model. If Model==<c>null</c>, an internal model will be created. The ActualModel.Update will be called (updates all series data).
        /// </summary>
        /// <param name="updateData">if set to <c>true</c> , all data collections will be updated.</param>
        protected new void UpdateModel(bool updateData = true)
        {
            SynchronizeProperties();
            SynchronizeSeries();
            SynchronizeAxes();
            SynchronizeAnnotations();
            SynchronizeLegends();

            // TODO: does this achieve anything? we always call InvalidatePlot after UpdateModel
            base.UpdateModel(updateData);
        }

        /// <summary>
        /// Called when the visual appearance is changed.
        /// </summary>
        protected void OnAppearanceChanged()
        {
            UpdateModel(false);
            InvalidatePlot(false);
        }

        void IPlot.ElementAppearanceChanged(object element)
        {
            // TODO: determine type of element to perform a more fine-grained update
            this.UpdateModel(false);
            base.InvalidatePlot(false);
        }

        void IPlot.ElementDataChanged(object element)
        {
            // TODO: determine type of element to perform a more fine-grained update
            this.UpdateModel(true);
            base.InvalidatePlot(true);
        }


        /// <summary>
        /// Called when the visual appearance is changed.
        /// </summary>
        /// <param name="d">The d.</param>
        /// <param name="e">The <see cref="System.Windows.DependencyPropertyChangedEventArgs" /> instance containing the event data.</param>
        private static void AppearanceChanged(global::Avalonia.AvaloniaObject d, global::Avalonia.AvaloniaPropertyChangedEventArgs e)
        {
            ((Plot)d).OnAppearanceChanged();
        }

        /// <summary>
        /// Called when annotations is changed.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="System.Collections.Specialized.NotifyCollectionChangedEventArgs" /> instance containing the event data.</param>
        private void OnAnnotationsChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            SyncLogicalTree(e);
        }

        /// <summary>
        /// Called when axes is changed.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="System.Collections.Specialized.NotifyCollectionChangedEventArgs" /> instance containing the event data.</param>
        private void OnAxesChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            SyncLogicalTree(e);
        }

        /// <summary>
        /// Called when series is changed.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="System.Collections.Specialized.NotifyCollectionChangedEventArgs" /> instance containing the event data.</param>
        private void OnSeriesChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            SyncLogicalTree(e);
        }

        /// <summary>
        /// Synchronizes the logical tree.
        /// </summary>
        /// <param name="e">The <see cref="System.Collections.Specialized.NotifyCollectionChangedEventArgs" /> instance containing the event data.</param>
        private void SyncLogicalTree(NotifyCollectionChangedEventArgs e)
        {
            // In order to get DataContext and binding to work with the series, axes and annotations
            // we add the items to the logical tree
            if (e.NewItems != null)
            {
                foreach (var item in e.NewItems.OfType<ISetLogicalParent>())
                {
                    item.SetParent(this);
                }
                LogicalChildren.AddRange(e.NewItems.OfType<ILogical>());
                VisualChildren.AddRange(e.NewItems.OfType<Visual>());
            }

            if (e.OldItems != null)
            {
                foreach (var item in e.OldItems.OfType<ISetLogicalParent>())
                {
                    item.SetParent(null);
                }

                foreach (var item in e.OldItems)
                {
                    LogicalChildren.Remove((ILogical)item);
                    VisualChildren.Remove((Visual)item);
                }
            }

            this.OnAppearanceChanged();
        }

        /// <summary>
        /// Synchronize properties in the internal Plot model
        /// </summary>
        private void SynchronizeProperties()
        {
            var m = internalModel;

            m.PlotType = PlotType;

            m.PlotMargins = PlotMargins.ToOxyThickness();
            m.Padding = Padding.ToOxyThickness();
            m.TitlePadding = TitlePadding;

            m.Culture = Culture;

            m.DefaultColors = DefaultColors.Select(c => c.ToOxyColor()).ToArray();
            m.DefaultFont = DefaultFont;
            m.DefaultFontSize = DefaultFontSize;

            m.Title = Title;
            m.TitleColor = TitleColor.ToOxyColor();
            m.TitleFont = TitleFont;
            m.TitleFontSize = TitleFontSize;
            m.TitleFontWeight = (int)TitleFontWeight;
            m.TitleToolTip = TitleToolTip;

            m.Subtitle = Subtitle;
            m.SubtitleColor = SubtitleColor.ToOxyColor();
            m.SubtitleFont = SubtitleFont;
            m.SubtitleFontSize = SubtitleFontSize;
            m.SubtitleFontWeight = (int)SubtitleFontWeight;

            m.TextColor = TextColor.ToOxyColor();
            m.SelectionColor = SelectionColor.ToOxyColor();

            m.RenderingDecorator = RenderingDecorator;

            m.AxisTierDistance = AxisTierDistance;

            m.IsLegendVisible = IsLegendVisible;

            m.PlotAreaBackground = PlotAreaBackground.ToOxyColor();
            m.PlotAreaBorderColor = PlotAreaBorderColor.ToOxyColor();
            m.PlotAreaBorderThickness = PlotAreaBorderThickness.ToOxyThickness();
        }

        /// <summary>
        /// Synchronizes the annotations in the internal model.
        /// </summary>
        private void SynchronizeAnnotations()
        {
            internalModel.Annotations.Clear();
            foreach (var a in Annotations)
            {
                internalModel.Annotations.Add(a.CreateModel());
            }
        }

        /// <summary>
        /// Synchronizes the axes in the internal model.
        /// </summary>
        private void SynchronizeAxes()
        {
            internalModel.Axes.Clear();
            foreach (var a in Axes)
            {
                internalModel.Axes.Add(a.CreateModel());
            }
        }

        /// <summary>
        /// Synchronizes the series in the internal model.
        /// </summary>
        private void SynchronizeSeries()
        {
            internalModel.Series.Clear();
            foreach (var s in Series)
            {
                internalModel.Series.Add(s.CreateModel());
            }
        }

        /// <summary>
        /// Synchronizes the legends in the internal model.
        /// </summary>
        private void SynchronizeLegends()
        {
            internalModel.Legends.Clear();
            foreach (var l in Legends)
            {
                internalModel.Legends.Add(l.CreateModel());
            }
        }
    }
}
