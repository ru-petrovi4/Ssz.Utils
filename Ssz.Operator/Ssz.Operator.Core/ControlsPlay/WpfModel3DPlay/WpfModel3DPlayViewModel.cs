using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;
using HelixToolkit.Wpf;
using Ssz.Utils;
using Ssz.Utils.Wpf;

namespace Ssz.Operator.Core.ControlsPlay.WpfModel3DPlay
{
    public class WpfModel3DPlayViewModel : ViewModelBase
    {
        #region private functions

        private async Task<Model3DGroup?> LoadModel3DAsync(string model3DFileFullName)
        {
            return await Task.Run(() =>
            {
                try
                {
                    var mi = new ModelImporter();
                    return mi.Load(model3DFileFullName, null, true);
                }
                catch (Exception)
                {
                    return null;
                }
            });
        }

        #endregion

        #region public functions

        public string Title
        {
            get => _title;
            set => SetValue(ref _title, value);
        }

        public string SubTitle
        {
            get => _subTitle;
            set => SetValue(ref _subTitle, value);
        }

        public bool IsBusy
        {
            get => _isBusy;
            set => SetValue(ref _isBusy, value);
        }

        public Model3D? CurrentModel
        {
            get => _currentModel;
            set => SetValue(ref _currentModel, value);
        }

        public async void JumpAsync(string model3DFileFullName)
        {
            Title = Path.GetFileNameWithoutExtension(model3DFileFullName);
            SubTitle = "";
            IsBusy = true;
            CurrentModel = await LoadModel3DAsync(model3DFileFullName);
            IsBusy = false;
        }

        #endregion

        #region private fields

        private Model3D? _currentModel;
        private bool _isBusy;
        private string _subTitle = @"";
        private string _title = @"";

        #endregion
    }
}