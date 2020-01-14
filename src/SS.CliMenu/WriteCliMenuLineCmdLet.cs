using SS.CliMenu.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Text;
using System.Threading.Tasks;

namespace SS.CliMenu
{
    /// <summary>
    /// 
    /// <para type="synopsis"></para>
    /// <para type="description"></para>
    /// <example>
    ///   <title>Default usage.</title>
    ///   <description></description>
    ///   <code>Write-CliMenuLine "Menu line item"</code>
    /// </example>
    /// <example>
    ///   <title>Default usage with script.</title>
    ///   <description></description>
    ///   <code>Write-CliMenuLine -Script { Write-Host "test" }</code>
    /// </example>
    /// </summary>
    [Cmdlet(VerbsCommunications.Write, "CliMenuLine")]
    public class WriteCliMenuLineCmdLet : UICmdLet
    {
        /// <summary>
        /// 
        /// <para type="description"></para>
        /// </summary>
        [Parameter(Mandatory = true, ParameterSetName = "ByText", ValueFromPipeline = true, Position = 0)]
        public string Text { get; set; }
        /// <summary>
        /// 
        /// <para type="description"></para>
        /// </summary>
        [Parameter(Mandatory = true, ParameterSetName = "ByScript")]
        public ScriptBlock Script { get; set; }
        /// <summary>
        /// 
        /// <para type="description"></para>
        /// </summary>
        [Parameter(Mandatory = true, ParameterSetName = "ByFunc")]
        public Action<CliMenuOptions> Func { get; set; }
        /// <summary>
        /// 
        /// <para type="description"></para>
        /// </summary>
        [Parameter]
        public ConsoleColor Color { get; set; } = System.ConsoleColor.White;
        /// <summary>
        /// 
        /// <para type="description"></para>
        /// </summary>
        [Parameter]
        public SwitchParameter IsMenuItem { get; set; }

        protected override string DataCollectionWarning => string.Empty;

        protected override string InstrumentationKey => string.Empty;

        protected override void InitializeQosEvent()
        {
            
        }
        protected override void LogCmdletStartInvocationInfo()
        {
            // We don't log anything for this cmdlet
        }
        protected override void LogCmdletEndInvocationInfo()
        {
            // We don't log anything for this cmdlet
        }
        protected override bool IsUsageMetricEnabled => false;
        protected override bool IsErrorMetricEnabled => false;
        protected override string ProductName => ".Default";

        /// <summary>
        /// 
        /// </summary>
        public override void ExecuteCmdlet()
        {
            if (Script != null)
                base.WriteMenuLine(Script, Color, IsMenuItem);
            if (Func != null)
                base.WriteMenuLine(Func, Color, IsMenuItem);
            else
                base.WriteMenuLine(Text, Color, IsMenuItem);

            base.ExecuteCmdlet();
        }
    }
}
