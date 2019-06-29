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
    /// Returns a CliMenuOptions object with all menu options. This CmdLet has no parameters
    /// <para type="synopsis">Get a list menu options</para>
    /// <para type="description">Returns a CliMenuOptions object with all menu options. This CmdLet has no parameters</para>
    /// <example>
    ///   <title>Default usage.</title>
    ///   <description>Returns default menu-items for all menus.</description>
    ///   <code>$opts = Get-CliMenuOption</code>
    /// </example>
    /// </summary>
    [Cmdlet(VerbsCommon.Get, "CliMenuOption")]
    [OutputType(typeof(CliMenuOptions))]
    public class GetCliMenuOptionCmdLet : PSCmdlet
    {
        protected override void ProcessRecord()
        {
            CliMenuOptions opts = GetVariableValue("CliMenuOptions", new CliMenuOptions(this.Host.UI.RawUI.WindowSize.Width)) as CliMenuOptions;
            WriteObject(opts);
            base.ProcessRecord();
        }
    }
}
