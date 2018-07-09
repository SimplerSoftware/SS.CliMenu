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
        /// 
        /// <para type="description"></para>
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
        /// 
        /// <para type="description"></para>
        /// </summary>
        [Parameter]
        public ScriptBlock Header { get; set; }

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
                        if (menuSelected.ConfirmBeforeInvoke && !this.ShouldProcess(menuSelected.ConfirmTargetData, $"{menuSelected.Name}"))
                        {
                            WriteWarning($"Execution aborted for [{menuSelected.Name}]");
                        }
                        else if (menuSelected.Action != null)
                        {
                            Host.UI.WriteLine(ConsoleColor.White, ConsoleColor.Blue, $"Invoking [{menuSelected.Name}]");
                            Host.UI.WriteLine($"Args: {menuSelected.ActionArgs}");
                            try
                            {
                                var r_objs = menuSelected.Action.InvokeWithContext(null, null, menuSelected.ActionArgs);
                                foreach (var obj in r_objs)
                                    WriteObject(obj.BaseObject);
                            }
                            catch (RuntimeException ex)
                            {
                                WriteError(ex.ErrorRecord);
                            }
                            Host.UI.WriteLine(ConsoleColor.White, ConsoleColor.Blue, $"Invoke [{menuSelected.Name}] Completed!");
                        }
                        else
                        {
                            WriteWarning($"No Action to perform for [{menuSelected.Name}]");
                        }
                    }
                }
                #endregion
                menuSelected = null;
                #region Show Menu
                Regex rxOption = new Regex("^\\d+$");
                var menuLines = new ArrayList();
                opts = GetVariableValue("CliMenuOptions", new CliMenuOptions(this.Host)) as CliMenuOptions;

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

                if (Header != null)
                {
                    try
                    {
                        Header.InvokeWithContext(null, null);
                    }
                    catch (RuntimeException ex)
                    {
                        WriteError(ex.ErrorRecord);
                    }
                }
                else if (opts.HeaderAction != null)
                {
                    try
                    {
                        opts.HeaderAction.InvokeWithContext(null, null);
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
                    if (!string.IsNullOrWhiteSpace(item.Option))
                        menuColor = opts.MenuItemColor;
                    else
                        menuColor = opts.ViewOnlyColor;

                    visible = true;
                    if (item.VisibleAction != null)
                    {
                        Collection<PSObject> aOut = null;
                        try
                        {
                            aOut = item.VisibleAction.InvokeWithContext(null, null, item.VisibleActionArgs);
                        }
                        catch (RuntimeException ex)
                        {
                            WriteError(ex.ErrorRecord);
                        }
                        if (aOut != null && aOut.Count > 0 && aOut[0].BaseObject is bool)
                            visible = (bool)aOut[0].BaseObject;
                        else
                            WriteWarning($"Object returned from VisibleAction script is not a boolean value for menu item [{item.Name}], returned [{aOut}]");
                    }
                    if (visible)
                    {
                        if (item.ForegroundColor != null)
                            menuColor = item.ForegroundColor.Value;

                        if (item.IsSpace)
                            WriteMenuLine(" ", menuColor, true);
                        else if (!string.IsNullOrWhiteSpace(item.Option))
                        {
                            var Option = item.Option;
                            if (maxIdxOptions >= 10 && rxOption.IsMatch(item.Option) && int.Parse(item.Option) < 10)
                                Option = " " + Option;
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

                Host.UI.Write("Please choose a option [Q] ");
                var userSelection = Host.UI.ReadLine();
                WriteVerbose($"Selection: {userSelection}");

                if (string.IsNullOrWhiteSpace(userSelection) || userSelection.Equals("Q", StringComparison.CurrentCultureIgnoreCase))
                {
                    WriteVerbose("Exiting Menu");
                    WriteObject("quit");
                    return;
                }

                menuSelected = Menu.MenuItems.Where(i => i.Option != null && i.Option.Equals(userSelection, StringComparison.CurrentCultureIgnoreCase)).SingleOrDefault();
                visible = true;
                if (menuSelected != null && menuSelected.VisibleAction != null)
                {
                    Collection<PSObject> aOut = null;
                    try
                    {
                        aOut = menuSelected.VisibleAction.InvokeWithContext(null, null, menuSelected.VisibleActionArgs);
                    }
                    catch (RuntimeException ex)
                    {
                        WriteError(ex.ErrorRecord);
                    }
                    if (aOut != null && aOut.Count > 0 && aOut[0].BaseObject is bool)
                        visible = (bool)aOut[0].BaseObject;
                    else
                        WriteWarning($"Object returned from VisibleAction script is not a boolean value for menu item [{menuSelected.Name}], returned [{aOut}]");
                }
                if (!visible)
                {
                    menuSelected = null;
                }

                if (menuSelected != null)
                {
                    WriteVerbose($"Menu: {menuSelected.Name}");
                    if (menuSelected.ConfirmBeforeInvoke && !this.ShouldProcess(menuSelected.ConfirmTargetData, $"{menuSelected.Name}"))
                    {
                        WriteWarning("Execution aborted");
                    }
                    else if (menuSelected.Action != null)
                    {
                        Host.UI.WriteLine(ConsoleColor.White, ConsoleColor.DarkBlue, $"Invoking [{menuSelected.Name}]");
                        Host.UI.Write($"Args: ");
                        if (menuSelected.ActionArgs != null)
                        {
                            foreach (var arg in menuSelected.ActionArgs)
                            {
                                Host.UI.Write($"{arg} ");
                            }
                        }
                        Host.UI.WriteLine();
                        try
                        {
                            var r_objs = menuSelected.Action.InvokeWithContext(null, null, menuSelected.ActionArgs);
                            foreach (var obj in r_objs)
                                WriteObject(obj.BaseObject);
                        }
                        catch (RuntimeException ex)
                        {
                            WriteError(ex.ErrorRecord);
                        }
                        Host.UI.WriteLine(ConsoleColor.White, ConsoleColor.DarkBlue, $"Invoke [{menuSelected.Name}] Completed!");
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
