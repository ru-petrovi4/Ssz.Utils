using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using Ssz.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ssz.Dcs.CentralServer_ClientWindowsService
{
    public partial class Worker
    {
        #region private functions        

        private void LaunchPlatInstructor(DirectoryInfo processDirectoryInfo, DirectoryInfo binDirectoryInfo, DirectoryInfo dataDirectoryInfo, string pathRelativeToDataDirectory, string instanceInfo)
        {   
            string exeFileFullName = Path.Combine(binDirectoryInfo.FullName, @"PlatInstructor.exe");
            string arguments = "\"" + Path.Combine(dataDirectoryInfo.FullName, pathRelativeToDataDirectory) +
                "\" /XISrv /s" +
                " --xiserveroptions=\"" + processDirectoryInfo.FullName + @"|" + instanceInfo + "\"";

            Logger.LogDebug("Dcs.PlatInstructorEngine is starting.. " + exeFileFullName + @" " + arguments);

            if (ConfigurationHelper.GetValue<bool>(Configuration, @"ShowPlatInstructor", false))
            {
                ProcessHelper.StartProcessAsCurrentUser(exeFileFullName, @" " + arguments, binDirectoryInfo.FullName, true);
            }
            else
            {
                var processStartInfo = new ProcessStartInfo(exeFileFullName, arguments)
                {
                    WorkingDirectory = binDirectoryInfo.FullName,
                    WindowStyle = ProcessWindowStyle.Hidden,
                };
                Process.Start(processStartInfo);
            }
        }

        #endregion
    }
}


//public partial class ClientWorker
//{
//    #region private functions        

//    private void LaunchPlatInstructor(string processModelingSessionId, DirectoryInfo binDirectoryInfo, DirectoryInfo dataDirectoryInfo, string pathRelativeToDataDirectory)
//    {
//        List<InstructorLauncherItem> registeredInstructorItems = LoadRegisteredInstructorItems();

//        InstructorLauncherItem? instructorLauncherItem = registeredInstructorItems.FirstOrDefault(i => String.Equals(i.ModelName, modelName, StringComparison.InvariantCultureIgnoreCase));
//        if (instructorLauncherItem is null)
//        {
//            Logger.LogWarning("ModelsManager::Run " + modelName + " is not installed");
//            return;
//        }

//        try
//        {
//            const string arg = @"/XISrv";
//            if (!instructorLauncherItem.PlatInstructorCommandLine.Contains(arg))
//                instructorLauncherItem.PlatInstructorCommandLine += (" " + arg);
//            var processStartInfo = new ProcessStartInfo(instructorLauncherItem.PlatInstructorExeFileFullName, instructorLauncherItem.PlatInstructorCommandLine)
//            {
//                WorkingDirectory = instructorLauncherItem.PlatInstructorWorkingDir,
//                WindowStyle = ProcessWindowStyle.Hidden,
//            };
//            /*
//            if (!string.IsNullOrEmpty(localModel.User))
//            {
//                procInfo.UserName = localModel.User;
//                procInfo.Password = new SecureString();
//                foreach (char c in localModel.Password)
//                    procInfo.Password.AppendChar(c);
//            }*/

//            Logger.LogDebug("Model starting.. " + instructorLauncherItem.PlatInstructorExeFileFullName + " " + instructorLauncherItem.PlatInstructorCommandLine);

//            Process.Start(processStartInfo);
//        }
//        catch (Exception ex)
//        {
//            Logger.LogError(ex, "ModelsManager::Run - Ошибка запуска модели ");
//            //MessageBox.Show(e.Message + "\nExePath=" + instructorLauncherItem.PlatInstructorExeFileFullName + " " +
//            //    instructorLauncherItem.PlatInstructorCommandLine,
//            //    "Ошибка запуска модели", MessageBoxButton.OK, MessageBoxImage.Error);
//        }
//    }

//    private List<InstructorLauncherItem> LoadRegisteredInstructorItems()
//    {
//        var modelInfosList = new List<InstructorLauncherItem>();
//        try
//        {
//            RegistryKey localMachineX32View = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32);
//            using (
//                RegistryKey? key = localMachineX32View.OpenSubKey(@"Software\Petrocom\Platform\Ver 1.6\Models",
//                    RegistryKeyPermissionCheck.ReadSubTree))
//            {
//                if (key is null)
//                {
//                    Logger.LogWarning(
//                        @"ModelsManager::RegisteredItems Software\Petrocom\Platform\Ver 1.6\Models Key=NULL");
//                    return modelInfosList;
//                }
//                foreach (string subKeyName in key.GetSubKeyNames())
//                {
//                    using (RegistryKey? keyModel = key.OpenSubKey(subKeyName, RegistryKeyPermissionCheck.ReadSubTree))
//                    {
//                        if (keyModel is null) continue;
//                        /*
//                        string platInstructorExeFileFullName = 
//                        if (!string.IsNullOrEmpty(platInstructorExeFileFullName) && !platInstructorExeFileFullName.StartsWith("\""))
//                            platInstructorExeFileFullName = "\"" + platInstructorExeFileFullName + "\"";*/
//                        var item = new InstructorLauncherItem
//                        {
//                            ModelId = (keyModel.GetValue("Id", "")?.ToString() ?? @"").Trim(),
//                            ModelName = subKeyName,
//                            ModelDescription =
//                                keyModel.GetValue("Description", "")?.ToString() ?? @"",
//                            PlatInstructorExeFileFullName = keyModel.GetValue("Module", "")?.ToString() ?? @"",
//                            PlatInstructorWorkingDir = keyModel.GetValue("WorkDir", "")?.ToString() ?? @"",
//                            PlatInstructorCommandLine = keyModel.GetValue("CmdLine", "")?.ToString() ?? @"",
//                        };
//                        if (string.IsNullOrEmpty(item.ModelId))
//                            throw new NullReferenceException("Instructor Id==NULL");

//                        modelInfosList.Add(item);
//                    }
//                }
//            }
//        }
//        catch (Exception exception)
//        {
//            Logger.LogError(exception, "ModelsManager::CheckRegisteredItems - " + exception.Message);
//            return modelInfosList;
//        }
//        ///RegisteredItems = modelInfosList.OrderBy(i => i.ModelDescription).ToArray();
//        return modelInfosList;
//    }

//    #endregion

//    private class InstructorLauncherItem
//    {
//        #region public functions

//        /// <summary>
//        ///     'Id' registry key
//        /// </summary>
//        public string ModelId { get; set; } = @"";

//        /// <summary>
//        ///     registry folder
//        /// </summary>
//        public string ModelName { get; set; } = @"";

//        /// <summary>
//        ///     'Description' registry key
//        /// </summary>
//        public string ModelDescription { get; set; } = @"";

//        public string Header { get { return ModelDescription; } }

//        /// <summary>
//        ///     'Module' registry key
//        /// </summary>
//        public string PlatInstructorExeFileFullName { get; set; } = @"";

//        /// <summary>
//        ///     'WorkDir' registry key
//        /// </summary>
//        public string PlatInstructorWorkingDir { get; set; } = @"";

//        /// <summary>
//        ///     'CmdLine' registry key
//        /// </summary>
//        public string PlatInstructorCommandLine { get; set; } = @"";

//        #endregion
//    }
//}