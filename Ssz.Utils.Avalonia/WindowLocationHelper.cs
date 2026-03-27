using Avalonia;
using Avalonia.Controls;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace Ssz.Utils.Avalonia;

public static class WindowLocationHelper
{
    // Кроссплатформенный путь: 
    // Windows: C:\Users\%User%\AppData\Roaming\Ssz\LocationMindfulWindows.json
    // Linux: ~/.config/Ssz/LocationMindfulWindows.json
    private static readonly string SettingsFilePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "Ssz",
        "LocationMindfulWindows.json");

    private static Dictionary<string, SavedRect> _savedLocations = new();
    private static readonly Dictionary<Window, WindowInfo> WindowInfosDictionary = new();
    private static readonly Dictionary<string, List<WindowSlot>> WindowSlotsDictionary = new(StringComparer.OrdinalIgnoreCase);

    static WindowLocationHelper()
    {
        LoadSettings();
    }

    #region public functions

    public static void InitializeWindow(Window window, string category, bool rememberSize, double initialWidth = double.NaN, double initialHeight = double.NaN)
    {
        category ??= "";

        if (!WindowSlotsDictionary.TryGetValue(category, out var windowSlots))
        {
            windowSlots = new List<WindowSlot>();
            WindowSlotsDictionary[category] = windowSlots;
        }

        WindowSlot? freeWindowSlot = windowSlots.FirstOrDefault(slot => slot.Window is null);
        if (freeWindowSlot is null)
        {
            // Инициализация координат "пустым" значением (в Avalonia PixelPoint работает с int)
            var rect = new SavedRect { X = int.MinValue, Y = int.MinValue, Width = double.NaN, Height = double.NaN };

            if (!string.IsNullOrEmpty(category) && _savedLocations.TryGetValue(category, out var savedRect))
            {
                rect = new SavedRect
                {
                    X = savedRect.X,
                    Y = savedRect.Y,
                    Width = savedRect.Width < 5 ? double.NaN : savedRect.Width,
                    Height = savedRect.Height < 5 ? double.NaN : savedRect.Height
                };
            }

            freeWindowSlot = new WindowSlot
            {
                Num = windowSlots.Count,
                Location = rect
            };
            windowSlots.Add(freeWindowSlot);
        }

        freeWindowSlot.Window = window;
        var windowInfo = new WindowInfo(category, freeWindowSlot.Num);
        WindowInfosDictionary[window] = windowInfo;

        SavedRect slotLocation = freeWindowSlot.Location;

        if (double.IsNaN(slotLocation.Width) && !double.IsNaN(initialWidth))
            slotLocation.Width = initialWidth;

        if (double.IsNaN(slotLocation.Height) && !double.IsNaN(initialHeight))
            slotLocation.Height = initialHeight;

        // Avalonia использует структуру PixelPoint для X и Y
        if (slotLocation.X != int.MinValue && slotLocation.Y != int.MinValue)
        {
            window.WindowStartupLocation = WindowStartupLocation.Manual;
            window.Position = new PixelPoint(slotLocation.X, slotLocation.Y);
        }

        if (rememberSize)
        {
            if (!double.IsNaN(slotLocation.Width))
            {
                window.WindowStartupLocation = WindowStartupLocation.Manual;
                window.Width = slotLocation.Width;
            }
            if (!double.IsNaN(slotLocation.Height))
            {
                window.WindowStartupLocation = WindowStartupLocation.Manual;
                window.Height = slotLocation.Height;
            }
        }

        // В Avalonia используем Opened вместо Loaded для корректировки финальных координат
        window.Opened += (sender, args) => WindowOnOpened(window);
        // Closed не будет работать.
        window.Closing += (sender, args) => WindowOnClosing(window);
    }

    public static Window? TryActivateExistingWindow(string category)
    {
        category ??= "";

        if (!WindowSlotsDictionary.TryGetValue(category, out var windowSlots))
            return null;

        WindowSlot? occupiedWindowSlot = windowSlots.FirstOrDefault(slot => slot.Window is not null);
        if (occupiedWindowSlot is null)
            return null;

        occupiedWindowSlot.Window?.Activate();
        return occupiedWindowSlot.Window;
    }

    public static void ResetWindowsSettings()
    {
        _savedLocations.Clear();
        SaveSettings();
    }

    #endregion

    #region private functions

    private static void WindowOnOpened(Window window)
    {
        var position = window.Position;
        var centerPoint = new PixelPoint(
            position.X + (int)(window.Bounds.Width / 2),
            position.Y + (int)(window.Bounds.Height / 2));

        // В Avalonia 11 доступ к экранам осуществляется через инстанс Window
        var screen = window.Screens.ScreenFromPoint(centerPoint) ?? window.Screens.Primary;
        if (screen is null) return;

        var workingArea = screen.WorkingArea;

        double width = window.Bounds.Width;
        double height = window.Bounds.Height;

        if (width > workingArea.Width) width = workingArea.Width;
        if (height > workingArea.Height) height = workingArea.Height;

        int newX = position.X;
        int newY = position.Y;

        if (newX < workingArea.X) newX = workingArea.X;
        if (newY < workingArea.Y) newY = workingArea.Y;

        if (newX + width > workingArea.X + workingArea.Width)
            newX = (int)(workingArea.X + workingArea.Width - width);

        if (newY + height > workingArea.Y + workingArea.Height)
            newY = (int)(workingArea.Y + workingArea.Height - height);

        // Если окно за пределами видимости, возвращаем его в рабочую зону экрана
        window.Position = new PixelPoint(newX, newY);
        window.Width = width;
        window.Height = height;
    }

    private static void WindowOnClosing(Window window)
    {
        if (!WindowInfosDictionary.TryGetValue(window, out WindowInfo? windowInfo))
            return;

        // Не сохраняем позицию, если окно свернуто (значения будут мусорными)
        if (window.WindowState == WindowState.Minimized)
            return;

        WindowInfosDictionary.Remove(window);

        List<WindowSlot> windowSlots = WindowSlotsDictionary[windowInfo.Category];

        var rect = new SavedRect
        {
            X = window.Position.X,
            Y = window.Position.Y,
            // Bounds содержит фактические размеры окна при закрытии
            Width = window.Bounds.Width,
            Height = window.Bounds.Height
        };

        var windowSlot = windowSlots[windowInfo.SlotNum];
        windowSlot.Window = null;
        windowSlot.Location = rect;

        if (!string.IsNullOrEmpty(windowInfo.Category))
        {
            _savedLocations[windowInfo.Category] = rect;
            SaveSettings();
        }
    }

    private static void LoadSettings()
    {
        try
        {
            if (File.Exists(SettingsFilePath))
            {
                var json = File.ReadAllText(SettingsFilePath);
                _savedLocations = JsonSerializer.Deserialize<Dictionary<string, SavedRect>>(json) ?? new();
            }
        }
        catch
        {
            _savedLocations = new Dictionary<string, SavedRect>();
        }
    }

    private static void SaveSettings()
    {
        try
        {
            var directory = Path.GetDirectoryName(SettingsFilePath);
            if (directory != null) Directory.CreateDirectory(directory);

            var json = JsonSerializer.Serialize(_savedLocations, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(SettingsFilePath, json);
        }
        catch
        {
            // Игнорируем ошибки доступа к файловой системе при сохранении
        }
    }

    #endregion

    #region private classes

    private class SavedRect
    {
        public double Width { get; set; }
        public double Height { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
    }

    private class WindowSlot
    {
        public int Num { get; set; }
        public Window? Window { get; set; }
        public SavedRect Location { get; set; } = new();
    }

    private class WindowInfo
    {
        public WindowInfo(string category, int slotNum)
        {
            Category = category;
            SlotNum = slotNum;
        }

        public string Category { get; }
        public int SlotNum { get; }
    }

    #endregion
}