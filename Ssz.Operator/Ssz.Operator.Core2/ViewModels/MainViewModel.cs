using CommunityToolkit.Mvvm.ComponentModel;
using Ssz.Operator.Core;
using Ssz.Operator.Core.DataAccess;
using System.Collections.Generic;

namespace Ssz.Operator.Core.ViewModels;

public partial class MainViewModel : DataValueViewModel
{
    #region construction and destruction

    public MainViewModel() : base(null, false)
    {
    }

    #endregion    
}
