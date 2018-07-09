using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SS.CliMenu
{
    /// <summary>
    /// Create a new Menu-Item for a Menu.
    /// <para type="synopsis">Create a new Menu-Item for a Menu.</para>
    /// <para type="description">Menu-Items are the action elements of the Menu. You add Menu-Items to a Menu.</para>
    /// <example>
    ///   <title>Default usage.</title>
    ///   <description>This will create a new Menu-Item for the menu named passed in. The Menu-object is piped into the New-CliMenuItem cmdlet.</description>
    ///   <code>$menu | New-MenuItem -Name "UnlockUser" -DisplayName "Unlock a user" -Action { Unlock-UserObject } -DisableConfirm $true</code>
    /// </example>
    /// </summary>
    [Cmdlet(VerbsCommon.New, "CliMenuItem")]
    [OutputType(typeof(MenuObject))]
    public class NewCliMenuItemCmdLet : PSCmdlet
    {
        /// <summary>
        /// 
        /// <para type="description"></para>
        /// </summary>
        [Parameter(ValueFromPipeline = true, Mandatory = true)]
        public MenuObject MenuObject { get; set; }
        /// <summary>
        /// 
        /// <para type="description"></para>
        /// </summary>
        [Parameter()]
        public string Name { get; set; }
        /// <summary>
        /// 
        /// <para type="description"></para>
        /// </summary>
        [Parameter(Mandatory = true, ParameterSetName ="MenuItem")]
        public string DisplayName { get; set; }
        /// <summary>
        /// 
        /// <para type="description"></para>
        /// </summary>
        [Parameter()]
        public string Description { get; set; }
        /// <summary>
        /// 
        /// <para type="description"></para>
        /// </summary>
        [Parameter(ParameterSetName = "MenuItem")]
        public ScriptBlock Action { get; set; }
        /// <summary>
        /// 
        /// <para type="description"></para>
        /// </summary>
        [Parameter(ParameterSetName = "MenuItem")]
        public object[] ActionArgs { get; set; }
        /// <summary>
        /// 
        /// <para type="description"></para>
        /// </summary>
        [Parameter()]
        public ScriptBlock VisibleAction { get; set; }
        /// <summary>
        /// 
        /// <para type="description"></para>
        /// </summary>
        [Parameter()]
        public object[] VisibleActionArgs { get; set; }
        /// <summary>
        /// 
        /// <para type="description"></para>
        /// </summary>
        [Parameter(ParameterSetName = "MenuItem")]
        public SwitchParameter DisableConfirm { get; set; }
        /// <summary>
        /// 
        /// <para type="description"></para>
        /// </summary>
        [Parameter()]
        public ConsoleColor? ForegroundColor { get; set; }
        /// <summary>
        /// 
        /// <para type="description"></para>
        /// </summary>
        [Parameter(ParameterSetName = "MenuItem")]
        public string OptionKey { get; set; }
        /// <summary>
        /// 
        /// <para type="description"></para>
        /// </summary>
        [Parameter(ParameterSetName = "MenuItemSpace")]
        public SwitchParameter IsSpace { get; set; }
        /// <summary>
        /// 
        /// <para type="description"></para>
        /// </summary>
        [Parameter(ParameterSetName = "MenuItem")]
        public string ConfirmTargetData { get; set; }


        protected override void BeginProcessing()
        {
            WriteVerbose($"{MyInvocation.InvocationName} - START");
            base.BeginProcessing();
        }

        protected override void ProcessRecord()
        {
            Regex rxOption = new Regex("^\\d+$");
            if (string.IsNullOrWhiteSpace(OptionKey) && !IsSpace && Action != null)
            {
                //if they did not assign a option, then auto-assign one via next index #
                var idxOptions = this.MenuObject.MenuItems.Where(i => i.Option != null && rxOption.IsMatch(i.Option)).Select(i => int.Parse(i.Option)).ToArray();
                int max = idxOptions.Length == 0 ? 0 : idxOptions.OrderBy(i => i).Max();
                OptionKey = (max + 1).ToString();
            }
            else if (!IsSpace && Action != null)
            {
                var existing = this.MenuObject.MenuItems.Where(i => i.Option == OptionKey).FirstOrDefault();
                if (existing != null)
                {
                    throw new Exception($"Menu item already exists with option '{OptionKey}");
                }
            }
            else if (new string[] { "q", "Q" }.Contains(OptionKey))
            {
                throw new Exception($"Menu item already exists with option '{OptionKey}");
            }


            MenuItemObject menuItem = new MenuItemObject
            {
                Name = this.Name,
                DisplayName = this.DisplayName,
                Description = this.Description,
                Action = this.Action,
                ActionArgs = this.ActionArgs,
                ForegroundColor = this.ForegroundColor,
                ConfirmBeforeInvoke = !this.DisableConfirm,
                ConfirmTargetData = this.ConfirmTargetData,
                IsSpace = this.IsSpace,
                Option = this.OptionKey,
                VisibleAction = this.VisibleAction,
                VisibleActionArgs = this.VisibleActionArgs
            };

            this.MenuObject.MenuItems.Add(menuItem);

            WriteObject(this.MenuObject);

            base.ProcessRecord();
        }

        protected override void EndProcessing()
        {
            WriteVerbose($"{MyInvocation.InvocationName} - END");
            base.EndProcessing();
        }
    }
}
