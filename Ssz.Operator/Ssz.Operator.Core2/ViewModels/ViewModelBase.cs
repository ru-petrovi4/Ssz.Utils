using CommunityToolkit.Mvvm.ComponentModel;

namespace Ssz.Operator.Play.ViewModels;

public abstract class ViewModelBase : ObservableObject
{
    public void ClearPropertyChangedEvent()
    {
        //PropertyChanged = null;
    }
}
