using System.Threading.Tasks;
using Avalonia.Controls;
using Ssz.Operator.Core.DataAccess;

namespace Ssz.Operator.Core.Design.Views;

/// <summary>
///     Only a frame around <see cref="DesignMainView" />: everything the editor shows lives in that
///     view, so that the browser, which has no windows, shows exactly the same editor.
/// </summary>
public partial class DesignMainWindow : Window
{
    #region construction and destruction

    public DesignMainWindow()
    {
        InitializeComponent();
    }

    #endregion

    #region protected functions

    /// <summary>
    ///     Closing the editor closes the project first, which is where the author is asked about the
    ///     drawings they have not saved.
    ///     <para>
    ///         Ported from the WPF editor. There the question was asked while the window was closing;
    ///         Avalonia cannot hold a window open on an answer, so the closing is held back and taken
    ///         up again once the project has been closed.
    ///     </para>
    /// </summary>
    protected override void OnClosing(WindowClosingEventArgs e)
    {
        base.OnClosing(e);

        if (_closing || !DsProject.Instance.IsInitialized) return;

        e.Cancel = true;

        _ = CloseDsProjectAndWindowAsync();
    }

    #endregion

    #region private functions

    private async Task CloseDsProjectAndWindowAsync()
    {
        // The author may still say no to losing what they have not saved.
        if (await DesignDsProjectViewModel.Instance.PrepareCloseDsProjectAsync()) return;

        await DesignDsProjectViewModel.Instance.CloseDsProjectAsync();

        DesignDsProjectViewModel.Instance.Dispose();

        await DsDataAccessProvider.StaticDisposeAsync();

        _closing = true;
        Close();
    }

    #endregion

    #region private fields

    private bool _closing;

    #endregion
}
