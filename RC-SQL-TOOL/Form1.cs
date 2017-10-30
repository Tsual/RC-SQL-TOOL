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
        public static Form1 ObjectReference { get; private set; }

        string[] strFileNames = null;
        public Form1()
        {
            InitializeComponent();
            Load += Form1_Load;
            ObjectReference = this;
        }

        private delegate void invokeDelegate();
        public void SendInfo(string info)
        {
            invokeDelegate del = () => { listView1.Items.Add(info); };
            Invoke(del);
        }



        private void Form1_Load(object sender, EventArgs e)
        {
            SqlConfig.StartFileWatching();
            try
            {
                SqlConfig.InitSqlConfig(SqlConfig.SqlConfigInitMode.Default);
                SendInfo("Config Read Complete");
            }
            catch (Exception ex)
            {
                SendInfo(ex.Message);
            }
            
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
            OpenFileDialog ofd = new OpenFileDialog
            {
                ValidateNames = true,
                CheckPathExists = true,
                CheckFileExists = true,
                Multiselect = true
            };
            if (ofd.ShowDialog() == DialogResult.OK)
                strFileNames = ofd.FileNames;
            if (strFileNames?.Length > 0)
                SendInfo("Files Count:" + strFileNames.Length);
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
                if(sPath=="")
                {
                    SendInfo("folder select none");
                    return;
                }
                SqlExcuter.Excute(strFileNames, sPath);
                System.Diagnostics.ProcessStartInfo psi = new System.Diagnostics.ProcessStartInfo("Explorer.exe")
                {
                    Arguments = "/e,/select," + sPath + @"\Insert\"
                };
                System.Diagnostics.Process.Start(psi);
                SqlConfig.SaveConfig();
            }
            catch (Exception ex)
            {
                SendInfo(ex.Message);
            }
            
            /*
            string sPath = "";
            FolderBrowserDialog folder = new FolderBrowserDialog();
            folder.Description = "选择所有文件存放目录";
            if (folder.ShowDialog() == DialogResult.OK)
                sPath = folder.SelectedPath;
            if (sPath == "")
            {
                SendInfo("folder select none");
                return;
            }
            SqlExcuter.Excute(strFileNames, sPath);
            System.Diagnostics.ProcessStartInfo psi = new System.Diagnostics.ProcessStartInfo("Explorer.exe")
            {
                Arguments = "/e,/select," + sPath + @"\Insert\"
            };
            System.Diagnostics.Process.Start(psi);
            SqlConfig.SaveConfig();
            */
        }
    }
}
