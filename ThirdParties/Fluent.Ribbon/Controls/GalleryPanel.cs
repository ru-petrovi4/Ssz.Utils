// ReSharper disable once CheckNamespace
namespace Fluent
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Data;
    using System.Windows.Media;
    using System.Windows.Threading;
    using Fluent.Extensions;
    using Fluent.Internal.KnownBoxes;

    /// <summary>
    /// Represents panel for Gallery and InRibbonGallery with grouping and filtering capabilities
    /// </summary>
    public class GalleryPanel : StackPanel
    {
        // todo: localization
        private const string Undefined = "Undefined";

        #region Fields

        // Currently used group containers
        private readonly List<GalleryGroupContainer> galleryGroupContainers = new List<GalleryGroupContainer>();

        // Designate that gallery panel must be refreshed its groups
        private bool needsRefresh;

        #endregion

        #region Properties

        #region IsGrouped

        /// <summary>
        /// Gets or sets whether gallery panel shows groups
        /// (Filter property still works as usual)
        /// </summary>
        public bool IsGrouped
        {
            get { return (bool)this.GetValue(IsGroupedProperty); }
            set { this.SetValue(IsGroupedProperty, BooleanBoxes.Box(value)); }
        }

        /// <summary>Identifies the <see cref="IsGrouped"/> dependency property.</summary>
        public static readonly DependencyProperty IsGroupedProperty =
            DependencyProperty.Register(nameof(IsGrouped), typeof(bool), typeof(GalleryPanel),
            new PropertyMetadata(BooleanBoxes.FalseBox, OnIsGroupedChanged));

        private static void OnIsGroupedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var galleryPanel = (GalleryPanel)d;
            galleryPanel.RefreshAsync();
        }

        #endregion

        #region GroupBy

        /// <summary>
        /// Gets or sets property name to group items
        /// </summary>
        public string? GroupBy
        {
            get { return (string?)this.GetValue(GroupByProperty); }
            set { this.SetValue(GroupByProperty, value); }
        }

        /// <summary>Identifies the <see cref="GroupBy"/> dependency property.</summary>
        public static readonly DependencyProperty GroupByProperty = DependencyProperty.Register(nameof(GroupBy), typeof(string), typeof(GalleryPanel), new PropertyMetadata(OnGroupByChanged));

        private static void OnGroupByChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var galleryPanel = (GalleryPanel)d;
            galleryPanel.RefreshAsync();
        }

        #endregion

        #region GroupByAdvanced

        /// <summary>
        /// Gets or sets name of property which
        /// will use to group items in the Gallery.
        /// </summary>
        public Func<object?, string>? GroupByAdvanced
        {
            get { return (Func<object?, string>?)this.GetValue(GroupByAdvancedProperty); }
            set { this.SetValue(GroupByAdvancedProperty, value); }
        }

        /// <summary>Identifies the <see cref="GroupByAdvanced"/> dependency property.</summary>
        public static readonly DependencyProperty GroupByAdvancedProperty = DependencyProperty.Register(nameof(GroupByAdvanced), typeof(Func<object?, string>), typeof(GalleryPanel), new PropertyMetadata(OnGroupByAdvancedChanged));

        private static void OnGroupByAdvancedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var galleryPanel = (GalleryPanel)d;
            galleryPanel.RefreshAsync();
        }

        #endregion

        #region ItemContainerGenerator

        /// <summary>
        /// Gets or sets ItemContainerGenerator which generates the
        /// user interface (UI) on behalf of its host, such as an  ItemsControl.
        /// </summary>
        public ItemContainerGenerator? ItemContainerGenerator
        {
            get { return (ItemContainerGenerator?)this.GetValue(ItemContainerGeneratorProperty); }
            set { this.SetValue(ItemContainerGeneratorProperty, value); }
        }

        /// <summary>Identifies the <see cref="ItemContainerGenerator"/> dependency property.</summary>
        public static readonly DependencyProperty ItemContainerGeneratorProperty =
            DependencyProperty.Register(nameof(ItemContainerGenerator), typeof(ItemContainerGenerator),
            typeof(GalleryPanel), new PropertyMetadata(OnItemContainerGeneratorChanged));

        private static void OnItemContainerGeneratorChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var galleryPanel = (GalleryPanel)d;
            galleryPanel.RefreshAsync();
        }

        #endregion

        #region ItemWidth

        /// <summary>
        /// Gets or sets a value that specifies the width of
        /// all items that are contained within
        /// </summary>
        public double ItemWidth
        {
            get { return (double)this.GetValue(ItemWidthProperty); }
            set { this.SetValue(ItemWidthProperty, value); }
        }

        /// <summary>Identifies the <see cref="ItemWidth"/> dependency property.</summary>
        public static readonly DependencyProperty ItemWidthProperty =
            DependencyProperty.Register(nameof(ItemWidth), typeof(double),
            typeof(GalleryPanel), new PropertyMetadata(DoubleBoxes.NaN));

        #endregion

        #region ItemHeight

        /// <summary>
        /// Gets or sets a value that specifies the height of
        /// all items that are contained within
        /// </summary>
        public double ItemHeight
        {
            get { return (double)this.GetValue(ItemHeightProperty); }
            set { this.SetValue(ItemHeightProperty, value); }
        }

        /// <summary>Identifies the <see cref="ItemHeight"/> dependency property.</summary>
        public static readonly DependencyProperty ItemHeightProperty =
            DependencyProperty.Register(nameof(ItemHeight), typeof(double),
            typeof(GalleryPanel), new PropertyMetadata(DoubleBoxes.NaN));

        #endregion

        #region Filter

        /// <summary>
        /// Gets or sets groups names separated by comma which must be shown
        /// </summary>
        public string? Filter
        {
            get { return (string?)this.GetValue(FilterProperty); }
            set { this.SetValue(FilterProperty, value); }
        }

        /// <summary>Identifies the <see cref="Filter"/> dependency property.</summary>
        public static readonly DependencyProperty FilterProperty =
            DependencyProperty.Register(nameof(Filter), typeof(string),
            typeof(GalleryPanel), new PropertyMetadata(OnFilterChanged));

        private static void OnFilterChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var galleryPanel = (GalleryPanel)d;
            galleryPanel.RefreshAsync();
        }

        #endregion

        #region MinItemsInRow

        /// <summary>
        /// Gets or sets maximum items quantity in row
        /// </summary>
        public int MinItemsInRow
        {
            get { return (int)this.GetValue(MinItemsInRowProperty); }
            set { this.SetValue(MinItemsInRowProperty, value); }
        }

        /// <summary>Identifies the <see cref="MinItemsInRow"/> dependency property.</summary>
        public static readonly DependencyProperty MinItemsInRowProperty =
            DependencyProperty.Register(nameof(MinItemsInRow), typeof(int),
            typeof(GalleryPanel), new FrameworkPropertyMetadata(1, FrameworkPropertyMetadataOptions.AffectsMeasure));

        #endregion

        #region MaxItemsInRow

        /// <summary>
        /// Gets or sets maximum items quantity in row
        /// </summary>
        public int MaxItemsInRow
        {
            get { return (int)this.GetValue(MaxItemsInRowProperty); }
            set { this.SetValue(MaxItemsInRowProperty, value); }
        }

        /// <summary>Identifies the <see cref="MaxItemsInRow"/> dependency property.</summary>
        public static readonly DependencyProperty MaxItemsInRowProperty = DependencyProperty.Register(nameof(MaxItemsInRow), typeof(int), typeof(GalleryPanel), new FrameworkPropertyMetadata(0, FrameworkPropertyMetadataOptions.AffectsMeasure));

        #endregion

        #endregion

        #region Initialization

        /// <summary>
        /// Default constructor
        /// </summary>
        public GalleryPanel()
        {
            this.visualCollection = new VisualCollection(this);

            this.Loaded += this.HandleGalleryPanel_Loaded;
        }

        private void HandleGalleryPanel_Loaded(object sender, RoutedEventArgs e)
        {
            this.Loaded -= this.HandleGalleryPanel_Loaded;
            this.Refresh();
        }

        #endregion

        #region Visual Tree

        private readonly VisualCollection visualCollection;

        /// <inheritdoc />
        protected override int VisualChildrenCount => base.VisualChildrenCount + this.visualCollection.Count;

        /// <inheritdoc />
        protected override Visual GetVisualChild(int index)
        {
            if (index < base.VisualChildrenCount)
            {
                return base.GetVisualChild(index);
            }

            return this.visualCollection[index - base.VisualChildrenCount];
        }

        #endregion

        #region Refresh

        private bool areUpdatesSuspsended;

        /// <summary>
        /// Suspends updates.
        /// </summary>
        public void SuspendUpdates()
        {
            this.areUpdatesSuspsended = true;
        }

        /// <summary>
        /// Resumes updates.
        /// </summary>
        public void ResumeUpdates()
        {
            this.areUpdatesSuspsended = false;
        }

        /// <summary>
        /// Resumes updates and calls <see cref="Refresh"/>.
        /// </summary>
        public void ResumeUpdatesRefresh()
        {
            this.ResumeUpdates();
            this.Refresh();
        }

        private void RefreshAsync()
        {
            if (this.needsRefresh
                || this.areUpdatesSuspsended)
            {
                return;
            }

            this.needsRefresh = true;
            this.RunInDispatcherAsync(() =>
                                      {
                                          if (this.needsRefresh == false)
                                          {
                                              return;
                                          }

                                          this.Refresh();
                                          this.needsRefresh = false;
                                      }, DispatcherPriority.Send);
        }

        private void Refresh()
        {
            if (this.areUpdatesSuspsended)
            {
                return;
            }

            // Clear currently used group containers
            // and supply with new generated ones
            foreach (var galleryGroupContainer in this.galleryGroupContainers)
            {
                BindingOperations.ClearAllBindings(galleryGroupContainer);
                this.visualCollection.Remove(galleryGroupContainer);
            }

            this.galleryGroupContainers.Clear();

            // Gets filters
            var filter = this.Filter?.Split(',');

            var dictionary = new Dictionary<string, GalleryGroupContainer>();

            foreach (UIElement? item in this.InternalChildren)
            {
                if (item is null)
                {
                    continue;
                }

                // Resolve group name
                string? propertyValue = null;

                if (this.GroupByAdvanced is not null)
                {
                    propertyValue = this.ItemContainerGenerator is null
                        ? this.GroupByAdvanced(item)
                        : this.GroupByAdvanced(this.ItemContainerGenerator.ItemFromContainerOrContainerContent(item));
                }
                else if (string.IsNullOrEmpty(this.GroupBy) == false)
                {
                    propertyValue = this.ItemContainerGenerator is null
                        ? this.GetPropertyValueAsString(item)
                        : this.GetPropertyValueAsString(this.ItemContainerGenerator.ItemFromContainerOrContainerContent(item));
                }

                if (propertyValue is null)
                {
                    propertyValue = Undefined;
                }

                // Make invisible if it is not in filter (or is not grouped)
                if (this.IsGrouped == false
                    || (filter is not null && filter.Contains(propertyValue) == false))
                {
                    item.Measure(new Size(0, 0));
                    item.Arrange(new Rect(0, 0, 0, 0));
                }

                // Skip if it is not in filter
                if (filter is not null
                    && filter.Contains(propertyValue) == false)
                {
                    continue;
                }

                // To put all items in one group in case of IsGrouped = False
                if (this.IsGrouped == false)
                {
                    propertyValue = Undefined;
                }

                if (dictionary.ContainsKey(propertyValue) == false)
                {
                    var galleryGroupContainer = new GalleryGroupContainer
                    {
                        Header = propertyValue
                    };
                    RibbonControl.Bind(this, galleryGroupContainer, nameof(this.Orientation), GalleryGroupContainer.OrientationProperty, BindingMode.OneWay);
                    RibbonControl.Bind(this, galleryGroupContainer, nameof(this.ItemWidth), GalleryGroupContainer.ItemWidthProperty, BindingMode.OneWay);
                    RibbonControl.Bind(this, galleryGroupContainer, nameof(this.ItemHeight), GalleryGroupContainer.ItemHeightProperty, BindingMode.OneWay);
                    RibbonControl.Bind(this, galleryGroupContainer, nameof(this.MaxItemsInRow), GalleryGroupContainer.MaxItemsInRowProperty, BindingMode.OneWay);
                    RibbonControl.Bind(this, galleryGroupContainer, nameof(this.MinItemsInRow), GalleryGroupContainer.MinItemsInRowProperty, BindingMode.OneWay);
                    dictionary.Add(propertyValue, galleryGroupContainer);
                    this.galleryGroupContainers.Add(galleryGroupContainer);

                    this.visualCollection.Add(galleryGroupContainer);
                }

                var galleryItemPlaceholder = new GalleryItemPlaceholder(item);
                dictionary[propertyValue].Items.Add(galleryItemPlaceholder);
            }

            if ((this.IsGrouped == false || (this.GroupBy is null && this.GroupByAdvanced is null))
                && this.galleryGroupContainers.Count != 0)
            {
                // Make it without headers if there is only one group or if we are not supposed to group
                this.galleryGroupContainers[0].IsHeadered = false;
            }

            this.InvalidateMeasure();
        }

        /// <inheritdoc />
        protected override void OnVisualChildrenChanged(DependencyObject visualAdded, DependencyObject visualRemoved)
        {
            base.OnVisualChildrenChanged(visualAdded, visualRemoved);

            if (visualRemoved is GalleryGroupContainer)
            {
                return;
            }

            if (visualAdded is GalleryGroupContainer)
            {
                return;
            }

            this.RefreshAsync();
        }

        #endregion

        #region Layout Overrides

        /// <inheritdoc />
        protected override Size MeasureOverride(Size availableSize)
        {
            double width = 0;
            double height = 0;
            foreach (var child in this.galleryGroupContainers)
            {
                child.Measure(availableSize);
                height += child.DesiredSize.Height;
                width = Math.Max(width, child.DesiredSize.Width);
            }

            var size = new Size(width, height);

            return size;
        }

        /// <inheritdoc />
        protected override Size ArrangeOverride(Size finalSize)
        {
            var finalRect = new Rect(finalSize);

            foreach (var item in this.galleryGroupContainers)
            {
                finalRect.Height = item.DesiredSize.Height;
                finalRect.Width = Math.Max(finalSize.Width, item.DesiredSize.Width);

                // Arrange a container to arrange placeholders
                item.Arrange(finalRect);

                finalRect.Y += item.DesiredSize.Height;

                // Now arrange our actual items using arranged size of placeholders
                foreach (GalleryItemPlaceholder? placeholder in item.Items)
                {
                    if (placeholder is null)
                    {
                        continue;
                    }

                    var leftTop = placeholder.TranslatePoint(default, this);

                    placeholder.Target.Arrange(new Rect(leftTop.X, leftTop.Y, placeholder.ArrangedSize.Width, placeholder.ArrangedSize.Height));
                }
            }

            return finalSize;
        }

        #endregion

        #region Private Methods

        private string? GetPropertyValueAsString(object? item)
        {
            if (item is null
                || this.GroupBy is null)
            {
                return Undefined;
            }

            var property = item.GetType().GetProperty(this.GroupBy, BindingFlags.Public | BindingFlags.Instance);

            var result = property?.GetValue(item, null);
            if (result is null)
            {
                return Undefined;
            }

            return result.ToString();
        }

        #endregion

        /// <inheritdoc />
        protected override IEnumerator LogicalChildren
        {
            get
            {
                var count = this.VisualChildrenCount;

                for (var i = 0; i < count; i++)
                {
                    yield return this.GetVisualChild(i);
                }
            }
        }
    }
}