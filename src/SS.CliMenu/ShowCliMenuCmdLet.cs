using Microsoft.ApplicationInsights;
using SS.CliMenu.Metrics;
using SS.CliMenu.Models;
using SS.CliMenu.Properties;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
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
        private string userSelection;

        public ShowCliMenuCmdLet()
        {
            MetricData = new MetricData();
        }

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
        /// <summary>
        /// Metric data used for metric logging.
        /// <para type="description">Metric data used for metric logging.</para>
        /// </summary>
        [Parameter]
        [ValidateNotNull]
        public MetricData MetricData { get; set; }

        protected override string DataCollectionWarning
        {
            get
            {
                if (MetricData.LogPIIData)
                    return string.Format(CultureInfo.CurrentCulture, Resources.NotAnonDataCollectionMessage, MetricData.DataCollectedBy);
                else
                    return string.Format(CultureInfo.CurrentCulture, Resources.AnonDataCollectionMessage, MetricData.DataCollectedBy);
            }
        }

        protected override string InstrumentationKey
        {
            get
            {
                return MetricData.CollectionKey;
            }
        }

        protected override string ProductName => MetricData?.Product ?? ".Default";

        protected override void InitializeQosEvent()
        {
            if (MetricData == null)
                MetricData = new MetricData();
            this._metricHelper.LogPIIData = MetricData.LogPIIData;

            var commandAlias = this.GetType().Name;
            if (this.MyInvocation != null && this.MyInvocation.MyCommand != null)
            {
                commandAlias = this.MyInvocation.MyCommand.Name;
            }

            _qosEvent = new PSQoSEvent()
            {
                CommandName = commandAlias,
                Id = Menu.Name,
                Name = Menu.Name,
                Title = Menu.DisplayName,
                ModuleName = MetricData.ModuleName ?? $"{this.MyInvocation.MyCommand.ModuleName}",
                ModuleVersion = MetricData.ModuleVersion ?? $"{this.MyInvocation.MyCommand.Version}",
                UserAgent = MetricData.UserAgent,
                AppVersion = MetricData.AppVersion,
                RoleName = MetricData.RoleName,
                AccountId = MetricData.AccountId,
                ClientRequestId = this._clientRequestId,
                SessionId = _sessionId,
                IsSuccess = true,
                ParameterSetName = this.ParameterSetName
            };

            if (this.MyInvocation != null && !string.IsNullOrWhiteSpace(this.MyInvocation.InvocationName))
            {
                _qosEvent.InvocationName = this.MyInvocation.InvocationName;
            }

            if (this.MyInvocation != null && this.MyInvocation.BoundParameters != null
                && this.MyInvocation.BoundParameters.Keys != null)
            {
                _qosEvent.Parameters = string.Join(" ",
                    this.MyInvocation.BoundParameters.Keys.Select(
                        s => string.Format(CultureInfo.InvariantCulture, "-{0} ***", s)));
            }

            if (!string.IsNullOrWhiteSpace(MetricData.AuthenticatedUserId) &&
                this._metricHelper.LogPIIData)
            {
                _qosEvent.AuthenticatedUserId = MetricData.AuthenticatedUserId;
            }
            if (!string.IsNullOrWhiteSpace(MetricData.UserId) &&
                MetricData.UserId != "defaultid")
            {
                if (this._metricHelper.LogPIIData)
                    _qosEvent.Uid = MetricData.UserId;
                else
                    _qosEvent.Uid = MetricHelper.GenerateSha256HashString(MetricData.UserId);
            }
            else
            {
                _qosEvent.Uid = "defaultid";
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public override void ExecuteCmdlet()
        {
            base.ExecuteCmdlet();
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
                Stopwatch processingTime = Stopwatch.StartNew();
                Regex rxOption = new Regex("^\\d+$");
                var menuLines = new ArrayList();

                if (Menu == null)
                {
                    WriteError(new ErrorRecord(new ArgumentNullException("Menu", "Menu parameter must not be null."), "MenuNotGiven", ErrorCategory.InvalidArgument, null));
                    return;
                }

                var menuFrame = new string(MenuOptions.MenuFillChar, (MenuOptions.MaxWidth - 2));
                var menuEmptyLine = new string(' ', (MenuOptions.MaxWidth - 2));
                // Top Header border
                WriteMenuLine(menuFrame, MenuOptions.MenuFillColor);
                // Menu Display Name
                WriteMenuLine(Menu.DisplayName, MenuOptions.MenuNameColor);
                if (!string.IsNullOrWhiteSpace(Menu.Description))
                    WriteMenuLine(Menu.Description, MenuOptions.MenuNameColor);

                WriteMenuLine(menuEmptyLine, MenuOptions.MenuFillColor);

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
                else if (MenuOptions.HeaderScript != null)
                {
                    try
                    {
                        MenuOptions.HeaderScript.InvokeWithContext(null, null);
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
                        HeaderFunc(this.Menu, MenuOptions);
                    }
                    catch (RuntimeException ex)
                    {
                        WriteError(ex.ErrorRecord);
                    }
                }
                else if (MenuOptions.HeaderFunc != null)
                {
                    try
                    {
                        MenuOptions.HeaderFunc(this.Menu, MenuOptions);
                    }
                    catch (RuntimeException ex)
                    {
                        WriteError(ex.ErrorRecord);
                    }
                }
                else
                {
                    WriteMenuLine(MenuOptions.Heading, MenuOptions.HeadingColor);
                    WriteMenuLine(menuEmptyLine, MenuOptions.MenuFillColor);
                    WriteMenuLine(MenuOptions.SubHeading, MenuOptions.SubHeadingColor);
                }
                WriteMenuLine(menuEmptyLine, MenuOptions.MenuFillColor);

                // Bottom Header border
                WriteMenuLine(menuFrame, MenuOptions.MenuFillColor);
                WriteMenuLine(menuEmptyLine, MenuOptions.MenuFillColor);

                var idxOptions = this.Menu.MenuItems.Where(i => rxOption.IsMatch($"{i.Option}")).Select(i => int.Parse(i.Option)).ToArray();
                int maxIdxOptions = (idxOptions.Length == 0) ? 0 : idxOptions.OrderBy(i => i).Max();

                bool visible;
                foreach (var item in Menu.MenuItems)
                {
                    ConsoleColor menuColor;
                    if (!string.IsNullOrWhiteSpace(item.Option) && !(item.Func == null && item.Script == null))
                        menuColor = MenuOptions.MenuItemColor;
                    else
                        menuColor = MenuOptions.ViewOnlyColor;

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

                WriteMenuLine(menuEmptyLine, MenuOptions.MenuFillColor);
                WriteMenuLine("Q Exit Menu", ConsoleColor.Red, true);

                WriteMenuLine(menuEmptyLine, MenuOptions.MenuFillColor);
                WriteMenuLine(menuEmptyLine, MenuOptions.MenuFillColor);

                WriteMenuLine(MenuOptions.FooterText, MenuOptions.FooterTextColor);
                WriteMenuLine(menuEmptyLine, MenuOptions.MenuFillColor);

                // Bottom border
                WriteMenuLine(menuFrame, MenuOptions.MenuFillColor);

                Host.UI.Write($"Please choose a option [{DefaultOption}] ");
                processingTime.Stop();
                LogPerfEvent(processingTime.Elapsed + MetricData.ProcessingTime);
                userSelection = Host.UI.ReadLine();
                WriteVerbose($"Selection: {userSelection}");

                if (string.IsNullOrWhiteSpace(userSelection))
                {
                    userSelection = DefaultOption;
                    WriteVerbose($"Using default option: {userSelection}");
                }
                else
                    WriteVerbose($"Selection: {userSelection}");
                _qosEvent.Selected = userSelection;

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

            }
        }

        MenuLine NewMenuLine(string text, ConsoleColor color = ConsoleColor.White, bool isMenuItem = false)
        {
            string textLine;

            if (isMenuItem)
            {
                textLine = $"  {text}";
                textLine += new string(' ', ((MenuOptions.MaxWidth - 1) - textLine.Length - 1));
            }
            else
            {
                var textLength = text.Length;
                var textBlanks = ((MenuOptions.MaxWidth - 2) - textLength) / 2;
                textLine = new string(' ', textBlanks) + text;
                textLine += new string(' ', ((MenuOptions.MaxWidth - 1) - textLine.Length - 1));
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
