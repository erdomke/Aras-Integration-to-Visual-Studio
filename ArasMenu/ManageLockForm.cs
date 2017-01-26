using Aras.IOM;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ArasMenu
{
    public partial class ManageLockForm : Form
    {
        private string serverName;
        private string databaseName;
        private string loginName;
        private string loginPassword;
        private string methodName;
        private string methodID;
        private CommandUtilities util;

        public ManageLockForm(string serverNameArg, string databaseNameArg, string loginNameArg, string loginPasswordArg, string methodNameArg, IServiceProvider serviceProvider)
        {
            InitializeComponent();

            serverName = serverNameArg;
            databaseName = databaseNameArg;
            loginName = loginNameArg;
            loginPassword = loginPasswordArg;
            methodName = methodNameArg;
            util = new CommandUtilities(serviceProvider);

            HttpServerConnection connection;
            Aras.IOM.Innovator inn;

            connection = IomFactory.CreateHttpServerConnection(serverName, databaseName, loginName, loginPassword);
            Aras.IOM.Item iLogin = connection.Login();
            if (iLogin.isError())
            {
                this.DialogResult = DialogResult.Abort;
                this.Close();
                return;
            }

            inn = new Aras.IOM.Innovator(connection);

            Item iQry = inn.newItem();
            iQry.setType("Method");
            iQry.setProperty("name", methodName);
            iQry.setAction("get");
            iQry = iQry.apply();

            connection.Logout();

            if (iQry.isError())
            {
                MessageBox.Show(iQry.getErrorString(), "Server Returned Error", MessageBoxButtons.OK);
                return;
            }

            methodID = iQry.getID();
            updateLockStatusLabel(iQry);
        }

        private void unlockButton_Click(object sender, EventArgs e)
        {
            HttpServerConnection connection;
            Aras.IOM.Innovator inn;

            connection = IomFactory.CreateHttpServerConnection(serverName, databaseName, loginName, loginPassword);
            Aras.IOM.Item iLogin = connection.Login();
            if (iLogin.isError())
            {
                this.DialogResult = DialogResult.Abort;
                this.Close();
                return;
            }

            inn = new Aras.IOM.Innovator(connection);

            Item iQry = inn.newItem();
            iQry.setType("Method");
            iQry.setID(methodID);
            iQry.setAction("unlock");
            iQry = iQry.apply();

            connection.Logout();

            if (iQry.isError())
            {
                util.showError(iQry.getErrorString(), "Server Returned Error");
                return;
            }
            else
            {
                util.setStatusBar(methodName+" successfully unlocked");
            }

            updateLockStatusLabel(iQry);
        }

        private void lockButton_Click(object sender, EventArgs e)
        {
            HttpServerConnection connection;
            Aras.IOM.Innovator inn;

            connection = IomFactory.CreateHttpServerConnection(serverName, databaseName, loginName, loginPassword);
            Aras.IOM.Item iLogin = connection.Login();
            if (iLogin.isError())
            {
                this.DialogResult = DialogResult.Abort;
                this.Close();
                return;
            }

            inn = new Aras.IOM.Innovator(connection);

            Item iQry = inn.newItem();
            iQry.setType("Method");
            iQry.setID(methodID);
            iQry.setAction("lock");
            iQry = iQry.apply();

            connection.Logout();

            if (iQry.isError())
            {
                util.showError(iQry.getErrorString(), "Server Returned Error");
                return;
            }
            else
            {
                util.setStatusBar(methodName+" successfully locked");
            }

            updateLockStatusLabel(iQry);
        }

        private void updateLockStatusLabel(Item iQry)
        {
            string statusText = methodName + " Lock Status:\n";

            if (string.IsNullOrEmpty(iQry.getProperty("locked_by_id", "")))
            {
                statusText += "Unlocked";
            }
            else
            {
                statusText += "Locked By " + iQry.getPropertyAttribute("locked_by_id", "keyed_name");
            }

            this.statusLabel.Text = statusText;
        }
    }
}
