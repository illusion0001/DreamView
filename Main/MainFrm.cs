using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Text;
using System.Windows.Forms;
using Microsoft.DirectX;
using Microsoft.DirectX.Direct3D;
using Direct3D = Microsoft.DirectX.Direct3D;
using Tools;

namespace DreamView
{
    public partial class MainForm : Form
    {
        bool pause = true;
        float turn1=(float)Math.PI/2;
        float turn2=0;
        Vector3 posUp = new Vector3(0, 0, 1);
        Vector3 posCam = new Vector3(-2, 0, 0);
        Vector3 posLook;
        bool mouseCtrl = false;
        int lastX=0, lastY=0;
        int lasttick=0;
        int frames=0;

        public bool paused { get { return pause; } set { pause = value; } }

        public MainForm()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.Opaque, true);
            InitializeComponent();            
        }
        
        public void update()
        {
            posLook = new Vector3(1, 0, 0);        
            posLook.TransformCoordinate(Matrix.RotationY(turn2));
            posLook.TransformCoordinate(Matrix.RotationZ(turn1));
                        
            Global.view = Matrix.LookAtRH(posCam, posCam + posLook, posUp);
            Global.projview = Global.view * Global.proj;
            
            Vector4 light = new Vector4((float)Math.Cos(Environment.TickCount / 250.0f), 1.0f, (float)Math.Sin(Environment.TickCount / 250.0f),1);
            light.Scale(4.0f);
            light.W = 1;

            Vector4 ltrans =Vector4.Transform(light, Global.view);            
            Global.device.SetVertexShaderConstant(42, ltrans); // light pos

            if (frames > 2)
            {
                float fps = 1000.0f * (float)frames / ((float)Environment.TickCount - lasttick);
                lasttick = Environment.TickCount;
                frames = 0;
                //this.Text = String.Format("({0:f2},{1:f2},{2:f2}) {3:f2} fps", posCam.X,posCam.Y,posCam.Z, fps);
                float time = ((float)Environment.TickCount % Global.animPeriod) / Global.animPeriod;                    
                this.Text = string.Format("{0}/{1} tset{2} * {4} time {3:f5} status {5}", Global.test2 % 4, Global.test2 /4, Global.test1, Global.test,Global.test3,Global.status );
            }
            else
                frames++;
        }
         
        protected override void OnPaint(System.Windows.Forms.PaintEventArgs e)
        {
            //Render(); // Render on painting            
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            if (mouseCtrl)
            {
                float dx = (lastX - e.X) * 0.004f;
                float dy = (lastY - e.Y) * 0.004f;
                lastX = e.X;
                lastY = e.Y;
                turn1 += dx;
                turn2 -= dy;
            }
            base.OnMouseMove(e);
        }
        protected override void OnMouseDown(MouseEventArgs e)
        {
            mouseCtrl = true;
            lastX = e.X;
            lastY = e.Y;
            base.OnMouseDown(e);
        }
        protected override void OnMouseUp(MouseEventArgs e)
        {
            turn1 = turn1 % (2 * (float)Math.PI);
            turn2 = turn2 % (2 * (float)Math.PI);
            mouseCtrl = false;
            base.OnMouseUp(e);
        }

        protected override void OnKeyDown(System.Windows.Forms.KeyEventArgs e)
        {
            float speed=0.6f;
            if (e.Shift)
                speed *= 10.0f;
            if (e.Control)
                speed /= 10.0f;

            switch (e.KeyCode)
            {
                case Keys.Up:
                    posCam += speed * posLook;
                    break;
                case Keys.Down:
                    posCam -= speed * posLook;
                    break;
                case Keys.Left:
                    posCam -= speed * Vector3.Cross(posLook,posUp);
                    break;
                case Keys.Right:
                    posCam += speed * Vector3.Cross(posLook,posUp);
                    break;
                case Keys.Escape:
                    pause = true;
                    this.Close();
                    break;
                case Keys.Subtract:
                    Global.test -= 0.005f;
                    if (Global.test < 0) Global.test = 0;
                    break;
                case Keys.Add:
                    Global.test += 0.005f;
                    if (Global.test > 1) Global.test = 1;
                    break;
            }
        }
        protected override void OnKeyPress(KeyPressEventArgs e)
        {
            //if (e.KeyChar == 'a')
              //  App.inst.attachSir("art/npc/japan/japanese_girl/japan_npc_girl.sir", Matrix.Translation(new Vector3(8.5f, 17.5f, -6.5f)));
            if (e.KeyChar == '1')
                Global.test1 = (Global.test1 + 1) % 4;
            if (e.KeyChar == '2')
                Global.test2 = (Global.test2 + 1) % 24;
            if (e.KeyChar == 'w')
            { Global.test2--; if (Global.test2 < 0) Global.test2 = 23; }
            if (e.KeyChar == '3')
                Global.test3 = (Global.test3 + 1) % 4;
            if (e.KeyChar == 'e')
            { Global.test3--; if (Global.test3 < 0) Global.test3 = 3; }
            
            base.OnKeyPress(e);
        }
        protected override void OnResize(System.EventArgs e)
        {
            pause = ((this.WindowState == FormWindowState.Minimized) || !this.Visible);
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            
        }
                
    }
}