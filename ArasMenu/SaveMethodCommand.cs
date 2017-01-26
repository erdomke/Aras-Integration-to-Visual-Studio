//------------------------------------------------------------------------------
// <copyright file="SaveMethodCommand.cs" company="Company">
//     Copyright (c) Company.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

using System;
using System.ComponentModel.Design;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using EnvDTE;
using System.IO;
using Aras.IOM;
using EnvDTE80;
using System.Xml;
using System.Collections.Generic;

namespace ArasMenu
{
    /// <summary>
    /// Command handler
    /// </summary>
    internal sealed class SaveMethodCommand
    {
        /// <summary>
        /// Command ID.
        /// </summary>
        public const int CommandId = 0x0102;

        /// <summary>
        /// Command menu group (command set GUID).
        /// </summary>
        public static readonly Guid CommandSet = new Guid("48f16a24-e327-4d26-a79d-17bf0286ac3a");

        /// <summary>
        /// VS Package that provides this command, not null.
        /// </summary>
        private readonly Package package;

        /// <summary>
        /// Initializes a new instance of the <see cref="SaveMethodCommand"/> class.
        /// Adds our command handlers for menu (commands must exist in the command table file)
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        private SaveMethodCommand(Package package)
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
        public static SaveMethodCommand Instance
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
            Instance = new SaveMethodCommand(package);
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
            ProjectItem currItem = null;
            ProjectItem configItem = null;
            String methodCode = "";
            String configName = "Innovator.config";

            if(dte.ActiveDocument == null)
            {
                util.showError("No active window.", "Active Window Required");
                return;
            }

            Project currProj = dte.ActiveDocument.ProjectItem.ContainingProject;
            dte.ActiveDocument.Save();
            
            if(string.IsNullOrEmpty(currProj.FullName))
            {
                util.showError("Method must be in a project.", "Project Required");
                return;
            }

            try
            {
                configItem = currProj.ProjectItems.Item(configName);
            }
            catch(ArgumentException ex)
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
            string methodExtension = fileName.Substring(fileName.LastIndexOf('.'));

            try
            {
                currItem = dte.ActiveDocument.ProjectItem;
            }
            catch (ArgumentException ex)
            {
                util.showError("Method file not found in current project.", "Method File Not Found");
                return;
            }

            string filePath = currItem.FileNames[0];
            string templateLines = File.ReadAllText(filePath);
            int insertIndex = templateLines.IndexOf(methodInsertTag) + methodInsertTag.Length;
            int endIndex = templateLines.IndexOf(methodEndTag);
            
            methodCode = templateLines.Substring(insertIndex+1, endIndex-insertIndex-2);

            //Connect to Aras Server
            HttpServerConnection connection;
            Aras.IOM.Innovator inn;

            connection = IomFactory.CreateHttpServerConnection(serverName, databaseName, loginName, loginPassword);
            Aras.IOM.Item iLogin = connection.Login();
            if (iLogin.isError())
            {
                util.showError("Unable to connect to Aras Innovator with the server, database, and login information provided in Innovator.config of the active project.", "Connection Error");
                return;
            }

            inn = new Aras.IOM.Innovator(connection);

            Item iQry = inn.newItem();
            iQry.setType("Method");
            iQry.setAction("get");
            iQry.setProperty("name", methodName);
            iQry = iQry.apply();

            //If method already exists, update, otherwise prompt to add it instead
            bool methodAdded = false;
            if(iQry.isError() || iQry.isEmpty())
            {
                int response;
                response = util.promptYesNo("The method does not exist on the server.  Would you like to create it?", "Create Method");

                if(response == 6)
                {
                    Item addQry = inn.newItem();
                    addQry.setType("Method");
                    addQry.setAction("add");
                    addQry.setProperty("name", methodName);
                    addQry.setProperty("method_code", methodCode);

                    if(methodExtension == ".js")
                    {
                        addQry.setProperty("method_type", "JavaScript");
                    }
                    else
                    {
                        addQry.setProperty("method_type", "C#");
                    }

                    addQry = addQry.apply();

                    if (addQry.isError())
                    {
                        connection.Logout();
                        util.showError(addQry.getErrorString(), "Error");
                        return;
                    }
                    methodAdded = true;

                    addQry.setAction("lock");
                    addQry = addQry.apply();
                    if (addQry.isError())
                    {
                        connection.Logout();
                        util.showError(addQry.getErrorString(), "Error");
                        return;
                    }
                }
                else
                {
                    connection.Logout();
                    return;
                }
            }
            else
            {
                iQry.setAction("update");
                iQry.setProperty("method_code", methodCode);
                iQry = iQry.apply();
            }

            connection.Logout();

            if (iQry.isError() && !methodAdded)
            {
                util.showError(iQry.getErrorString(), "Error");
                return;
            }


            util.setStatusBar(methodName + " was succesfully saved to server");
        }
    }
}
