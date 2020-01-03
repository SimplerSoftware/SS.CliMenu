using SS.CliMenu.Metrics;
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
    /// Base abstract Cmdlet with helper functions for UI
    /// </summary>
    public abstract class UICmdLet : MeasurablePSCmdlet
    {
        private CliMenuOptions _opts;
        protected CliMenuOptions MenuOptions
        {
            get
            {
                return _opts ?? (_opts = GetVariableValue("CliMenuOptions", new CliMenuOptions(this.Host.UI.RawUI.WindowSize.Width)) as CliMenuOptions);
            }
        }

        internal void WriteMenuLine(string Text, ConsoleColor Color = System.ConsoleColor.White, bool IsMenuItem = false)
        {
            if (string.IsNullOrEmpty(Text))
                return;

            Host.UI.Write(MenuOptions.MenuFillColor, Host.UI.RawUI.BackgroundColor, new string(MenuOptions.MenuFillChar, 1));
            string text = SpaceMenuLine(Text, IsMenuItem);
            Host.UI.Write(Color, Host.UI.RawUI.BackgroundColor, text);
            Host.UI.WriteLine(MenuOptions.MenuFillColor, Host.UI.RawUI.BackgroundColor, new string(MenuOptions.MenuFillChar, 1));
        }
        internal void WriteMenuLine(ScriptBlock Script, ConsoleColor Color = System.ConsoleColor.White, bool IsMenuItem = false)
        {
            Host.UI.Write(MenuOptions.MenuFillColor, Host.UI.RawUI.BackgroundColor, new string(MenuOptions.MenuFillChar, 1));
            try
            {
                Script.InvokeWithContext(null, null);
            }
            catch (RuntimeException ex)
            {
                WriteError(ex.ErrorRecord);
            }
            Host.UI.WriteLine(MenuOptions.MenuFillColor, Host.UI.RawUI.BackgroundColor, new string(MenuOptions.MenuFillChar, 1));
        }
        internal void WriteMenuLine(Action<CliMenuOptions> func, ConsoleColor Color = System.ConsoleColor.White, bool IsMenuItem = false)
        {
            Host.UI.Write(MenuOptions.MenuFillColor, Host.UI.RawUI.BackgroundColor, new string(MenuOptions.MenuFillChar, 1));
            try
            {
                func(MenuOptions);
            }
            catch (RuntimeException ex)
            {
                WriteError(ex.ErrorRecord);
            }
            Host.UI.WriteLine(MenuOptions.MenuFillColor, Host.UI.RawUI.BackgroundColor, new string(MenuOptions.MenuFillChar, 1));
        }

        internal string SpaceMenuLine(string Text, bool IsMenuItem)
        {
            string textLine;
            if (IsMenuItem)
            {
                var sizeLine = Text.Length + 2; // Two space prefix added below
                if (sizeLine > (MenuOptions.MaxWidth - 2))
                {
                    Text = Text.Substring(0, MenuOptions.MaxWidth - 7);
                    Text += "...";
                }

                textLine = $"  {Text}";
                if (textLine.Length < (MenuOptions.MaxWidth - 2))
                {
                    textLine += new string(' ', ((MenuOptions.MaxWidth - 1) - textLine.Length - 1));
                }
            }
            else
            {
                var textLength = Text.Length;
                var textBlanks = ((MenuOptions.MaxWidth - 2) - textLength) / 2;
                textLine = new string(' ', textBlanks) + Text;
                textLine += new string(' ', ((MenuOptions.MaxWidth - 1) - textLine.Length - 1));
            }
            return textLine;
        }

    }
}
