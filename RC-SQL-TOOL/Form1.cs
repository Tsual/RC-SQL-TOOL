using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace RC_SQL_TOOL
{
    public partial class Form1 : Form
    {
        string[] strFileNames = null;
        public Form1()
        {
            SqlConfig.initSqlConfig();
            InitializeComponent();
        }

        private void label2_Click(object sender, EventArgs e)
        {

        }

        private void button3_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start(Application.StartupPath + "\\config.xml");
        }

        private void button1_Click(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.ValidateNames = true;
            ofd.CheckPathExists = true;
            ofd.CheckFileExists = true;
            ofd.Multiselect = true;
            if (ofd.ShowDialog() == DialogResult.OK)
                strFileNames = ofd.FileNames;
            if (strFileNames.Length > 0)
                label1.Text = "" + strFileNames.Length;
        }

        private void button2_Click(object sender, EventArgs e)
        {
            try
            {
                string sPath = "";
                FolderBrowserDialog folder = new FolderBrowserDialog();
                folder.Description = "选择所有文件存放目录";
                if (folder.ShowDialog() == DialogResult.OK)
                    sPath = folder.SelectedPath;
                SqlConfig.initSqlConfig();
                SqlExcuter.Excute(strFileNames, sPath);

                System.Diagnostics.ProcessStartInfo psi = new System.Diagnostics.ProcessStartInfo("Explorer.exe")
                {
                    Arguments = "/e,/select," + sPath + @"\Insert\"
                };
                System.Diagnostics.Process.Start(psi);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }

        }
    }
}
