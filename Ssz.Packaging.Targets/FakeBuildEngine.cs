using Microsoft.Build.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ssz.Packaging.Targets
{
    public class FakeBuildEngine : IBuildEngine
    {

        // It's just a test helper so public fields is fine.
#pragma warning disable SA1401 // Fields should be private
        public List<BuildErrorEventArgs> LogErrorEvents = new List<BuildErrorEventArgs>();

        public List<BuildMessageEventArgs> LogMessageEvents =
            new List<BuildMessageEventArgs>();

        public List<CustomBuildEventArgs> LogCustomEvents =
            new List<CustomBuildEventArgs>();

        public List<BuildWarningEventArgs> LogWarningEvents =
            new List<BuildWarningEventArgs>();
#pragma warning restore SA1401 // Fields should be private

        public int ColumnNumberOfTaskNode
        {
            get { return 0; }
        }

        public bool ContinueOnError
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public int LineNumberOfTaskNode
        {
            get { return 0; }
        }

        public string ProjectFileOfTaskNode
        {
            get { return "fake ProjectFileOfTaskNode"; }
        }

        public bool BuildProjectFile(
            string projectFileName,
            string[] targetNames,
            System.Collections.IDictionary globalProperties,
            System.Collections.IDictionary targetOutputs)
        {
            throw new NotImplementedException();
        }        

        public void LogCustomEvent(CustomBuildEventArgs e)
        {
            this.LogCustomEvents.Add(e);
        }

        public void LogErrorEvent(BuildErrorEventArgs e)
        {
            this.LogErrorEvents.Add(e);
        }

        public void LogMessageEvent(BuildMessageEventArgs e)
        {
            this.LogMessageEvents.Add(e);
        }

        public void LogWarningEvent(BuildWarningEventArgs e)
        {
            this.LogWarningEvents.Add(e);
        }
    }
}
