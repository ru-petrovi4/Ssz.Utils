﻿using System.Collections.ObjectModel;
using System.Drawing;


namespace Egorozh.ColorPicker;

public class ColorPalette : ListBox, IColorClient
{
    #region Private Fields
    
    private readonly ObservableCollection<ColorListItem> _itemsSource;
    
    private IColorManager? _colorManager;

    private MenuItem _addContextMenuItem;
    private MenuItem _removeItem;

    private MenuItem _loadPaletteContextMenuItem;
    private MenuItem _savePaletteContextMenuItem;

    private readonly ColorListItem _addItem;

    #endregion
    
    #region Dependency Properties

    public static readonly StyledProperty<LoadPaletteHandlerAsync?> LoadPaletteHandlerProperty =
        AvaloniaProperty.Register<ColorPalette, LoadPaletteHandlerAsync?>(nameof(LoadPaletteHandler));

    public static readonly StyledProperty<SavePaletteHandler?> SavePaletteHandlerProperty =
        AvaloniaProperty.Register<ColorPalette, SavePaletteHandler?>(nameof(SavePaletteHandler));

    public static readonly StyledProperty<GetColorHandler?> GetColorHandlerProperty =
        AvaloniaProperty.Register<ColorPalette, GetColorHandler?>(nameof(GetColorHandler));

    public static readonly StyledProperty<IEnumerable<Avalonia.Media.Color>?> ColorsProperty =
        AvaloniaProperty.Register<ColorPalette, IEnumerable<Avalonia.Media.Color>?>(nameof(Colors));
    
    public static readonly StyledProperty<string> RemoveColorContextMenuTextProperty =
        AvaloniaProperty.Register<ColorPalette, string>(nameof(RemoveColorContextMenuText), "Remove color");

    public static readonly StyledProperty<string> RemoveColorsContextMenuTextProperty =
        AvaloniaProperty.Register<ColorPalette, string>(nameof(RemoveColorsContextMenuText), "Remove colors");

    public static readonly StyledProperty<string> AddColorContextMenuTextProperty =
        AvaloniaProperty.Register<ColorPalette, string>(nameof(AddColorContextMenuText), "Add color");

    public static readonly StyledProperty<DataTemplate> LoadPaletteIconTemplateProperty =
        AvaloniaProperty.Register<ColorPalette, DataTemplate>(nameof(LoadPaletteIconTemplate));

    public static readonly StyledProperty<DataTemplate> SavePaletteIconTemplateProperty =
        AvaloniaProperty.Register<ColorPalette, DataTemplate>(nameof(SavePaletteIconTemplate));

    public static readonly StyledProperty<string> LoadPaletteContextMenuTextProperty =
        AvaloniaProperty.Register<ColorPalette, string>(nameof(LoadPaletteContextMenuText), "Load Palette");

    public static readonly StyledProperty<string> SavePaletteContextMenuTextProperty =
        AvaloniaProperty.Register<ColorPalette, string>(nameof(SavePaletteContextMenuText), "Save Palette");

    private Color _prevColorForDoubleTappedHandler;

    #endregion

    #region Public Properties

    public LoadPaletteHandlerAsync? LoadPaletteHandler
    {
        get => GetValue(LoadPaletteHandlerProperty);
        set => SetValue(LoadPaletteHandlerProperty, value);
    }

    public SavePaletteHandler? SavePaletteHandler
    {
        get => GetValue(SavePaletteHandlerProperty);
        set => SetValue(SavePaletteHandlerProperty, value);
    }

    public GetColorHandler? GetColorHandler
    {
        get => GetValue(GetColorHandlerProperty);
        set => SetValue(GetColorHandlerProperty, value);
    }

    public IEnumerable<Avalonia.Media.Color>? Colors
    {
        get => GetValue(ColorsProperty);
        set => SetValue(ColorsProperty, value);
    }

    public string RemoveColorContextMenuText
    {
        get => GetValue(RemoveColorContextMenuTextProperty);
        private set => SetValue(RemoveColorContextMenuTextProperty, value);
    }

    public string RemoveColorsContextMenuText
    {
        get => GetValue(RemoveColorsContextMenuTextProperty);
        private set => SetValue(RemoveColorsContextMenuTextProperty, value);
    }

    public string AddColorContextMenuText
    {
        get => GetValue(AddColorContextMenuTextProperty);
        private set => SetValue(AddColorContextMenuTextProperty, value);
    }

    public DataTemplate LoadPaletteIconTemplate
    {
        get => GetValue(LoadPaletteIconTemplateProperty);
        private set => SetValue(LoadPaletteIconTemplateProperty, value);
    }

    public DataTemplate SavePaletteIconTemplate
    {
        get => GetValue(SavePaletteIconTemplateProperty);
        private set => SetValue(SavePaletteIconTemplateProperty, value);
    }

    public string LoadPaletteContextMenuText
    {
        get => GetValue(LoadPaletteContextMenuTextProperty);
        private set => SetValue(LoadPaletteContextMenuTextProperty, value);
    }

    public string SavePaletteContextMenuText
    {
        get => GetValue(SavePaletteContextMenuTextProperty);
        private set => SetValue(SavePaletteContextMenuTextProperty, value);
    }

    #endregion

    #region Constructor

    public ColorPalette()
    {
        SelectionMode = SelectionMode.Multiple;

        _addItem = new ColorListItem();
            
        _addItem.Classes.Add("Add");

        _addItem.Tapped += AddItemOnTapped;

        SelectionChanged += OnSelectionChanged;
        
        _itemsSource = new ObservableCollection<ColorListItem>();
        ItemsSource = _itemsSource;
    }

    #endregion

    #region Public Methods

    public void ColorUpdated(Color color, IColorClient client)
    {
    }

    public void Init(IColorManager colorManager)
    {
        _colorManager = colorManager;
    }

    #endregion

    #region Protected Methods

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);

        _addContextMenuItem = e.NameScope.Find<MenuItem>("PART_AddMenuItem");
        _removeItem = e.NameScope.Find<MenuItem>("PART_RemoveMenuItem");

        _loadPaletteContextMenuItem = e.NameScope.Find<MenuItem>("PART_LoadPaletteMenuItem");
        _savePaletteContextMenuItem = e.NameScope.Find<MenuItem>("PART_SavePaletteMenuItem");

        _removeItem.Click += RemoveItemOnClick;
        _addContextMenuItem.Click += AddItemOnClick;
        _loadPaletteContextMenuItem.Click += LoadPaletteContextMenuItemOnClick;
        _savePaletteContextMenuItem.Click += SavePaletteContextMenuItemOnClick;

        ColorsPropertyChanged();
    }

    
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == ColorsProperty)
        {
            ColorsPropertyChanged();
        }
    }
    
    #endregion

    #region Private Methods

    private void RemoveItemOnClick(object? sender, RoutedEventArgs e)
    {
        List<ColorListItem> removedItems = new();

        foreach (var selectedItem in SelectedItems)
        {
            if (selectedItem is ColorListItem removedItem && selectedItem != _addItem)
                removedItems.Add(removedItem);
        }

        foreach (var removedItem in removedItems)
            RemoveItem(removedItem);

        UpdateColors();
    }

    private async void AddItemOnTapped(object? sender, RoutedEventArgs e)
    {
        if (GetColorHandler == null)
            return;

        var (success, newColor) = await GetColorHandler.Invoke(_colorManager.CurrentColor.ToColor());

        if (!success)
            return;

        _itemsSource.Insert(_itemsSource.Count - 1, CreateColorItem(newColor));

        UpdateColors();
    }

    private void ColorsPropertyChanged()
    {
        if (Colors == null)
            return;

        _itemsSource.Clear();

        foreach (var color in Colors)
            _itemsSource.Add(CreateColorItem(color));

        _itemsSource.Add(_addItem);
    }

    private async void AddItemOnClick(object? sender, RoutedEventArgs e)
    {
        if (GetColorHandler == null)
            return;

        var (success, newColor) = await GetColorHandler.Invoke(_colorManager.CurrentColor.ToColor());

        if (!success)
            return;

        var newColorItem = CreateColorItem(newColor);

        if (SelectedItems.Count > 0)
        {
            ColorListItem? selectedItem = (ColorListItem?) SelectedItems[SelectedItems.Count - 1];

            if (selectedItem != _addItem)
            {
                var index = _itemsSource.IndexOf(selectedItem);

                _itemsSource.Insert(index, newColorItem);
            }
            else
            {
                _itemsSource.Insert(_itemsSource.Count - 1, newColorItem);
            }
        }
        else
        {
            _itemsSource.Insert(_itemsSource.Count - 1, newColorItem);
        }

        UpdateColors();
    }

    private async void ItemOnDoubleTapped(object? sender, RoutedEventArgs e)
    {
        if (sender is not ListBoxItem colorItem ||
            _colorManager == null || GetColorHandler == null)
            return;

        _colorManager.CurrentColor = _prevColorForDoubleTappedHandler;

        var color = GetColorFromItem(colorItem);

        (bool success, var newColor) = await GetColorHandler.Invoke(color);

        if (success)
        {
            colorItem.Background = new Avalonia.Media.SolidColorBrush(newColor);

            UpdateColors();
        }
    }

    private void ItemOnTapped(object? sender, RoutedEventArgs e)
    {
        if (sender is not ListBoxItem colorItem || _colorManager == null)
            return;

        var color = GetColorFromItem(colorItem);

        _prevColorForDoubleTappedHandler = _colorManager.CurrentColor;

        _colorManager.CurrentColor = color.ToColor();
    }

    private void OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        _removeItem.IsEnabled = SelectedItem != _addItem;

        _removeItem.Header = SelectedItems.Count > 1 ? RemoveColorsContextMenuText : RemoveColorContextMenuText;
    }

    private ColorListItem CreateColorItem(Avalonia.Media.Color color)
    {
        ColorListItem item = new(color);

        item.Tapped += ItemOnTapped;
        item.DoubleTapped += ItemOnDoubleTapped;

        return item;
    }

    private void RemoveItem(ColorListItem removedItem)
    {
        removedItem.Tapped -= ItemOnTapped;
        removedItem.DoubleTapped -= ItemOnDoubleTapped;

        _itemsSource.Remove(removedItem);
    }

    private void SavePaletteContextMenuItemOnClick(object? sender, RoutedEventArgs e)
    {
        SavePaletteHandler?.Invoke(Colors);
    }

    private async void LoadPaletteContextMenuItemOnClick(object? sender, RoutedEventArgs e)
    {
        if (LoadPaletteHandler != null)
        {
            var (success, colors) = await LoadPaletteHandler.Invoke();

            if (success)
                Colors = colors;
        }
    }

    private void UpdateColors()
    {
        if (ItemsSource is not IList<object> items)
            return;

        Colors = items.OfType<ListBoxItem>()
            .Where(item => item != _addItem)
            .Select(GetColorFromItem)
            .ToList();
    }

    private static Avalonia.Media.Color GetColorFromItem(ListBoxItem item)
    {
        var brush = item.Background as Avalonia.Media.SolidColorBrush;

        return brush.Color;
    }

    #endregion
}

public delegate Task<(bool, IEnumerable<Avalonia.Media.Color>)> LoadPaletteHandlerAsync();

public delegate void SavePaletteHandler(IEnumerable<Avalonia.Media.Color> colors);

public delegate Task<(bool, Avalonia.Media.Color)> GetColorHandler(Avalonia.Media.Color oldColor);