using SS.CliMenu.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Text;
using System.Threading.Tasks;

namespace SS.CliMenu
{
    public abstract class UICmdLet : PSCmdlet
    {
        protected CliMenuOptions opts;

        protected void WriteMenuLine(string Text, ConsoleColor Color = System.ConsoleColor.White, bool IsMenuItem = false)
        {
            if (string.IsNullOrEmpty(Text))
                return;

            Host.UI.Write(opts.MenuFillColor, Host.UI.RawUI.BackgroundColor, new string(opts.MenuFillChar, 1));
            string text = SpaceMenuLine(Text, IsMenuItem);
            Host.UI.Write(Color, Host.UI.RawUI.BackgroundColor, text);
            Host.UI.WriteLine(opts.MenuFillColor, Host.UI.RawUI.BackgroundColor, new string(opts.MenuFillChar, 1));
        }
        protected void WriteMenuLine(ScriptBlock Script, ConsoleColor Color = System.ConsoleColor.White, bool IsMenuItem = false)
        {
            Host.UI.Write(opts.MenuFillColor, Host.UI.RawUI.BackgroundColor, new string(opts.MenuFillChar, 1));
            try
            {
                Script.InvokeWithContext(null, null);
            }
            catch (RuntimeException ex)
            {
                WriteError(ex.ErrorRecord);
            }
            Host.UI.WriteLine(opts.MenuFillColor, Host.UI.RawUI.BackgroundColor, new string(opts.MenuFillChar, 1));
        }
        protected void WriteMenuLine(Action<CliMenuOptions> func, ConsoleColor Color = System.ConsoleColor.White, bool IsMenuItem = false)
        {
            Host.UI.Write(opts.MenuFillColor, Host.UI.RawUI.BackgroundColor, new string(opts.MenuFillChar, 1));
            try
            {
                func(opts);
            }
            catch (RuntimeException ex)
            {
                WriteError(ex.ErrorRecord);
            }
            Host.UI.WriteLine(opts.MenuFillColor, Host.UI.RawUI.BackgroundColor, new string(opts.MenuFillChar, 1));
        }

        protected string SpaceMenuLine(string Text, bool IsMenuItem)
        {
            string textLine;
            if (IsMenuItem)
            {
                var sizeLine = Text.Length + 2;
                if (sizeLine > opts.MaxWidth) {
                    Text = Text.Substring(0, opts.MaxWidth - 7);
                    Text += "...";
                }

                textLine = $"  {Text}";
                if (textLine.Length < opts.MaxWidth) {
                    textLine += new string(' ', ((opts.MaxWidth - 1) - textLine.Length - 1));
                }
            }
            else
            {
                var textLength = Text.Length;
                var textBlanks = ((opts.MaxWidth - 2) - textLength) / 2;
                textLine = new string(' ', textBlanks) + Text;
                textLine += new string(' ', ((opts.MaxWidth - 1) - textLine.Length - 1));
            }
            return textLine;
        }

    }
}
