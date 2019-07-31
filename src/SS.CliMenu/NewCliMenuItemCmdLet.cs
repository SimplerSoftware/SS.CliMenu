using SS.CliMenu.Models;
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
    ///   <code>$menu | New-MenuItem -Name "UnlockUser" -DisplayName "Unlock a user" -Script { Unlock-UserObject } -DisableConfirm $true</code>
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
        /// The name of this menu item, for debug/logging purposes.
        /// <para type="description">The name of this menu item, for debug/logging purposes.</para>
        /// </summary>
        [Parameter()]
        public string Name { get; set; }
        /// <summary>
        /// The displayed value for this menu item.
        /// <para type="description">The displayed value for this menu item.</para>
        /// </summary>
        [Parameter(Mandatory = true, ParameterSetName ="MenuItem")]
        public string DisplayName { get; set; }
        /// <summary>
        /// Currently not used, possibly used for help menu at some point.
        /// <para type="description">Currently not used, possibly used for help menu at some point.</para>
        /// </summary>
        [Parameter()]
        public string Description { get; set; }
        /// <summary>
        /// Script to run when menu is selected.
        /// <para type="description">Script to run when menu is selected.</para>
        /// </summary>
        [Parameter(ParameterSetName = "MenuItem")]
        [Alias("Action")]
        public ScriptBlock Script { get; set; }
        /// <summary>
        /// Arguments to pass to Script when ran.
        /// <para type="description">Arguments to pass to Script when ran.</para>
        /// </summary>
        [Parameter(ParameterSetName = "MenuItem")]
        [Alias("ActionArgs")]
        public object[] ScriptArgs { get; set; }
        /// <summary>
        /// .Net Func to run when menu is selected. This is to support calling and building native .Net lambda methods from within a Cmdlet.
        /// <para type="description">.Net Func to run when menu is selected. This is to support calling and building native .Net lambda methods from within a Cmdlet.</para>
        /// </summary>
        [Parameter(ParameterSetName = "MenuItem")]
        public Func<MenuItemObject, object[], object[]> Func { get; set; }
        /// <summary>
        /// Arguments to pass to Func when ran.
        /// <para type="description">Arguments to pass to Func when ran.</para>
        /// </summary>
        [Parameter(ParameterSetName = "MenuItem")]
        public object[] FuncArgs { get; set; }
        /// <summary>
        /// Script to run to evaluate if the menu item is visible or not.
        /// <para type="description">Script to run to evaluate if the menu item is visible or not.</para>
        /// </summary>
        [Parameter()]
        [Alias("VisibleAction")]
        public ScriptBlock VisibleScript { get; set; }
        /// <summary>
        /// Arguments to pass to visible script when ran.
        /// <para type="description">Arguments to pass to visible script when ran.</para>
        /// </summary>
        [Parameter()]
        [Alias("VisibleActionArgs")]
        public object[] VisibleScriptArgs { get; set; }
        /// <summary>
        /// .Net Func to run to evaluate if the menu item is visible or not. This is to support calling and building native .Net lambda methods from within a Cmdlet.
        /// <para type="description">.Net Func to run to evaluate if the menu item is visible or not. This is to support calling and building native .Net lambda methods from within a Cmdlet.</para>
        /// </summary>
        [Parameter()]
        public Func<MenuItemObject, object[], bool> VisibleFunc { get; set; }
        /// <summary>
        /// Arguments to pass to visible Func when ran.
        /// <para type="description">Arguments to pass to visible Func when ran.</para>
        /// </summary>
        [Parameter()]
        public object[] VisibleFuncArgs { get; set; }
        /// <summary>
        /// When true the menu will run without asking confirmation.
        /// <para type="description">When true the menu will run without asking confirmation.</para>
        /// </summary>
        [Parameter(ParameterSetName = "MenuItem")]
        public SwitchParameter DisableConfirm { get; set; }
        /// <summary>
        /// Text color of menu item.
        /// <para type="description">Text color of menu item.</para>
        /// </summary>
        [Parameter()]
        public ConsoleColor? ForegroundColor { get; set; }
        /// <summary>
        /// The key(s) to use to select this menu item.
        /// <para type="description">The key(s) to use to select this menu item.</para>
        /// </summary>
        [Parameter(ParameterSetName = "MenuItem",  ValueFromPipelineByPropertyName = true)]
        [Alias("Option")]
        public string OptionKey { get; set; }
        /// <summary>
        /// Is this menu item a blank line space.
        /// <para type="description">Is this menu item a blank line space.</para>
        /// </summary>
        [Parameter(ParameterSetName = "MenuItemSpace")]
        public SwitchParameter IsSpace { get; set; }
        /// <summary>
        /// The target value passed to ShouldProcess.
        /// <para type="description">The target value passed to ShouldProcess.</para>
        /// </summary>
        [Parameter(ParameterSetName = "MenuItem")]
        public string ConfirmTargetData { get; set; }
        /// <summary>
        /// Is this menu item the default option.
        /// <para type="description">Is this menu item the default option.</para>
        /// </summary>
        [Parameter(ParameterSetName = "MenuItem")]
        public SwitchParameter Default { get; set; }
        /// <summary>
        /// The zero-based index at which the menu item should be inserted.
        /// <para type="description">The zero-based index at which the menu item should be inserted.</para>
        /// </summary>
        [Parameter()]
        public int? InsertAt { get; set; } = null;

        /// <summary>
        /// 
        /// </summary>
        protected override void BeginProcessing()
        {
            WriteVerbose($"{MyInvocation.InvocationName} - START");
            base.BeginProcessing();
        }
        /// <summary>
        /// 
        /// </summary>
        protected override void ProcessRecord()
        {
            Regex rxOption = new Regex("^\\d+$");
            if (string.IsNullOrWhiteSpace(OptionKey) && !IsSpace && (Script != null || Func != null))
            {
                //if they did not assign a option, then auto-assign one via next index #
                var idxOptions = this.MenuObject.MenuItems.Where(i => i.Option != null && rxOption.IsMatch(i.Option)).Select(i => int.Parse(i.Option)).ToArray();
                int max = idxOptions.Length == 0 ? 0 : idxOptions.OrderBy(i => i).Max();
                OptionKey = (max + 1).ToString();
            }
            else if (!IsSpace && (Script != null || Func != null))
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
                Script = this.Script,
                ScriptArgs = this.ScriptArgs,
                Func = this.Func,
                FuncArgs = this.FuncArgs,
                ForegroundColor = this.ForegroundColor,
                DisableConfirm = this.DisableConfirm,
                ConfirmTargetData = this.ConfirmTargetData,
                IsSpace = this.IsSpace,
                Option = this.OptionKey,
                VisibleScript = this.VisibleScript,
                VisibleScriptArgs = this.VisibleScriptArgs,
                VisibleFunc = this.VisibleFunc,
                VisibleFuncArgs = this.VisibleFuncArgs,
                Default = this.Default
            };

            if (InsertAt.HasValue)
                this.MenuObject.MenuItems.Insert(InsertAt.Value, menuItem);
            else
                this.MenuObject.MenuItems.Add(menuItem);

            WriteObject(this.MenuObject);

            base.ProcessRecord();
        }
        /// <summary>
        /// 
        /// </summary>
        protected override void EndProcessing()
        {
            WriteVerbose($"{MyInvocation.InvocationName} - END");
            base.EndProcessing();
        }
    }
}
