using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Text;
using System.Threading.Tasks;

namespace SS.CliMenu.Models
{
    public class MenuItemObject
    {
        /// <summary>
        /// The key(s) to use to select this menu item.
        /// </summary>
        public string Option { get; set; }
        /// <summary>
        /// The name of this menu item, for debug/logging purposes.
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// The displayed value for this menu item.
        /// </summary>
        public string DisplayName { get; set; }
        /// <summary>
        /// Currently not used, possibly used for help menu at some point.
        /// </summary>
        public string Description { get; set; }
        /// <summary>
        /// Script to run when menu is selected.
        /// </summary>
        public ScriptBlock Script { get; set; }
        /// <summary>
        /// Arguments to pass to Script when ran.
        /// </summary>
        public object[] ScriptArgs { get; set; }
        /// <summary>
        /// Text color of menu item.
        /// </summary>
        public ConsoleColor? ForegroundColor { get; set; }
        /// <summary>
        /// When true the menu will run without asking confirmation.
        /// </summary>
        public bool? DisableConfirm { get; set; }
        /// <summary>
        /// The target value passed to ShouldProcess.
        /// </summary>
        public string ConfirmTargetData { get; set; }
        /// <summary>
        /// Is this menu item a blank line space.
        /// </summary>
        public bool? IsSpace { get; set; }
        /// <summary>
        /// Script to run to evaluate if the menu item is visible or not.
        /// </summary>
        public ScriptBlock VisibleScript { get; set; }
        /// <summary>
        /// Arguments to pass to visible script when ran.
        /// </summary>
        public object[] VisibleScriptArgs { get; set; }
        /// <summary>
        /// .Net Func to run when menu is selected. This is to support calling and building native .Net lambda methods from within a Cmdlet.
        /// </summary>
        /// <remarks>Script has precedence over Func</remarks>
        public Func<MenuItemObject, object[], object[]> Func { get; set; }
        /// <summary>
        /// Arguments to pass to Func when ran.
        /// </summary>
        public object[] FuncArgs { get; set; }
        /// <summary>
        /// .Net Func to run to evaluate if the menu item is visible or not. This is to support calling and building native .Net lambda methods from within a Cmdlet.
        /// </summary>
        /// <remarks>VisibleScript has precedence over VisibleFunc</remarks>
        public Func<MenuItemObject, object[], bool> VisibleFunc { get; set; }
        /// <summary>
        /// Arguments to pass to visible Func when ran.
        /// </summary>
        public object[] VisibleFuncArgs { get; set; }
        /// <summary>
        /// Is this menu item the default option.
        /// </summary>
        public bool? Default { get; set; }
    }
}
