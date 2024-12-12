using System;
using System.Windows;
using Ssz.Operator.Core.DataAccess;

namespace Ssz.Operator.Core.Commands.DsCommandOptions
{
    public static class UrlHelper
    {
        #region public functions

        public static bool WebBrowserOnNavigating(string url,
            IDsItem senderItem,
            DataValueViewModel dataContext)
        {            
            /*
            string dsPageFileName = url + @".dspage";
            if (File.Exists(dsPageFileName))
            {
                var fileInfo = new FileInfo(dsPageFileName);
                string fileRelativePath = DsProject.Instance.GetFileRelativePath(fileInfo.FullName);
                if (!String.IsNullOrWhiteSpace(fileRelativePath))
                {
                    if (FileSystemHelper.Compare(fileInfo, drawingFileInfo)) return false;

                    CommandsManager.NotifyCommand(null, CommandsManager.JumpCommand, new JumpDsCommandOptions
                    {
                        TargetWindow =
                            TargetWindow.CurrentWindow,
                        FileRelativePath =
                            fileRelativePath
                    });

                    return true;
                }
            }*/

            var index = url.IndexOf("Command=");
            if (index == -1) return false;
            url = url.Substring(index);
            if (DsCommandValueSerializer.Instance.CanConvertFromString(url, null))
            {
                var dsCommand =
                    DsCommandValueSerializer.Instance.ConvertFromString(url, null) as DsCommand;
                if (dsCommand is not null)
                {
                    dsCommand.ParentItem = senderItem;
                    if (!string.IsNullOrWhiteSpace(dsCommand.Command))
                    {
                        Application.Current.Dispatcher.BeginInvoke(
                            new Action(new DsCommandView(null, dsCommand, dataContext).DoCommand));

                        return true;
                    }
                }
            }

            return false;
        }

        #endregion
    }
}