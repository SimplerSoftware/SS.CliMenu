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
    /// Model to store options for the a menu.
    /// <para type="description">Model to store options for the a menu.</para>
    /// </summary>
    public class CliMenuOptions
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="maxWidth"></param>
        public CliMenuOptions(int maxWidth)
        {
            this.MaxWidth = maxWidth;
        }

        /// <summary>
        /// The character to be used to fill borders in the menu.
        /// </summary>
        public char MenuFillChar { get; set; } = '*';
        /// <summary>
        /// The foreground color to be used when drawing borders with the <see cref="MenuFillChar"/>.
        /// </summary>
        public ConsoleColor MenuFillColor { get; set; } = ConsoleColor.White;
        /// <summary>
        /// Header text for the menu shown under the menu's <see cref="MenuObject.DisplayName"/> and <see cref="MenuObject.Description"/>.
        /// </summary>
        public string Heading { get; set; } = "";
        /// <summary>
        /// Foreground text color for the <see cref="Heading"/> text.
        /// </summary>
        public ConsoleColor HeadingColor { get; set; } = ConsoleColor.White;
        /// <summary>
        /// Sub heading shown below main <see cref="Heading"/> text.
        /// </summary>
        public string SubHeading { get; set; } = "";
        /// <summary>
        /// Foreground text color for the <see cref="SubHeading"/> text.
        /// </summary>
        public ConsoleColor SubHeadingColor { get; set; } = ConsoleColor.White;
        /// <summary>
        /// Text to be shown on the footer of the menu.
        /// </summary>
        public string FooterText { get; set; } = "";
        /// <summary>
        /// Foreground text color of the <see cref="FooterText"/> text.
        /// </summary>
        public ConsoleColor FooterTextColor { get; set; } = ConsoleColor.White;
        /// <summary>
        /// Foreground text color for menu items that have no <see cref="HeaderScript"/>/<see cref="HeaderFunc"/> assigned.
        /// </summary>
        public ConsoleColor ViewOnlyColor { get; set; } = ConsoleColor.DarkGray;
        /// <summary>
        /// Default foreground text color for menu items.
        /// </summary>
        public ConsoleColor MenuItemColor { get; set; } = ConsoleColor.White;
        /// <summary>
        /// Foreground text color for menu's <see cref="MenuObject.DisplayName"/> and <see cref="MenuObject.Description"/>.
        /// </summary>
        public ConsoleColor MenuNameColor { get; set; } = ConsoleColor.White;
        /// <summary>
        /// The menu's max character width, defaults to current window's character width.
        /// </summary>
        public int MaxWidth { get; set; } = 80;
        /// <summary>
        /// Script to call to dynamically build header, in place of <see cref="Heading"/> text. (<see cref="HeaderScript"/> has precedence over <see cref="HeaderFunc"/>)
        /// </summary>
        public ScriptBlock HeaderScript { get; set; }
        /// <summary>
        /// Script to call to dynamically build header, in place of <see cref="Heading"/> text. (<see cref="HeaderScript"/> has precedence over <see cref="HeaderFunc"/>)
        /// </summary>
        public Action<MenuObject, CliMenuOptions> HeaderFunc { get; set; }
    }
}
