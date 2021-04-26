﻿// ReSharper disable once CheckNamespace
namespace Fluent
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Globalization;
    using System.Linq;
    using System.Reflection;
    using System.Runtime.CompilerServices;
    using Fluent.Localization;

    /// <summary>
    /// Contains localizable Ribbon's properties.
    /// Set Culture property to change current Ribbon localization or
    /// set properties independently to use your localization
    /// </summary>
    public class RibbonLocalization : INotifyPropertyChanged
    {
        private CultureInfo culture;

        private RibbonLocalizationBase localization;

        #region Implementation of INotifyPropertyChanged

        /// <inheritdoc />
        public event PropertyChangedEventHandler? PropertyChanged;

        /// <summary>
        /// Raises the <see cref="PropertyChanged"/> event.
        /// </summary>
        protected void RaisePropertyChanged([CallerMemberName] string? propertyName = null)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion

        /// <summary>
        /// Static instance of <see cref="RibbonLocalization"/> to ease it's usage in XAML.
        /// </summary>
        public static RibbonLocalization Current { get; } = new RibbonLocalization();

        /// <summary>
        /// Gets a map of all registered localization classes.
        /// </summary>
        /// <remarks>
        /// The key of items in this dictionary should be the CultureName.
        /// </remarks>
        public Dictionary<string, Type> LocalizationMap { get; }

        /// <summary>
        /// Gets or sets current culture used for localization.
        /// </summary>
        public CultureInfo Culture
        {
            get { return this.culture; }

            set
            {
                if (!Equals(this.culture, value))
                {
                    this.culture = value;
                    this.LoadCulture(this.culture);
                    this.RaisePropertyChanged();
                }
            }
        }

        /// <summary>
        /// Gets or sets the current localization.
        /// </summary>
        public RibbonLocalizationBase Localization
        {
            get { return this.localization; }

            set
            {
                if (!Equals(this.localization, value))
                {
                    this.localization = value;
                    this.RaisePropertyChanged();
                }
            }
        }

        /// <summary>
        /// Default constructor
        /// </summary>
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        public RibbonLocalization()
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        {
            var localizationClasses = GetTypesInNamespace(Assembly.GetExecutingAssembly(), "Fluent.Localization.Languages");

            this.LocalizationMap = localizationClasses.ToDictionary(x => x.GetCustomAttribute<RibbonLocalizationAttribute>()!.CultureName, x => x);

            this.Culture = CultureInfo.CurrentUICulture;
        }

        private void LoadCulture(CultureInfo requestedCulture)
        {
            if (this.LocalizationMap.TryGetValue(requestedCulture.Name, out var localizationClass))
            {
                this.Localization = (RibbonLocalizationBase)Activator.CreateInstance(localizationClass)!;
                return;
            }

            if (this.LocalizationMap.TryGetValue(requestedCulture.TwoLetterISOLanguageName, out localizationClass))
            {
                this.Localization = (RibbonLocalizationBase)Activator.CreateInstance(localizationClass)!;
                return;
            }

            Trace.WriteLine($"Localization for culture \"{requestedCulture.DisplayName}\" with culture name \"{requestedCulture.Name}\" and LCID \"{requestedCulture.LCID}\" could not be found. Falling back to english.");

            this.Localization = RibbonLocalizationBase.FallbackLocalization;
        }

        // ReSharper disable once ReturnTypeCanBeEnumerable.Local
        private static IList<Type> GetTypesInNamespace(Assembly assembly, string nameSpace)
        {
            return assembly.GetTypes().Where(t => string.Equals(t.Namespace, nameSpace, StringComparison.Ordinal)).ToList();
        }
    }
}