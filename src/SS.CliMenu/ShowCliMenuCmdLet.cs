using SS.CliMenu.Models;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Internal;
using System.Management.Automation.Runspaces;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SS.CliMenu
{
    /// <summary>
    /// Show the menu that is passed into the cmdlet.
    /// <para type="synopsis">Show a Menu.</para>
    /// <para type="description">Show the menu that is passed into the cmdlet.</para>
    /// <example>
    ///   <title>Default usage.</title>
    ///   <description></description>
    ///   <code>$menu | Show-Menu</code>
    /// </example>
    /// </summary>
    [Cmdlet(VerbsCommon.Show, "CliMenu", SupportsShouldProcess = true, ConfirmImpact = ConfirmImpact.High)]
    public class ShowCliMenuCmdLet : UICmdLet
    {
        /// <summary>
        /// The index of the menu item to invoke.
        /// <para type="description">The index of the menu item to invoke.</para>
        /// </summary>
        [Parameter]
        public int InvokeItem { get; set; }
        /// <summary>
        /// 
        /// <para type="description"></para>
        /// </summary>
		[Parameter(Mandatory = true, ValueFromPipeline = true)]
        public MenuObject Menu { get; set; }
        /// <summary>
        /// The default option for the menu.
        /// <para type="description">The default option for the menu.</para>
        /// </summary>
        [Parameter]
        public string DefaultOption { get; set; } = "Q";
        /// <summary>
        /// 
        /// <para type="description"></para>
        /// </summary>
        [Parameter]
        [Alias("Header")]
        public ScriptBlock HeaderScript { get; set; }
        /// <summary>
        /// 
        /// <para type="description"></para>
        /// </summary>
        [Parameter]
        public Action<MenuObject, CliMenuOptions> HeaderFunc { get; set; }

        protected override void BeginProcessing()
        {
            WriteVerbose($"{MyInvocation.InvocationName} - START");
            base.BeginProcessing();
        }

        protected override void ProcessRecord()
        {
            try
            {
                MenuItemObject menuSelected = null;
                #region InvokeItem
                if (MyInvocation.BoundParameters.ContainsKey("InvokeItem"))
                {
                    menuSelected = Menu.MenuItems[this.InvokeItem];

                    if (menuSelected != null)
                    {
                        WriteVerbose($"Menu: {menuSelected.Name}");
                        if (menuSelected.DisableConfirm.HasValue && !menuSelected.DisableConfirm.Value && !this.ShouldProcess(menuSelected.ConfirmTargetData, $"{menuSelected.Name}"))
                        {
                            WriteWarning($"Execution aborted for [{menuSelected.Name}]");
                        }
                        else if (menuSelected.Script != null)
                        {
                            Host.UI.WriteLine(ConsoleColor.White, ConsoleColor.DarkBlue, $"Invoking [{menuSelected.Name}]");
                            Host.UI.Write($"Args: ");
                            if (menuSelected.ScriptArgs != null)
                            {
                                foreach (var arg in menuSelected.ScriptArgs)
                                {
                                    Host.UI.Write($"{arg} ");
                                }
                            }
                            Host.UI.WriteLine();
                            try
                            {
                                WriteVerbose("Calling Script");
                                var r_objs = menuSelected.Script.InvokeWithContext(null, null, menuSelected.ScriptArgs);
                                foreach (var obj in r_objs)
                                    WriteObject(obj.BaseObject);
                                WriteVerbose("Called Script");
                            }
                            catch (RuntimeException ex)
                            {
                                WriteError(ex.ErrorRecord);
                            }
                            Host.UI.WriteLine(ConsoleColor.White, ConsoleColor.DarkBlue, $"Invoke [{menuSelected.Name}] Completed!");
                        }
                        else if (menuSelected.Func != null)
                        {
                            Host.UI.WriteLine(ConsoleColor.White, ConsoleColor.DarkBlue, $"Invoking [{menuSelected.Name}]");
                            Host.UI.Write($"Args: ");
                            if (menuSelected.FuncArgs != null)
                            {
                                foreach (var arg in menuSelected.FuncArgs)
                                {
                                    Host.UI.Write($"{arg} ");
                                }
                            }
                            Host.UI.WriteLine();
                            try
                            {
                                WriteVerbose("Calling Func");
                                var r_objs = menuSelected.Func(menuSelected, menuSelected.FuncArgs ?? new object[0]);
                                if (r_objs != null)
                                    foreach (var obj in r_objs)
                                        WriteObject(obj);
                                WriteVerbose("Called Func");
                            }
                            catch (RuntimeException ex)
                            {
                                WriteError(ex.ErrorRecord);
                            }
                            Host.UI.WriteLine(ConsoleColor.White, ConsoleColor.DarkBlue, $"Invoke [{menuSelected.Name}] Completed!");
                        }
                        else
                        {
                            WriteWarning($"No Script or Func to perform for [{menuSelected.Name}]");
                        }
                    }
                }
                #endregion
                menuSelected = null;
                #region Show Menu
                Regex rxOption = new Regex("^\\d+$");
                var menuLines = new ArrayList();
                opts = GetVariableValue("CliMenuOptions", new CliMenuOptions(this.Host.UI.RawUI.WindowSize.Width)) as CliMenuOptions;

                if (Menu == null)
                {
                    WriteError(new ErrorRecord(new ArgumentNullException("Menu", "Menu parameter must not be null."), "MenuNotGiven", ErrorCategory.InvalidArgument, null));
                    return;
                }

                var menuFrame = new string(opts.MenuFillChar, (opts.MaxWidth - 2));
                var menuEmptyLine = new string(' ', (opts.MaxWidth - 2));
                // Top Header border
                WriteMenuLine(menuFrame, opts.MenuFillColor);
                // Menu Display Name
                WriteMenuLine(Menu.DisplayName, opts.MenuNameColor);
                if (!string.IsNullOrWhiteSpace(Menu.Description))
                    WriteMenuLine(Menu.Description, opts.MenuNameColor);

                WriteMenuLine(menuEmptyLine, opts.MenuFillColor);

                if (HeaderScript != null)
                {
                    try
                    {
                        HeaderScript.InvokeWithContext(null, null);
                    }
                    catch (RuntimeException ex)
                    {
                        WriteError(ex.ErrorRecord);
                    }
                }
                else if (opts.HeaderScript != null)
                {
                    try
                    {
                        opts.HeaderScript.InvokeWithContext(null, null);
                    }
                    catch (RuntimeException ex)
                    {
                        WriteError(ex.ErrorRecord);
                    }
                }
                else if (HeaderFunc != null)
                {
                    try
                    {
                        HeaderFunc(this.Menu, opts);
                    }
                    catch (RuntimeException ex)
                    {
                        WriteError(ex.ErrorRecord);
                    }
                }
                else if (opts.HeaderFunc != null)
                {
                    try
                    {
                        opts.HeaderFunc(this.Menu, opts);
                    }
                    catch (RuntimeException ex)
                    {
                        WriteError(ex.ErrorRecord);
                    }
                }
                else
                {
                    WriteMenuLine(opts.Heading, opts.HeadingColor);
                    WriteMenuLine(menuEmptyLine, opts.MenuFillColor);
                    WriteMenuLine(opts.SubHeading, opts.SubHeadingColor);
                }
                WriteMenuLine(menuEmptyLine, opts.MenuFillColor);

                // Bottom Header border
                WriteMenuLine(menuFrame, opts.MenuFillColor);
                WriteMenuLine(menuEmptyLine, opts.MenuFillColor);

                var idxOptions = this.Menu.MenuItems.Where(i => rxOption.IsMatch($"{i.Option}")).Select(i => int.Parse(i.Option)).ToArray();
                int maxIdxOptions = (idxOptions.Length == 0) ? 0 : idxOptions.OrderBy(i => i).Max();

                bool visible;
                foreach (var item in Menu.MenuItems)
                {
                    ConsoleColor menuColor;
                    if (!string.IsNullOrWhiteSpace(item.Option) && !(item.Func == null && item.Script == null))
                        menuColor = opts.MenuItemColor;
                    else
                        menuColor = opts.ViewOnlyColor;

                    visible = true;
                    if (item.VisibleScript != null)
                    {
                        Collection<PSObject> aOut = null;
                        try
                        {
                            aOut = item.VisibleScript.InvokeWithContext(null, null, item.VisibleScriptArgs);
                        }
                        catch (RuntimeException ex)
                        {
                            WriteError(ex.ErrorRecord);
                        }
                        if (aOut != null && aOut.Count > 0 && aOut[0].BaseObject is bool)
                            visible = (bool)aOut[0].BaseObject;
                        else
                            WriteWarning($"Object returned from VisibleScript script is not a boolean value for menu item [{item.Name}], returned [{aOut}]");
                    }
                    else if (item.VisibleFunc != null)
                    {
                        try
                        {
                            visible = item.VisibleFunc(item, item.VisibleFuncArgs);
                        }
                        catch (RuntimeException ex)
                        {
                            WriteError(ex.ErrorRecord);
                        }
                    }

                    if (visible)
                    {
                        if (item.ForegroundColor != null)
                            menuColor = item.ForegroundColor.Value;

                        if (item.IsSpace.HasValue && item.IsSpace.Value)
                            WriteMenuLine(" ", menuColor, true);
                        else if (!string.IsNullOrWhiteSpace(item.Option))
                        {
                            if (DefaultOption == "Q" && (item.Default ?? false)) // We only use the first option that is set as default.
                                DefaultOption = item.Option;
                            var Option = item.Option;
                            if (maxIdxOptions >= 10 && rxOption.IsMatch(item.Option) && int.Parse(item.Option) < 10)
                                Option = $" {Option}";
                            WriteMenuLine($"{Option}. {item.DisplayName}", menuColor, true);
                        }
                        else
                            WriteMenuLine($"{item.DisplayName}", menuColor, true);
                    }
                }

                WriteMenuLine(menuEmptyLine, opts.MenuFillColor);
                WriteMenuLine("Q Exit Menu", ConsoleColor.Red, true);

                WriteMenuLine(menuEmptyLine, opts.MenuFillColor);
                WriteMenuLine(menuEmptyLine, opts.MenuFillColor);

                WriteMenuLine(opts.FooterText, opts.FooterTextColor);
                WriteMenuLine(menuEmptyLine, opts.MenuFillColor);

                // Bottom border
                WriteMenuLine(menuFrame, opts.MenuFillColor);

                Host.UI.Write($"Please choose a option [{DefaultOption}] ");
                var userSelection = Host.UI.ReadLine();
                WriteVerbose($"Selection: {userSelection}");

                if (string.IsNullOrWhiteSpace(userSelection))
                {
                    userSelection = DefaultOption;
                    WriteVerbose($"Using default option: {userSelection}");
                }
                else
                    WriteVerbose($"Selection: {userSelection}");

                if (userSelection.Equals("Q", StringComparison.CurrentCultureIgnoreCase))
                {
                    WriteVerbose("Exiting Menu");
                    WriteObject("quit");
                    return;
                }

                menuSelected = Menu.MenuItems.Where(i => i.Option != null && i.Option.Equals(userSelection, StringComparison.CurrentCultureIgnoreCase)).SingleOrDefault();
                visible = true;
                if (menuSelected != null && menuSelected.VisibleScript != null)
                {
                    Collection<PSObject> aOut = null;
                    try
                    {
                        aOut = menuSelected.VisibleScript.InvokeWithContext(null, null, menuSelected.VisibleScriptArgs);
                    }
                    catch (RuntimeException ex)
                    {
                        WriteError(ex.ErrorRecord);
                    }
                    if (aOut != null && aOut.Count > 0 && aOut[0].BaseObject is bool)
                        visible = (bool)aOut[0].BaseObject;
                    else
                        WriteWarning($"Object returned from VisibleScript script is not a boolean value for menu item [{menuSelected.Name}], returned [{aOut}]");
                }
                else if (menuSelected != null && menuSelected.VisibleFunc != null)
                {
                    try
                    {
                        visible = menuSelected.VisibleFunc(menuSelected, menuSelected.VisibleFuncArgs);
                    }
                    catch (RuntimeException ex)
                    {
                        WriteError(ex.ErrorRecord);
                    }
                }
                if (!visible)
                {
                    menuSelected = null;
                }

                if (menuSelected != null)
                {
                    WriteVerbose($"Menu: {menuSelected.Name}");
                    if (menuSelected.DisableConfirm.HasValue && !menuSelected.DisableConfirm.Value && !this.ShouldProcess(menuSelected.ConfirmTargetData, $"{menuSelected.Name}"))
                    {
                        WriteWarning("Execution aborted");
                    }
                    else if (menuSelected.Script != null)
                    {
                        Host.UI.WriteLine(ConsoleColor.White, ConsoleColor.DarkBlue, $"Invoking [{menuSelected.Name}]");
                        Host.UI.Write($"Args: ");
                        if (menuSelected.ScriptArgs != null)
                        {
                            foreach (var arg in menuSelected.ScriptArgs)
                            {
                                Host.UI.Write($"{arg} ");
                            }
                        }
                        Host.UI.WriteLine();
                        try
                        {
                            WriteVerbose("Calling Script");
                            var r_objs = menuSelected.Script.InvokeWithContext(null, null, menuSelected.ScriptArgs);
                            foreach (var obj in r_objs)
                                WriteObject(obj.BaseObject);
                            WriteVerbose("Called Script");
                        }
                        catch (RuntimeException ex)
                        {
                            WriteError(ex.ErrorRecord);
                        }
                        Host.UI.WriteLine(ConsoleColor.White, ConsoleColor.DarkBlue, $"Invoke [{menuSelected.Name}] Completed!");
                    }
                    else if (menuSelected.Func != null)
                    {
                        Host.UI.WriteLine(ConsoleColor.White, ConsoleColor.DarkBlue, $"Invoking [{menuSelected.Name}]");
                        Host.UI.Write($"Args: ");
                        if (menuSelected.FuncArgs != null)
                        {
                            foreach (var arg in menuSelected.FuncArgs)
                            {
                                Host.UI.Write($"{arg} ");
                            }
                        }
                        Host.UI.WriteLine();
                        try
                        {
                            WriteVerbose("Calling Func");
                            var r_objs = menuSelected.Func(menuSelected, menuSelected.FuncArgs ?? new object[0]);
                            if (r_objs != null)
                                foreach (var obj in r_objs)
                                    WriteObject(obj);
                            WriteVerbose("Called Func");
                        }
                        catch (RuntimeException ex)
                        {
                            WriteError(ex.ErrorRecord);
                        }
                        Host.UI.WriteLine(ConsoleColor.White, ConsoleColor.DarkBlue, $"Invoke [{menuSelected.Name}] Completed!");
                    }
                    else
                    {
                        WriteWarning($"No Script or Func to perform for [{menuSelected.Name}]");
                    }
                }
                else
                {
                    Host.UI.WriteLine(ConsoleColor.Red, Host.UI.RawUI.BackgroundColor, $"Menuitem not found [{userSelection}]");
                    Host.UI.Write("Press enter to continue...");
                    Host.UI.ReadLine();
                }
                #endregion
            }
            catch (Exception ex)
            {
                WriteError(new ErrorRecord(ex, "5000", ErrorCategory.NotSpecified, this));
            }
            finally
            {
                base.ProcessRecord();
            }
        }

        protected override void EndProcessing()
        {
            WriteVerbose($"{MyInvocation.InvocationName} - END");
            base.EndProcessing();
        }

        MenuLine NewMenuLine(string text, ConsoleColor color = ConsoleColor.White, bool isMenuItem = false)
        {
            string textLine;

            if (isMenuItem)
            {
                textLine = $"  {text}";
                textLine += new string(' ', ((opts.MaxWidth - 1) - textLine.Length - 1));
            }
            else
            {
                var textLength = text.Length;
                var textBlanks = ((opts.MaxWidth - 2) - textLength) / 2;
                textLine = new string(' ', textBlanks) + text;
                textLine += new string(' ', ((opts.MaxWidth - 1) - textLine.Length - 1));
            }

            return new MenuLine
            {
                Text = textLine,
                Color = color
            };
        }

        struct MenuLine
        {
            public string Text { get; internal set; }
            public object Color { get; internal set; }
        }
    }
}
