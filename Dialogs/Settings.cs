using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using Tools;

namespace DreamView
{
    public partial class Settings : Form
    {        
        public Settings()
        {
            InitializeComponent();
            chkHW.Checked = Direct3d.inst.usePureDevice;
            chkshading.Checked = Direct3d.inst.useShading;
            numVerbose.Value = Log.verbose;
            listDevice.Items.AddRange(Direct3d.getAdapters());
            listDevice.SelectedIndex = 0;
            textPath.Text = Tools.Global.pakPath;
            chkRefLighting.Checked = Global.useReferenceBox;
        }
        private void numVerbose_ValueChanged(object sender, EventArgs e)
        {
            Log.verbose = (int)numVerbose.Value;
        }
        
        private void chkshading_CheckedChanged(object sender, EventArgs e)
        {
            Direct3d.inst.useShading = chkshading.Checked;
        }

        private void chkHW_CheckedChanged(object sender, EventArgs e)
        {
            Direct3d.inst.usePureDevice = chkHW.Checked;
        }

        private void listDevice_SelectedIndexChanged(object sender, EventArgs e)
        {
            Direct3d.inst.deviceAdapter = Microsoft.DirectX.Direct3D.Manager.Adapters[listDevice.SelectedIndex].Adapter;
        }

        private void textPath_TextChanged(object sender, EventArgs e)
        {
            string path = textPath.Text;
            if (path.Length > 1 && path[path.Length - 1] != '\\')
                path += '\\';
            Tools.Global.pakPath = path;            
        }

        private void Settings_Load(object sender, EventArgs e)
        {
            labelVersion.Text += Tools.Global.version;
        }

        private void btOK_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void chkRefLighting_CheckedChanged(object sender, EventArgs e)
        {
            Global.useReferenceBox = chkRefLighting.Checked;
        }
    }
}