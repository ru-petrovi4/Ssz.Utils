﻿using System;
using System.Windows;
using Ssz.Operator.Core.ControlsCommon.Trends;
using Ssz.Operator.Core;
using System.Collections.Generic;
using Ssz.Operator.Core.DsShapes.Trends;
using Avalonia.Controls;

namespace Ssz.Operator.Core.ControlsCommon.Trends.GenericTrends
{
    /// <summary>
    ///     Interaction logic for TrendsControl.axaml
    /// </summary>
    public partial class TrendGroupControl : UserControl, IDisposable
    {
        #region construction and destruction

        public TrendGroupControl()
        {
            InitializeComponent();            

            Loaded += (s, a) =>
            {
                TrendGroupToolbar2.toolStripComboBox2.SelectedIndex = 0;
            };
            //TrendPeriodChanged();

            DataContext = new GenericTrendsViewModel(DateTime.Now);
        }

        ~TrendGroupControl()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                (DataContext as IDisposable)?.Dispose();
            }
        }

        #endregion

        #region public functions

        public void Jump(string groupId, string tag)
        {
            ((GenericTrendsViewModel)DataContext).LoadTrendGroup(groupId, tag);
        }

        public void Jump(IEnumerable<DsTrendItem> trendItemInfos)
        {
            ((GenericTrendsViewModel)DataContext).Display(trendItemInfos);
        }

        public void ChangeTrendColor(TrendViewModel? trendViewModel = null)
        {
            if (trendViewModel == null)
            {
                var trendsViewModel = DataContext as TrendsViewModel;
                if (trendsViewModel == null)
                    return;

                trendViewModel = trendsViewModel.SelectedItem;
                if (trendViewModel == null)
                    return;
            }

            Color? newColor = WpfColorDialog.Show(trendViewModel.Color);
            if (newColor != null)
            {
                trendViewModel.Source.DsTrendItem.DsBrush = new BrushDataBinding(false, true)
                {
                    ConstValue = new SolidDsBrush
                    {
                        Color = newColor.Value
                    }
                };
                trendViewModel.Source.Brush = new SolidColorBrush(newColor.Value);
            }
        }

        public void Save()
        {
            ((GenericTrendsViewModel)DataContext).Save();

            WpfMessageBox.Show(Window.GetWindow(this), "Сохранено");
        }

        public void TrendPeriodChanged()
        {
            switch (TrendGroupToolbar2.toolStripComboBox2.SelectedIndex)
            {
                case 0:
                    ((GenericTrendsViewModel)DataContext).Zoom(TimeSpan.FromMinutes(30));
                    break;
                case 1:
                    ((GenericTrendsViewModel)DataContext).Zoom(TimeSpan.FromMinutes(60));
                    break;
                case 2:
                    ((GenericTrendsViewModel)DataContext).Zoom(TimeSpan.FromMinutes(120));
                    break;
                case 3:
                    ((GenericTrendsViewModel)DataContext).Zoom(TimeSpan.FromMinutes(240));
                    break;
            }            
        }

        #endregion

        #region private functions

        private void OnChangeTrendColorClicked(object sender, RoutedEventArgs e)
        {
            var fe = sender as FrameworkElement;
            if (fe == null)
                return;

            var trendViewModel = fe.DataContext as TrendViewModel;
            if (trendViewModel == null)
                return;

            ChangeTrendColor(trendViewModel);
        }

        //private void onTagClicked(object sender, MouseButtonEventArgs e)
        //{
        //    var fe = sender as FrameworkElement;
        //    if (fe == null)
        //        return;

        //    var trendViewModel = fe.DataContext as TrendViewModel;
        //    if (trendViewModel == null)
        //        return;

        //    CommandsManager.NotifyCommand(RuntimeProject.LastActiveRootPlayWindow,
        //        GenericCommands.DisplayFaceplateForTagCommand, new GenericCommandProps
        //        {
        //            ParamsString = trendViewModel.Source.Tag
        //        });
        //}

        #endregion
    }
}