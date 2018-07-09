using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SS.CliMenu
{
    /// <summary>
    /// A menu object that stores a menu's configuration settings.
    /// <para type="description">A menu object that stores a menu's configuration settings.</para>
    /// </summary>
    public class MenuObject
    {
        public string Name { get; internal set; }
        public string DisplayName { get; internal set; }
        public string Description { get; internal set; }
        public List<MenuItemObject> MenuItems { get; internal set; }
    }
}
