//------------------------------------------------------------------------------
// <copyright file="ManageLockCommand.cs" company="Company">
//     Copyright (c) Company.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

using System;
using System.ComponentModel.Design;
using System.Globalization;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using EnvDTE;
using System.Xml;
using EnvDTE80;
using System.Windows.Forms;
using System.Collections.Generic;

namespace ArasMenu
{
    /// <summary>
    /// Command handler
    /// </summary>
    internal sealed class ManageLockCommand
    {
        /// <summary>
        /// Command ID.
        /// </summary>
        public const int CommandId = 0x0103;

        /// <summary>
        /// Command menu group (command set GUID).
        /// </summary>
        public static readonly Guid CommandSet = new Guid("48f16a24-e327-4d26-a79d-17bf0286ac3a");

        /// <summary>
        /// VS Package that provides this command, not null.
        /// </summary>
        private readonly Package package;

        /// <summary>
        /// Initializes a new instance of the <see cref="ManageLockCommand"/> class.
        /// Adds our command handlers for menu (commands must exist in the command table file)
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        private ManageLockCommand(Package package)
        {
            if (package == null)
            {
                throw new ArgumentNullException("package");
            }

            this.package = package;

            OleMenuCommandService commandService = this.ServiceProvider.GetService(typeof(IMenuCommandService)) as OleMenuCommandService;
            if (commandService != null)
            {
                var menuCommandID = new CommandID(CommandSet, CommandId);
                var menuItem = new MenuCommand(this.MenuItemCallback, menuCommandID);
                commandService.AddCommand(menuItem);
            }
        }

        /// <summary>
        /// Gets the instance of the command.
        /// </summary>
        public static ManageLockCommand Instance
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the service provider from the owner package.
        /// </summary>
        private IServiceProvider ServiceProvider
        {
            get
            {
                return this.package;
            }
        }

        /// <summary>
        /// Initializes the singleton instance of the command.
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        public static void Initialize(Package package)
        {
            Instance = new ManageLockCommand(package);
        }

        /// <summary>
        /// This function is the callback used to execute the command when the menu item is clicked.
        /// See the constructor to see how the menu item is associated with this function using
        /// OleMenuCommandService service and MenuCommand class.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event args.</param>
        private void MenuItemCallback(object sender, EventArgs e)
        {
            DTE dte = Package.GetGlobalService(typeof(SDTE)) as DTE;
            Solution2 currSol2 = (EnvDTE80.Solution2)dte.Solution;
            CommandUtilities util = new CommandUtilities(this.ServiceProvider);
            ProjectItem configItem = null;
            String configName = "Innovator.config";

            if (dte.ActiveDocument == null)
            {
                util.showError("No active window.", "Active Window Required");
                return;
            }

            Project currProj = dte.ActiveDocument.ProjectItem.ContainingProject;
            dte.ActiveDocument.Save();

            if (string.IsNullOrEmpty(currProj.FullName))
            {
                util.showError("Method must be in a project.", "Project Required");
                return;
            }

            try
            {
                configItem = currProj.ProjectItems.Item(configName);
            }
            catch (ArgumentException ex)
            {
                util.showError("Required Innovator.config file not found in selected project.", "Config File Not Found");
                return;
            }

            string configPath = configItem.FileNames[0];
            XmlDocument configXML = new XmlDocument();
            configXML.Load(configPath);

            Dictionary<string, string> configDic = util.ReadConfigFile(configXML);
            string val = "";

            if (configDic.TryGetValue("failCheck", out val))
            {
                return;
            }

            string csTemplateName;
            configDic.TryGetValue("csTemplateName", out csTemplateName);
            string jsTemplateName;
            configDic.TryGetValue("jsTemplateName", out jsTemplateName);
            string methodInsertTag;
            configDic.TryGetValue("methodInsertTag", out methodInsertTag);
            string methodEndTag;
            configDic.TryGetValue("methodEndTag", out methodEndTag);
            string serverName;
            configDic.TryGetValue("serverName", out serverName);
            string databaseName;
            configDic.TryGetValue("databaseName", out databaseName);
            string loginName;
            configDic.TryGetValue("loginName", out loginName);
            string loginPassword;
            configDic.TryGetValue("loginPassword", out loginPassword);
            string defaultMethodSearch;
            configDic.TryGetValue("defaultMethodSearch", out defaultMethodSearch);

            string fileName = dte.ActiveDocument.Name;
            string methodName = fileName.Substring(0, fileName.LastIndexOf('.'));

            //Dialog to manage locking
            var methodForm = new ManageLockForm(serverName, databaseName, loginName, loginPassword, methodName, this.ServiceProvider);
            var formRes = methodForm.ShowDialog();

            if (formRes == DialogResult.Abort)
            {
                util.showError("Unable to connect to Aras Innovator with the server, database, and login information provided in Innovator.config of the active project.", "Connection Error");
                return;
            }

            return;
        }
    }
}
