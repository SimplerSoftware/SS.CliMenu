using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Text;
using System.Threading.Tasks;

namespace SS.CliMenu
{
    public class MenuItemObject
    {
        public string Option { get; internal set; }
        public string Name { get; internal set; }
        public string DisplayName { get; internal set; }
        public string Description { get; internal set; }
        public ScriptBlock Action { get; internal set; }
        public object[] ActionArgs { get; internal set; }
        public ConsoleColor? ForegroundColor { get; internal set; }
        public bool ConfirmBeforeInvoke { get; internal set; }
        public string ConfirmTargetData { get; internal set; }
        public SwitchParameter IsSpace { get; internal set; }
        public ScriptBlock VisibleAction { get; internal set; }
        public object[] VisibleActionArgs { get; internal set; }
    }
}
