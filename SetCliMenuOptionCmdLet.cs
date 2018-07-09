using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Security.Principal;
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
    ///   <code>Set-CliMenuOption -Heading 'PowerShell Menu'</code>
    /// </example>
    /// </summary>
    [Cmdlet(VerbsCommon.Set, "CliMenuOption")]
    [OutputType(typeof(CliMenuOptions))]
    public class SetCliMenuOptionCmdLet : PSCmdlet
    {
        /// <summary>
        /// 
        /// <para type="description"></para>
        /// </summary>
        [Parameter]
        public char MenuFillChar { get; set; } = '*';
        /// <summary>
        /// 
        /// <para type="description"></para>
        /// </summary>
        [Parameter]
        public ConsoleColor MenuFillColor { get; set; }
        /// <summary>
        /// 
        /// <para type="description"></para>
        /// </summary>
        [Parameter]
        public string Heading { get; set; }
        /// <summary>
        /// 
        /// <para type="description"></para>
        /// </summary>
        [Parameter]
        public ConsoleColor HeadingColor { get; set; }
        /// <summary>
        /// 
        /// <para type="description"></para>
        /// </summary>
        [Parameter]
        public string SubHeading { get; set; }
        /// <summary>
        /// 
        /// <para type="description"></para>
        /// </summary>
        [Parameter]
        public ConsoleColor SubHeadingColor { get; set; }
        /// <summary>
        /// 
        /// <para type="description"></para>
        /// </summary>
        [Parameter]
        public string FooterText { get; set; }
        /// <summary>
        /// 
        /// <para type="description"></para>
        /// </summary>
        [Parameter]
        public ConsoleColor FooterTextColor { get; set; }
        /// <summary>
        /// 
        /// <para type="description"></para>
        /// </summary>
        [Parameter]
        public ConsoleColor MenuItemColor { get; set; }
        /// <summary>
        /// 
        /// <para type="description"></para>
        /// </summary>
        [Parameter]
        public ConsoleColor ViewOnlyColor { get; set; }
        /// <summary>
        /// 
        /// <para type="description"></para>
        /// </summary>
        [Parameter]
        public ConsoleColor MenuNameColor { get; set; }
        /// <summary>
        /// 
        /// <para type="description"></para>
        /// </summary>
        [Parameter]
        public int MaxWidth { get; set; }
        /// <summary>
        /// 
        /// <para type="description"></para>
        /// </summary>
        [Parameter]
        public ScriptBlock HeaderAction { get; set; }

        protected override void ProcessRecord()
        {

            CliMenuOptions opts = GetVariableValue("CliMenuOptions", new CliMenuOptions(this.Host)) as CliMenuOptions;
            
            foreach (var key in MyInvocation.BoundParameters.Keys)
            {
                //WriteVerbose -Message "$f -  Setting [$key] to value $($PSBoundParameters.$key)"
                switch (key.ToLower()) {
                    case "menufillchar":
                        opts.MenuFillChar = this.MenuFillChar;
                        break;
                    case "menufillcolor":
                        opts.MenuFillColor = this.MenuFillColor;
                        break;
                    case "heading":
                        opts.Heading = this.Heading;
                        break;
                    case "headingcolor":
                        opts.HeadingColor = this.HeadingColor;
                        break;
                    case "subheading":
                        opts.SubHeading = this.SubHeading;
                        break;
                    case "subheadingcolor":
                        opts.SubHeadingColor = this.SubHeadingColor;
                        break;
                    case "footertext":
                        opts.FooterText = this.FooterText;
                        break;
                    case "footertextcolor":
                        opts.FooterTextColor = this.FooterTextColor;
                        break;
                    case "menuitemcolor":
                        opts.MenuItemColor = this.MenuItemColor;
                        break;
                    case "viewonlycolor":
                        opts.ViewOnlyColor = this.ViewOnlyColor;
                        break;
                    case "menunamecolor":
                        opts.MenuNameColor = this.MenuNameColor;
                        break;
                    case "maxwidth":
                        opts.MaxWidth = this.MaxWidth;
                        break;
                    case "headeraction":
                        opts.HeaderAction = this.HeaderAction;
                        break;
                    default:
                        throw new ParameterBindingException($"Unknown parameter for options '{key}'");
                }
            }

            if (string.IsNullOrWhiteSpace(opts.FooterText))
                opts.FooterText = $"{DateTime.Now} - Running as {WindowsIdentity.GetCurrent().Name}";

            this.SessionState.PSVariable.Set("CliMenuOptions", opts);
            WriteObject(opts);

            base.ProcessRecord();
        }
    }
}
