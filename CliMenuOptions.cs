using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Host;
using System.Text;
using System.Threading.Tasks;

namespace SS.CliMenu
{
    /// <summary>
    /// 
    /// <para type="description"></para>
    /// </summary>
    public class CliMenuOptions
    {
        internal CliMenuOptions(PSHost host)
        {
            this.MaxWidth = host.UI.RawUI.WindowSize.Width;
        }

        public char MenuFillChar { get; internal set; } = '*';
        public ConsoleColor MenuFillColor { get; internal set; } = ConsoleColor.White;
        public string Heading { get; internal set; } = "";
        public ConsoleColor HeadingColor { get; internal set; } = ConsoleColor.White;
        public string SubHeading { get; internal set; } = "";
        public ConsoleColor SubHeadingColor { get; internal set; } = ConsoleColor.White;
        public string FooterText { get; internal set; } = "";
        public ConsoleColor FooterTextColor { get; internal set; } = ConsoleColor.White;
        public ConsoleColor ViewOnlyColor { get; internal set; } = ConsoleColor.DarkGray;
        public ConsoleColor MenuItemColor { get; internal set; } = ConsoleColor.White;
        public ConsoleColor MenuNameColor { get; internal set; } = ConsoleColor.White;
        public int MaxWidth { get; internal set; } = 80;
        public ScriptBlock HeaderAction { get; internal set; }
    }
}
