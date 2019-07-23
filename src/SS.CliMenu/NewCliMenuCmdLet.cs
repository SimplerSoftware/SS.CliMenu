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
    /// You can create as many menus you like, however you may only have one main Menu. The Menu must have a name, hence the Name parameter is Mandatory. The first Menu you create will become the main Menu even if you do not specify the IsMainMenu switch.
    /// <para type="synopsis">Create a new Menu</para>
    /// <para type="description">You can create as many menus you like, however you may only have one main Menu. The Menu must have a name, hence the Name parameter is Mandatory. The first Menu you create will become the main Menu even if you do not specify the IsMainMenu switch.</para>
    /// <example>
    ///   <title>Default usage.</title>
    ///   <description>This will create a new Menu with name MainMenu. If this is the first Menu, it will be created as a main Menu</description>
    ///   <code>New-CliMenu -Name "MainMenu"</code>
    /// </example>
    /// </summary>
    [Cmdlet(VerbsCommon.New, "CliMenu")]
    [OutputType(typeof(MenuObject))]
    public class NewCliMenuCmdLet : PSCmdlet
    {
        /// <summary>
        /// Normally you would like to specify a name without space and Camel-case the name.
        /// <para type="description">Normally you would like to specify a name without space and Camel-case the name.</para>
        /// </summary>
        [Parameter(Mandatory = true)]
        public string Name { get; set; }
        /// <summary>
        /// 
        /// <para type="description"></para>
        /// </summary>
        [Parameter()]
        public string DisplayName { get; set; }
        /// <summary>
        /// 
        /// <para type="description"></para>
        /// </summary>
        [Parameter()]
        public string Description { get; set; }

        /// <summary>
        /// 
        /// </summary>
        protected override void BeginProcessing()
        {
            WriteVerbose($"{MyInvocation.InvocationName} - START");
            base.BeginProcessing();
        }

        /// <summary>
        /// 
        /// </summary>
        protected override void ProcessRecord()
        {
            WriteVerbose($"Creating menu [{Name}]");
            MenuObject newMenu = new MenuObject
            {
                Name = this.Name,
                DisplayName = this.DisplayName,
                Description = this.Description,
                //IsMainMenu = $IsMainMenu
                MenuItems = new List<MenuItemObject>()
            };
            WriteObject(newMenu);

            base.ProcessRecord();
        }
        /// <summary>
        /// 
        /// </summary>
        protected override void EndProcessing()
        {
            WriteVerbose($"{MyInvocation.InvocationName} - END");
            base.EndProcessing();
        }

    }
}
