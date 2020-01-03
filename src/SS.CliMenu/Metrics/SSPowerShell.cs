using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http.Headers;
using System.Text;

namespace SS.CliMenu.Metrics
{
    public class SSPowerShell
    {
        public static readonly string AssemblyVersion = $"{typeof(SSPowerShell).Assembly.GetName().Version}";
        public static string ProfileDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), ".CliMenu");
        public static ProductInfoHeaderValue UserAgentValue = new ProductInfoHeaderValue("SSPowerShell", SSPowerShell.AssemblyVersion);
    }




}
