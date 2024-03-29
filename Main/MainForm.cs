using System;
using System.IO;
using System.Collections.Generic;
using System.ComponentModel;
using Tools;
using Microsoft.DirectX;
using System.Text;
using System.Windows.Forms;
using System.Drawing;

namespace DreamView
{
    
    public partial class MainForm : Form
    {
        bool pause = true;
        float angleCam1 = 0, angleCam2 = 0, angleObj1 = 0, angleObj2 = 0,angleObj3=0;
        Vector3 posUp = new Vector3(0, 0, 1);
        Vector3 posCam, posLook, posLook0, posRight;
        Vector3 center;
        bool mouseCtrl = false, resizing = false;
        int lastX = 0, lastY = 0;
        int lasttick = 0;
        int frames = 0;

        public MainForm()
        {
            InitializeComponent();
            this.Text = "Dreamview v" + Tools.Global.version;            
        }

        private void MainForm_Resize(object sender, EventArgs e)
        {
            pause = ((this.WindowState == FormWindowState.Minimized) || !this.Visible);
            Direct3d.inst.correctSize();
        }

        public void update()
        {
            posLook = posLook0;
            posLook.TransformCoordinate(Matrix.RotationZ(angleCam1));
            posRight = Vector3.Cross(posLook, posUp);
            posLook.TransformCoordinate(Matrix.RotationAxis(-posRight,angleCam2));
            
            Global.view = Matrix.Translation(-center) * Matrix.RotationYawPitchRoll(angleObj1,angleObj2,angleObj3) * Matrix.LookAtRH(posCam, posCam + posLook, posUp);
            Global.projview = Global.view * Global.proj;

            Global.lightPos = Vector3.Scale(new Vector3((float)Math.Cos(Environment.TickCount / 250.0f), 1.0f, (float)Math.Sin(Environment.TickCount / 250.0f)),4);
                        
            if (frames > 20)
            {
                float fps = 1000.0f * (float)frames / ((float)Environment.TickCount - lasttick);
                lasttick = Environment.TickCount;
                frames = 0;
                this.Text = String.Format("({0:f2},{1:f2},{2:f2}) {3:f2} fps", posCam.X,posCam.Y,posCam.Z, fps);                
            }
            else
                frames++;
        }

        public void renderPanel_MouseMove(object sender, MouseEventArgs e)
        {
            if (mouseCtrl)
            {
                float dx = (lastX - e.X) * 0.004f;
                float dy = (lastY - e.Y) * 0.004f;
                lastX = e.X;
                lastY = e.Y;
                angleCam1 += dx;
                angleCam2 -= dy;
            }
            //base.OnMouseMove(e);
        }

        public void renderPanel_MouseUp(object sender, MouseEventArgs e)
        {
            angleCam1 = angleCam1 % (2 * (float)Math.PI);
            angleCam2 = angleCam2 % (2 * (float)Math.PI);
            mouseCtrl = false;
            base.OnMouseUp(e);
        }

        public void renderPanel_MouseDown(object sender, MouseEventArgs e)
        {
            mouseCtrl = true;
            lastX = e.X;
            lastY = e.Y;
            base.OnMouseDown(e);
        }
        
        public void MainForm_KeyDown(object sender, KeyEventArgs e)
        {
            float speed = 0.6f, rotSpeed = 0.07f,peed=0.25f;
            if (e.Shift)
                speed *= 10.0f;
            if (e.Control)
                speed /= 10.0f;
            e.Handled = true;
            e.SuppressKeyPress = true;
            switch (e.KeyCode)
            {
                case Keys.D1:
                    Global.itest--; break;
                case Keys.D2:
                    Global.itest++; break;
                case Keys.R:
                    Global.vtest.X += peed; break;
                case Keys.T:
                    Global.vtest.Y += peed; break;
                case Keys.Y:
                    Global.vtest.Z += peed; break;
                case Keys.F:
                    Global.vtest.X -= peed; break;
                case Keys.G:
                    Global.vtest.Y -= peed; break;
                case Keys.H:
                    Global.vtest.Z -= peed; break;
                case Keys.Q:
                    angleObj1 = (angleObj1 + rotSpeed) % (2.0f * (float)Math.PI); break;
                case Keys.A:
                    angleObj1 = (angleObj1 - rotSpeed) % (2.0f * (float)Math.PI); break;
                case Keys.W:
                    angleObj2 = (angleObj2 + rotSpeed) % (2.0f*(float)Math.PI); break;
                case Keys.S:
                    angleObj2 = (angleObj2 - rotSpeed) % (2.0f*(float)Math.PI); break;
                case Keys.E:
                    angleObj3 = (angleObj3 + rotSpeed) % (2.0f * (float)Math.PI); break;
                case Keys.D:
                    angleObj3 = (angleObj3 - rotSpeed) % (2.0f * (float)Math.PI); break;
                case Keys.Up:
                    posCam += speed * posLook; break;
                case Keys.Down:
                    posCam -= speed * posLook; break;
                case Keys.Left:
                    posCam -= speed * posRight; break;
                case Keys.Right:
                    posCam += speed * posRight; break;
                case Keys.PageUp:
                    posCam -= speed * Vector3.Cross(posLook, posRight); break;
                case Keys.PageDown:
                    posCam += speed * Vector3.Cross(posLook, posRight); break;
                case Keys.Escape:
                    if (Direct3d.inst.useFullscreen)
                    {
                        FormBorderStyle = FormBorderStyle.Sizable;
                        Direct3d.inst.switchToFullscreen(false, renderPanel);
                        split.Panel1Collapsed = false;
                        upperPanel.Enabled = true;
                        this.Invalidate();
                    }
                    else
                    {
                        pause = true;
                        this.Close();
                    }
                    break;
                case Keys.Subtract:
                    Global.test -= 0.005f;
                    if (Global.test < 0) Global.test = 0; break;
                case Keys.Add:
                    Global.test += 0.005f;
                    if (Global.test > 1) Global.test = 1; break;
            }
        }

        private void renderPanel_Paint(object sender, PaintEventArgs e)
        {             
            if (!pause && Scene.main.isReady && !resizing)
            {
                try
                {
                    update();
                    Scene.main.display();
                }
                catch (Exception x) { pause = true; Log.error(x); Environment.Exit(1); }
                
                renderPanel.Invalidate();
            }
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            System.Threading.Thread.CurrentThread.CurrentCulture = 
                new System.Globalization.CultureInfo("en-US", false);            
            setupTree(false);
            pause = false;
        }

        private void setupTree(bool reindex)
        {
            try
            {
                tree.BeginUpdate();
                tree.Nodes.Clear();
                if (Global.pakPath == null || Global.pakPath == "" || !System.IO.Directory.Exists(Global.pakPath))
                    tree.Nodes.Add("0", "Pak-file path invalid");
                else
                    Parser.Scene.loadSceneTree(tree.Nodes, "scene.idx", reindex);
                tree.EndUpdate();
            }
            catch (Exception e) { pause = true; Log.error(e); Environment.Exit(1); }
        }

        public void hideTab()
        {
            split.Panel1Collapsed = true;
            btShowHide.Text = "Show tab";
        }
        public void loadAnim(string name)
        {
            Direct3d.inst.resetTime();
            if (!Scene.main.isReady || !Scene.main.loadAnim(name))
                this.Text = "please load an object to animate first";
        }

        public void loadScene(string[] obj, string bundle, bool singleAnim)
        {
            #if !DEBUG
            try
            #endif
            {
                pause = true;
                this.Text = "loading scene... please wait";
                Application.DoEvents();
                Direct3d.inst.reset(renderPanel);
                Scene.main.loadBundle(bundle, false);
                foreach (string name in obj)
                    Scene.main.addScene(name);
                Scene.main.cleanup();                
                if (Scene.main.isReady)
                    this.Text = "scene loaded";
                else
                    this.Text = "scene does not contain visual elements";
                ((MPanel)renderPanel).setBackground(Scene.main.isReady);
                Global.singleAnim = singleAnim;
                renderPanel.Invalidate();
                pause = !Scene.main.isReady;
                angleCam1 = angleCam2 = angleObj2 = angleObj1 = angleObj3 = 0;
                center = Scene.main.centerPos;
                posCam = new Vector3(0, 3, 0);
                posLook0 = Vector3.Normalize(-posCam);
            }
            #if !DEBUG
            catch (Exception e) { pause = true; Log.error(e); Environment.Exit(1); }
            #endif
        }
        
        private void tree_DoubleClick_1(object sender, EventArgs e)
        {         
            TreeNode node = tree.SelectedNode;
            if (node != null && node.Tag != null)
            {
                string obj = (string)node.Tag;
                if (node.Level == 2)
                    loadAnim(obj);
                else if (node.Level == 1)
                {
                    List<string> objs=new List<string>();
                    if (obj == "all")
                    {
                        foreach (TreeNode sub in node.Parent.Nodes)
                            if ((string)sub.Tag != "all" && ((string)sub.Tag).StartsWith("art/locations"))
                                objs.Add((string)sub.Tag);
                    }
                    else
                        objs.Add(obj);
                    loadScene(objs.ToArray(), node.Parent.Text, node.Nodes.Count > 0);
                }
            }
        }

        private void MainForm_ResizeBegin(object sender, EventArgs e)
        {
            resizing = true;
        }
        private void MainForm_ResizeEnd(object sender, EventArgs e)
        {
            resizing = false;
            renderPanel.Invalidate();
        }

        private void btShowHide_Click(object sender, EventArgs e)
        {
            split.Panel1Collapsed = !split.Panel1Collapsed;
            btShowHide.Text = split.Panel1Collapsed ? "Hide tab" : "Show tab";
        }

        private void btOptions_Click(object sender, EventArgs e)
        {
            pause = true;
            string oldPak = Global.pakPath;
            using (Settings frmSettings = new Settings())
                frmSettings.ShowDialog();
            Direct3d.inst.unloadGraphics();
            Direct3d.inst.reset(renderPanel);
            Scene.main.reset(false);
            if (Global.pakPath != oldPak)
                setupTree(true);            
            pause = false;                            
        }

        private void btFullscreen_Click(object sender, EventArgs e)
        {
            if (Scene.main.isReady)
            {
                split.Panel1Collapsed = true;
                upperPanel.Enabled = false;
                FormBorderStyle = FormBorderStyle.None;
                Direct3d.inst.switchToFullscreen(true, this);
            }
        }

        private void btExport_Click(object sender, EventArgs e)
        {
            #if !DEBUG
            try
            #endif
            {
                if (Scene.main.isReady)
                {
                    string name = null;
                    int type = -1;
                    using (SaveFileDialog dlg = new SaveFileDialog())
                    {
                        dlg.Title = "Save scene file";
                        dlg.OverwritePrompt = false;
                        dlg.Filter = Exporter.BaseExporter.filter;
                        if (dlg.ShowDialog() == DialogResult.OK)
                        {
                            name = dlg.FileName;
                            type = dlg.FilterIndex - 1;                            
                        }
                    }
                    if (type != -1)
                    {
                        bool cancel = false;
                        using (MeshSelect frmSel = new MeshSelect(Scene.main.frameRoot))
                        {
                            cancel = frmSel.ShowDialog() == DialogResult.Cancel;
                        }
                        if (!cancel)
                        {
                            this.Text = "Exporting...";
                            Scene.main.export(name, type);
                        }
                    }
                }
            }
            #if !DEBUG
            catch (Exception x) { pause = true; Log.error(x); Environment.Exit(1); }
            #endif
        }

        private void btImport_Click(object sender, EventArgs e)
        {
            #if !DEBUG
            try
            #endif
            {
                if (Scene.main.isReady)
                {
                    string name = null;
                    int type = -1;
                    using (OpenFileDialog dlg = new OpenFileDialog())
                    {
                        dlg.Title = "Re-Import scene file";
                        dlg.Filter = Importer.BaseImporter.filter;
                        if (dlg.ShowDialog() == DialogResult.OK)
                        {
                            name = dlg.FileName;
                            type = dlg.FilterIndex - 1;
                        }
                    }
                    if (type != -1)
                    {                        
                        this.Text = "Reimporting...";
                        Scene.main.reimport(name, type);
                    }
                }
            }
            #if !DEBUG
            catch (Exception x) { pause = true; Log.error(x); Environment.Exit(1); }
            #endif
        }

        private void mButton1_Click(object sender, EventArgs e)
        {
            this.Text = "Re-Extracting bundle from pak file...";
            Scene.main.revertPak();
        }

        private void btInject_Click(object sender, EventArgs e)
        {
            Text = "Injecting current bundle into pak file...";
            Scene.main.inject();
        }
    }
}