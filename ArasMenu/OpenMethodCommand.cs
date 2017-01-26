//------------------------------------------------------------------------------
// <copyright file="ArasCommand.cs" company="Company">
//     Copyright (c) Company.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

using System;
using System.ComponentModel.Design;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Aras.IOM;
using EnvDTE;
using EnvDTE80;
using System.IO;
using System.Xml;
using System.Windows.Forms;
using System.Collections.Generic;

namespace ArasMenu
{
    /// <summary>
    /// Command handler
    /// </summary>
    internal sealed class OpenMethodCommand
    {
        /// <summary>
        /// Command ID.
        /// </summary>
        public const int CommandId = 0x0100;

        /// <summary>
        /// Command menu group (command set GUID).
        /// </summary>
        public static readonly Guid CommandSet = new Guid("48f16a24-e327-4d26-a79d-17bf0286ac3a");

        /// <summary>
        /// VS Package that provides this command, not null.
        /// </summary>
        private readonly Package package;

        /// <summary>
        /// Initializes a new instance of the <see cref="OpenMethodCommand"/> class.
        /// Adds our command handlers for menu (commands must exist in the command table file)
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        private OpenMethodCommand(Package package)
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
        public static OpenMethodCommand Instance
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
            Instance = new OpenMethodCommand(package);
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
            Project currProj = null;
            ProjectItem configItem = null;
            string configName = "Innovator.config";

            Array projectArray = dte.ActiveSolutionProjects as Array;
            if (projectArray != null && projectArray.Length > 0)
            {
                if (projectArray.Length > 1)
                {
                    util.showError("Only one project can be selected.", "One Project Allowed");
                    return;
                }

                currProj = projectArray.GetValue(0) as Project;
            }

            else
            {
                util.showError("One project must be selected in the Solution Explorer.", "Active Project Required");
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

            if(configDic.TryGetValue("failCheck", out val))
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

            //Dialog to collect method name from user
            var methodForm = new MethodSelectionForm(serverName, databaseName, loginName, loginPassword, defaultMethodSearch);
            var formRes = methodForm.ShowDialog();
            string openID = "NO_ID_SELECTED";
            bool lockChecked = false;

            if (formRes == DialogResult.OK)
            {
                openID = methodForm.returnID;
                lockChecked = methodForm.lockChecked;
            }
            else if(formRes == DialogResult.Abort)
            {
                util.showError("Unable to connect to Aras Innovator with the server, database, and login information provided in Innovator.config of the active project.", "Connection Error");
                return;
            }
            else
            {
                return;
            }

            if (openID == "NO_ID_SELECTED")
            {
                util.showError("No valid method ID was provided.", "Method ID Error");
            }

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
            iQry.setID(openID);
            iQry = iQry.apply();

            if (iQry.isError())
            {
                connection.Logout();
                util.showError(iQry.getErrorString(), "Error");
                return;
            }

            Item lockQry = inn.newItem();
            if (lockChecked)
            {
                lockQry.setType("Method");
                lockQry.setAction("lock");
                lockQry.setID(openID);
                lockQry = lockQry.apply();
            }

            connection.Logout();

            if (lockQry.isError())
            {
                util.showError("The Method will still be opened but attempting to lock it returned the following error: \n" + lockQry.getErrorString(), "Error");
            }

            string methodName = iQry.getProperty("name");
            string methodExtension = ".cs";
            string methodCode = iQry.getProperty("method_code");
            string templatePath;

            // For JavaScript support
            if(iQry.getProperty("method_type", "") == "JavaScript")
            {
                methodExtension = ".js";
                try
                {
                    templatePath = currSol2.GetProjectItemTemplate(jsTemplateName, "CSharp");
                }
                catch (ArgumentException ex)
                {
                    util.showError("The specified JavaScript template could not be found.", "Template Not Found");
                    return;
                }
            }
            else
            {
                try
                {
                    templatePath = currSol2.GetProjectItemTemplate(csTemplateName, "CSharp");
                }
                catch (ArgumentException ex)
                {
                    util.showError("The specified CSharp template could not be found.", "Template Not Found");
                    return;
                }
            }

            string methodString = methodName + methodExtension;
            ProjectItem currItem = util.GetProjectItem(currProj.ProjectItems, methodString);

            if (currItem == null)
            {
                if(string.IsNullOrEmpty(templatePath))
                {
                    util.showError("The specified template could not be found.", "Template Not Found");
                    return;
                }
                currProj.ProjectItems.AddFromTemplate(templatePath,methodString);
                currItem = currProj.ProjectItems.Item(methodString);
            }

            string filePath = currItem.FileNames[0];
            string templateLines = File.ReadAllText(filePath);

            int insertIndex = templateLines.IndexOf(methodInsertTag) + methodInsertTag.Length;
            int endIndex = templateLines.IndexOf(methodEndTag);
            
            if(endIndex < 0 || (insertIndex - methodInsertTag.Length) < 0)
            {
                util.showError("There was an error locating the method_insert_tag or method_end_tag and the method could not be loaded.  Correct the tags and Refresh Method From Server to populate with the method code.", "Method Tag Error");
                util.setStatusBar(methodString + " was created but could not be populated with the method from the server");
                return;
            }

            string modifiedLines = templateLines.Substring(0, insertIndex) + "\n" + methodCode + "\n" + templateLines.Substring(endIndex);

            File.WriteAllText(filePath, modifiedLines);

            util.setStatusBar(methodName + " was succesfully opened and loaded into file " + methodString + " in project "+ currProj.Name);
        }
    }
}
