using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Ssz.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Ssz.Dcs.CentralServer.Common.Helpers;

public static class LoggerHelper
{
    public static void LogProgramInformation(
        ILogger logger,
        IConfiguration configuration,
        string[] args,
        string environmentName
        )
    {
        logger.LogInformation($"App starting with args: \"{String.Join(" ", args)}\"; Environment: {environmentName}; Working Directory: \"{Directory.GetCurrentDirectory()}\"; Workstation Name: {ConfigurationHelper.GetWorkstationName(configuration)}");
    }
}
