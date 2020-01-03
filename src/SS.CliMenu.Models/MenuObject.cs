using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SS.CliMenu.Models
{
    /// <summary>
    /// A menu object that stores a menu's configuration settings.
    /// <para type="description">A menu object that stores a menu's configuration settings.</para>
    /// </summary>
    public class MenuObject
    {
        /// <summary>
        /// The menu's name, a unique name used for metric logging.
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// The display name of the menu, shown at the top of the menu.
        /// </summary>
        public string DisplayName { get; set; }
        /// <summary>
        /// The description of the menu, show below the <see cref="DisplayName"/>.
        /// </summary>
        public string Description { get; set; }
        /// <summary>
        /// List of menu items for this menu.
        /// </summary>
        public List<MenuItemObject> MenuItems { get; set; }
    }
}
