using System.Linq;
using Ssz.Operator.Core.Commands;
using Ssz.Operator.Core;
using Ssz.Operator.Core.ControlsCommon.Trends;

namespace Ssz.Operator.Core.ControlsCommon.Trends;

public class GenericTrendViewModel : TrendViewModel
{
    #region construction and destruction

    public GenericTrendViewModel(GenericTrendsViewModel trendsCollectionViewModel, Trend trend, int index) :
        base(trend)
    {
        _trendsCollectionViewModel = trendsCollectionViewModel;

        Index = index + 1;

        Source.PropertyChanged += Source_OnPropertyChanged;

        Source_OnPropertyChanged();
    }

    #endregion

    #region public functions

    public double VisibleValueRange
    {
        get { return _visibleValueRange; }
        private set { SetValue(ref _visibleValueRange, value); }
    }

    public double YMinWithPadding
    {
        get { return _minScaleWithPadding; }
        set { SetValue(ref _minScaleWithPadding, value); }
    }

    public double YMaxWithPadding
    {
        get { return _maxScaleWithPadding; }
        set { SetValue(ref _maxScaleWithPadding, value); }
    }

    public int Index { get; private set; }

    public void DisplayFaceplate()
    {
        string tag = Source.TagName;
        if (!string.IsNullOrWhiteSpace(tag))
        {
            CommandsManager.NotifyCommand(
                null,
                GenericCommands.ShowFaceplateCommand,
                new ShowFaceplateDsCommandOptions { ParamsString = tag});
        }
    }

    public override void OnSelectedTrendChanged()
    {
        base.OnSelectedTrendChanged();
        UpdatePoints();
    }

    public void UpdateMinimumMaximumBorders()
    {            
        VisibleValueRange = _trendsCollectionViewModel.ValueZoomLevel.VisibleRange(
            Source.YMin,
            Source.YMax);

        if (Source.YMin < Source.YMax)
        {
            YMinWithPadding = Source.YMin - VisibleValueRange/2;
            YMaxWithPadding = Source.YMax + VisibleValueRange/2;
        }

        Source.ValueFormat = FaceplateBaseControl.GetNumberFormat(Source.YMin, Source.YMax);
    }

    public void UpdatePoints()
    {
        if (_trendsCollectionViewModel.SelectedItem != null)
        {
            Points = GetScaledPoints(
                Source.YMin, Source.YMax,
                _trendsCollectionViewModel.SelectedItem.Source.YMin,
                _trendsCollectionViewModel.SelectedItem.Source.YMax);
        }
    }

    public void OnClicked(bool doubleClick)
    {
        _trendsCollectionViewModel.SelectedItem = this;

        if (doubleClick)
        {
            CommandsManager.NotifyCommand(null,
                GenericCommands.ShowSingleTrendCommand,
                new ShowSingleTrendDsCommandOptions
                {
                    HdaId = Source.HdaId,
                    ColorBrush = new SolidDsBrush(Color)
                });
        }
    }

    #endregion

    #region protected functions

    protected override void OnRawTrendPointsLoaded(TrendPoint[] points)
    {
        UpdatePoints();
    }

    #endregion

    #region private functions

    private void Source_OnPropertyChanged(DependencyPropertyChangedEventArgs args = default(DependencyPropertyChangedEventArgs))
    {
        if (args == default(DependencyPropertyChangedEventArgs) || args.Property == Trend.YMinProperty || args.Property == Trend.YMaxProperty)
        {
            UpdateMinimumMaximumBorders();
            foreach (GenericTrendViewModel trendViewModel in _trendsCollectionViewModel.Items.OfType<GenericTrendViewModel>())
                trendViewModel.UpdatePoints();
        }
    }

    #endregion

    #region private fields

    private readonly GenericTrendsViewModel _trendsCollectionViewModel;
    private double _visibleValueRange;
    private double _minScaleWithPadding;
    private double _maxScaleWithPadding = 100;

    #endregion
}