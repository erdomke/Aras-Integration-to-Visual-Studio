using EnvDTE;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace ArasMenu
{
    class CommandUtilities
    {
        private IServiceProvider serviceProvider;

        public CommandUtilities(IServiceProvider spArg)
        {
            serviceProvider = spArg;
        }

        public ProjectItem GetProjectItem(ProjectItems proj, string fileName)
        {
            ProjectItem ret = null;
            foreach (ProjectItem item in proj)
            {
                if (item.Name == fileName)
                {
                    ret = item;
                }
                else if (item.ProjectItems.Count > 0)
                {
                    ret = GetProjectItem(item.ProjectItems, fileName);
                }

                if (ret != null)
                {
                    break;
                }
            }

            return ret;
        }

        public Dictionary<string, string> ReadConfigFile(XmlDocument configContent)
        {
            Dictionary<string, string> ret = new Dictionary<string, string>();

            XmlNode valueNode = configContent.SelectSingleNode("//configuration/csharp_template");
            if (valueNode != null)
            {
                 ret.Add("csTemplateName", valueNode.InnerText);
            }
            else
            {
                showError("The config file does not contain the <csharp_template> to use.", "Config Template Not Found");
                ret.Add("failCheck", "1");
                return ret;
            }

            valueNode = configContent.SelectSingleNode("//configuration/javascript_template");
            if (valueNode != null)
            {
                ret.Add("jsTemplateName", valueNode.InnerText);
            }
            /*else  Throw no error, javascript template not currently used
            {
                showError("The config file does not contain the <javascript_template> to use.", "Config Template Not Found");
                ret.Add("failCheck", "1");
                return ret;
            }*/

            valueNode = configContent.SelectSingleNode("//configuration/method_insert_tag");
            if (valueNode != null)
            {
                ret.Add("methodInsertTag", valueNode.InnerText);
            }
            else
            {
                showError("The config file does not contain the <method_insert_tag> to use.", "Config Method Insert Tag Not Found");
                ret.Add("failCheck", "1");
                return ret;
            }

            valueNode = configContent.SelectSingleNode("//configuration/method_end_tag");
            if (valueNode != null)
            {
                ret.Add("methodEndTag", valueNode.InnerText);
            }
            else
            {
                showError("The config file does not contain the <method_end_tag> to use.", "Config Method End Tag Not Found");
                ret.Add("failCheck", "1");
                return ret;
            }

            valueNode = configContent.SelectSingleNode("//configuration/server");
            if (valueNode != null)
            {
                ret.Add("serverName", valueNode.InnerText);
            }
            else
            {
                showError("The config file does not contain the <server> to use.", "Config Server Not Found");
                ret.Add("failCheck", "1");
                return ret;
            }

            valueNode = configContent.SelectSingleNode("//configuration/database");
            if (valueNode != null)
            {
                ret.Add("databaseName", valueNode.InnerText);
            }
            else
            {
                showError("The config file does not contain the <database> to use.", "Config Database Not Found");
                ret.Add("failCheck", "1");
                return ret;
            }

            valueNode = configContent.SelectSingleNode("//configuration/login_user");
            if (valueNode != null)
            {
                ret.Add("loginName", valueNode.InnerText);
            }
            else
            {
                showError("The config file does not contain the <login_user> to use.", "Config Login User Not Found");
                ret.Add("failCheck", "1");
                return ret;
            }

            valueNode = configContent.SelectSingleNode("//configuration/login_password");
            if (valueNode != null)
            {
                ret.Add("loginPassword", valueNode.InnerText);
            }
            else
            {
                showError("The config file does not contain the <login_password> to use.", "Config Login Password Not Found");
                ret.Add("failCheck", "1");
                return ret;
            }

            //Optional collection of default method search string to use
            valueNode = configContent.SelectSingleNode("//configuration/default_method_name_search");
            if (valueNode != null)
            {
                ret.Add("defaultMethodSearch", valueNode.InnerText);
            }
            else
            {
                ret.Add("defaultMethodSearch", "NO_DEFAULT_MN_SEARCH_PROVIDED_IN_CONFIG");
            }

            //Optional collection of deployment server
            valueNode = configContent.SelectSingleNode("//configuration/deployment_server");
            if (valueNode != null)
            {
                ret.Add("deployment_serverName", valueNode.InnerText);
            }

            //Optional collection of deployment database
            valueNode = configContent.SelectSingleNode("//configuration/deployment_database");
            if (valueNode != null)
            {
                ret.Add("deployment_databaseName", valueNode.InnerText);
            }

            //Optional collection of deployment login user
            valueNode = configContent.SelectSingleNode("//configuration/deployment_login_user");
            if (valueNode != null)
            {
                ret.Add("deployment_loginName", valueNode.InnerText);
            }

            //Optional collection of deployment login password
            valueNode = configContent.SelectSingleNode("//configuration/deployment_login_password");
            if (valueNode != null)
            {
                ret.Add("deployment_loginPassword", valueNode.InnerText);
            }

            return ret;
        }

        public void showError(string errorMessage, string errorTitle)
        {
            VsShellUtilities.ShowMessageBox(
            serviceProvider,
            errorMessage,
            errorTitle,
            OLEMSGICON.OLEMSGICON_INFO,
            OLEMSGBUTTON.OLEMSGBUTTON_OK,
            OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
        }

        public int promptYesNo(string errorMessage, string errorTitle)
        {
            int ret;

            ret = VsShellUtilities.ShowMessageBox(
            serviceProvider,
            errorMessage,
            errorTitle,
            OLEMSGICON.OLEMSGICON_INFO,
            OLEMSGBUTTON.OLEMSGBUTTON_YESNO,
            OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);

            return ret;
        }

        public void setStatusBar(string statusMessage)
        {
            IVsStatusbar statusBar = (IVsStatusbar)serviceProvider.GetService(typeof(SVsStatusbar));

            // Check if status bar is frozen and unfreeze it if so
            int frozen;
            statusBar.IsFrozen(out frozen);
            if (frozen != 0)
            {
                statusBar.FreezeOutput(0);
            }

            // Set the status bar text
            statusBar.SetText(DateTime.Now+" - "+statusMessage);
        }
    }
}
