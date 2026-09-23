using CommunityToolkit.Mvvm.ComponentModel;

namespace Ssz.Operator.Core.ViewModels;

public abstract class ViewModelBase : ObservableObject
{
    public void ClearPropertyChangedEvent()
    {
        //PropertyChanged = null;
    }
}
