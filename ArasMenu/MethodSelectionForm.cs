using Aras.IOM;
using System;
using System.Data;
using System.Windows.Forms;

namespace ArasMenu
{
    public partial class MethodSelectionForm : Form
    {
        public string returnID = "NO_ID_SELECTED";
        public bool lockChecked = false;

        private string serverName;
        private string databaseName;
        private string loginName;
        private string loginPassword;
        private string defaultMethodSearch;

        public MethodSelectionForm(string serverNameArg, string databaseNameArg, string loginNameArg, string loginPasswordArg, string defaultMethodSearchArg)
        {
            InitializeComponent();

            serverName = serverNameArg;
            databaseName = databaseNameArg;
            loginName = loginNameArg;
            loginPassword = loginPasswordArg;
            defaultMethodSearch = defaultMethodSearchArg;
        }

        private void MethodSelectionForm_Load(object sender, EventArgs e)
        {
            codeQryBox.Text = "*";
            if (!string.IsNullOrEmpty(defaultMethodSearch) && defaultMethodSearch != "NO_DEFAULT_MN_SEARCH_PROVIDED_IN_CONFIG")
            {
                nameQryBox.Text = defaultMethodSearch;
            }
        }

        private void searchButton_Click(object sender, EventArgs e)
        {
            executeSearch();
        }

        private void openButton_Click(object sender, EventArgs e)
        {
            this.returnID = (string)((DataRowView)resultGrid.SelectedRows[0].DataBoundItem)["id"];
            this.lockChecked = this.lockCheck.Checked;
            if (string.IsNullOrEmpty(returnID) ||  returnID == "NO_ID_SELECTED")
            {
                MessageBox.Show("A valid method must be selected to open.", "Valid Method Required", MessageBoxButtons.OK);
                return;
            }
            else
            {
                this.DialogResult = DialogResult.OK;
            }
            this.Close();
        }

        private void nameQryBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                executeSearch();
            }
        }

        private void codeQryBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                executeSearch();
            }
        }

        private void executeSearch()
        {
            //Connect to Aras Server
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
            iQry.setAction("get");
            iQry.setPropertyCondition("name", "like");
            iQry.setPropertyCondition("method_code", "like");
            iQry.setProperty("name", nameQryBox.Text);
            iQry.setProperty("method_code", codeQryBox.Text);
            if (!string.IsNullOrEmpty(typeBox.Text) && typeBox.Text != "All")
            {
                iQry.setProperty("method_type", typeBox.Text);
            }
            iQry = iQry.apply();

            connection.Logout();

            if (iQry.isError())
            {
                return;
            }

            var table = new DataTable("ResultTable");
            DataColumn nameCol = new DataColumn();
            nameCol.DataType = System.Type.GetType("System.String");
            nameCol.ColumnName = "Name";
            nameCol.ReadOnly = true;
            nameCol.Unique = true;

            DataColumn typeCol = new DataColumn();
            typeCol.DataType = System.Type.GetType("System.String");
            typeCol.ColumnName = "Type";
            typeCol.ReadOnly = true;
            typeCol.Unique = false;

            DataColumn codeCol = new DataColumn();
            codeCol.DataType = System.Type.GetType("System.String");
            codeCol.ColumnName = "Code";
            codeCol.ReadOnly = true;
            codeCol.Unique = false;

            DataColumn idCol = new DataColumn();
            idCol.DataType = System.Type.GetType("System.String");
            idCol.ColumnName = "id";
            idCol.ReadOnly = true;
            idCol.Unique = true;

            table.Columns.Add(nameCol);
            table.Columns.Add(typeCol);
            table.Columns.Add(codeCol);
            table.Columns.Add(idCol);

            DataColumn[] primaryCol = new DataColumn[1];
            primaryCol[0] = table.Columns["id"];
            table.PrimaryKey = primaryCol;

            for (int i = 0; i < iQry.getItemCount(); i++)
            {
                Item curr = iQry.getItemByIndex(i);
                DataRow row = table.NewRow();
                row["Name"] = curr.getProperty("name");
                row["Type"] = curr.getProperty("method_type");
                row["Code"] = curr.getProperty("method_code");
                row["id"] = curr.getID();
                table.Rows.Add(row);
            }

            resultGrid.DataSource = new BindingSource(table, null);
            resultGrid.Columns[0].Width = (resultGrid.Width / 3);
            resultGrid.Columns[1].Width = (resultGrid.Width / 6);
            resultGrid.Columns[2].Width = (resultGrid.Width / 2);
            resultGrid.Columns[3].Visible = false;
        }
    }
}
