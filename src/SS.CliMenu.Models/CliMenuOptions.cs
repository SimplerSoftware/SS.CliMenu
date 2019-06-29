using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Host;
using System.Text;
using System.Threading.Tasks;

namespace SS.CliMenu.Models
{
    /// <summary>
    /// 
    /// <para type="description"></para>
    /// </summary>
    public class CliMenuOptions
    {
        public CliMenuOptions(int maxWidth)
        {
            this.MaxWidth = maxWidth;
        }

        public char MenuFillChar { get; set; } = '*';
        public ConsoleColor MenuFillColor { get; set; } = ConsoleColor.White;
        public string Heading { get; set; } = "";
        public ConsoleColor HeadingColor { get; set; } = ConsoleColor.White;
        public string SubHeading { get; set; } = "";
        public ConsoleColor SubHeadingColor { get; set; } = ConsoleColor.White;
        public string FooterText { get; set; } = "";
        public ConsoleColor FooterTextColor { get; set; } = ConsoleColor.White;
        public ConsoleColor ViewOnlyColor { get; set; } = ConsoleColor.DarkGray;
        public ConsoleColor MenuItemColor { get; set; } = ConsoleColor.White;
        public ConsoleColor MenuNameColor { get; set; } = ConsoleColor.White;
        public int MaxWidth { get; set; } = 80;
        public ScriptBlock HeaderScript { get; set; }
        public Action<MenuObject, CliMenuOptions> HeaderFunc { get; set; }
    }
}
