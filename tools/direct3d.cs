/*
    Dreamview - Dreamfall model viewer
    Copyright (C) 2006, Tobias Pfaff (vertigo80@gmx.net)
    -------------------------------------------------------------------------- 
    This program is free software; you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation; either version 2 of the License, or
    (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with this program; if not, write to the Free Software
    Foundation, Inc., 51 Franklin St, Fifth Floor, Boston, MA  02110-1301 USA
    --------------------------------------------------------------------------
    I spent a lot of time interpreting the dreamfall file formats and developing
    this tool. Feel free to use this code/the procedures for your own projects;
    but do so under the terms of the GPL and if you use major parts of it please 
    refer me.
 */
using System;
using System.Collections.Generic;
using System.Windows.Forms;
using Microsoft.DirectX;
using Microsoft.DirectX.Direct3D;
using DreamView;
using System.Drawing;

namespace Tools
{
    class Direct3d
    {
        static Direct3d instance = null;

        bool doTimeStop=false;
        float lastTick=0, time=0;
        int adapter=0;
        bool fullscreen=false, pureHW=true, shading=true;
        Caps caps;
        Control ctrl;
        Format format;
        
        public int deviceAdapter { set { adapter = value; } get { return adapter; } }
        public bool timeStop { set { doTimeStop = value; } get { return doTimeStop; } }
        public bool useFullscreen { set { fullscreen = value; } get { return fullscreen; } }
        public bool usePureDevice { set { pureHW = value; } get { return pureHW; } }
        public bool useShading { set { shading = value; } get { return shading; } }
        public Caps deviceCaps { get { return caps; } }        
                
        public static Direct3d inst { get { if (instance == null) instance = new Direct3d(); return instance; } }

        public void setVertexShaderMatrix3(int reg, Matrix m)
        {
            Global.device.SetVertexShaderConstant(reg, new Vector4(m.M11, m.M12, m.M13, m.M14));
            Global.device.SetVertexShaderConstant(reg + 1, new Vector4(m.M21, m.M22, m.M23, m.M24));
            Global.device.SetVertexShaderConstant(reg + 2, new Vector4(m.M31, m.M32, m.M33, m.M34));
        }
        public void setVertexShaderMatrix3T(int reg, Matrix m)
        {
            Global.device.SetVertexShaderConstant(reg, new Vector4(m.M11, m.M21, m.M31, m.M41));
            Global.device.SetVertexShaderConstant(reg + 1, new Vector4(m.M12, m.M22, m.M32, m.M42));
            Global.device.SetVertexShaderConstant(reg + 2, new Vector4(m.M13, m.M23, m.M33, m.M43));
        }

        public static string[] getAdapters()
        {
            Log.write(1, "enumerating directx devices");            
            string[] adapters = new string[Manager.Adapters.Count];
            for (int i = 0; i < Manager.Adapters.Count; i++)
            {
                adapters[i] = Manager.Adapters[i].Information.Description;                
                Log.write(2, String.Format("adapter {0}: {1}",i,adapters[i]));
            }
            return adapters;
        }
        private PresentParameters setParameters(Control parent, bool windowed)
        {
            PresentParameters presentParams = new PresentParameters();
            presentParams.Windowed = windowed;
            presentParams.SwapEffect = SwapEffect.Discard;
            presentParams.EnableAutoDepthStencil = true;
            presentParams.PresentFlag = PresentFlag.DiscardDepthStencil;
            presentParams.BackBufferCount = 1;
            format = Manager.Adapters.Default.CurrentDisplayMode.Format;
            if (!windowed)
            {
                presentParams.BackBufferFormat = format;
                presentParams.BackBufferWidth = 800;
                presentParams.BackBufferHeight = 600;
                presentParams.FullScreenRefreshRateInHz = 0;
            }
            DeviceType type = Manager.CheckDeviceType(adapter, DeviceType.Hardware, format, format, !fullscreen) ? DeviceType.Hardware : DeviceType.Software;
            presentParams.AutoDepthStencilFormat = Manager.CheckDeviceFormat(adapter, type, format, Usage.DepthStencil, ResourceType.Surface, DepthFormat.D24X8) ? DepthFormat.D24X8 : DepthFormat.D16;
            presentParams.DeviceWindow = parent;
            return presentParams;
        }

        private void initializeGraphics(Control ctrl)
        {
            Log.write(0,String.Format("initialize graphics : adapter {0},fullscreen {1}, pure {2}, shading {3}",adapter,fullscreen,pureHW,shading));
            PresentParameters presentParams = setParameters(ctrl, !fullscreen);
            DeviceType type = Manager.CheckDeviceType(adapter, DeviceType.Hardware, format, format, !fullscreen) ? DeviceType.Hardware : DeviceType.Software;
            caps = Manager.GetDeviceCaps(adapter, type);
            CreateFlags flags = (pureHW && type == DeviceType.Hardware) ? (CreateFlags.HardwareVertexProcessing | CreateFlags.PureDevice) : CreateFlags.SoftwareVertexProcessing;

            Log.write(1, "graphic adapter info :");
            Log.write(1, Manager.Adapters[adapter].Information.Description);
            Log.write(1, Manager.Adapters[adapter].Information.DeviceName);
            Log.write(1, String.Format("type {0} format {1} stencil format {2} flags {3}", type, format, presentParams.AutoDepthStencilFormat, flags));
            Log.write(1, String.Format("vertex shader v{0}.{1}",caps.VertexShaderVersion.Major,caps.VertexShaderVersion.Minor));
            Log.write(1, String.Format("pixel shader v{0}.{1}",caps.PixelShaderVersion.Major,caps.PixelShaderVersion.Minor));

            Device.IsUsingEventHandlers = false;            
            Global.device = new Device(adapter, type, ctrl, flags, presentParams);
            Global.device.DeviceReset += new System.EventHandler(this.OnResetDevice);
            Global.device.DeviceLost += new System.EventHandler(this.OnDeviceLost);
            this.ctrl = ctrl;

            //hack
            //GraphicsStream gsv = ShaderLoader.FromFile("vs.scr", null, ShaderFlags.None);
            //GraphicsStream gsp = ShaderLoader.FromFile("ps.scr", null, ShaderFlags.None);
            //vs = new VertexShader(Global.device, gsv);
            //ps = new PixelShader(Global.device, gsp);
        }

        public void reset(Control ctrl)
        {
            if (Global.device == null)
                initializeGraphics(ctrl);
            setup();
        }

        public void switchToFullscreen(bool full,Control ctrl)
        {
            fullscreen = full;
            Global.device.Reset(setParameters(ctrl, !fullscreen));
        }
                
        private void setup()
        {
            ResourceStack.clear();
            MTexture.clearStore();
            MShader.clearStore();
            MShader.setDefaultRenderStates();

            Global.lastShader = null;
            Global.lastTexStage = null;            
            Global.passes = 0;

            correctSize();
            if (!Direct3d.inst.useShading)
            {
                Global.device.PixelShader = null;
                Global.device.VertexShader = null;
                Global.device.TextureState[0].ColorOperation = TextureOperation.Modulate;
                Global.device.TextureState[0].ColorArgument1 = TextureArgument.TextureColor;
                Global.device.TextureState[0].ColorArgument2 = TextureArgument.Diffuse;
                Global.device.TextureState[0].AlphaOperation = TextureOperation.Disable;
                Global.device.Transform.Projection = Global.proj;
                Global.passes = 1;                
            }
        }

        public void correctSize()
        {
            if (ctrl != null)
                Global.proj = Matrix.PerspectiveFovRH((float)Math.PI / 4.0f, (float)ctrl.Size.Width / (float)ctrl.Size.Height, 0.2f, 900.0f);            
        }

        private void OnResetDevice(object sender, EventArgs e)
        {
            Log.write(1, "device reset");
            setup();
            Scene.main.reset(false);
        }

        private void OnDeviceLost(object sender, EventArgs e)
        {
            ResourceStack.clear();
            MTexture.clearStore();
            MShader.clearStore(); 
        }

        public void unloadGraphics()
        {
            ResourceStack.clear();
            MTexture.clearStore();
            MShader.clearStore();
            if (Global.device != null)
                Global.device.Dispose();
            Global.device = null;
        }

        public void resetTime()
        {
            time = 0;
            lastTick = Environment.TickCount;
        }

        public void render(MFrame root)
        {
            if (Global.device == null)
                return;
            Global.device.Clear(ClearFlags.Target | ClearFlags.ZBuffer, System.Drawing.Color.Blue, 1.0f, 0);
            Global.device.BeginScene();

            if (!useShading)
                Global.device.Transform.View = Global.view;
            if (root != null)
            {
                float curTime = Environment.TickCount;
                if (!doTimeStop)
                    time += (curTime - lastTick) / Global.animPeriod;
                if (time < 0) time = 0;
                if (time > 100) time -= 100;                
                lastTick = curTime;

                root.updateMatrices(Matrix.Identity, time);
                for (int pass = 0; pass < Global.passes; pass++)
                    root.render(pass, 0, time);
                for (int pass = 0; pass < Global.passes; pass++)
                    root.render(pass, 1, time);                
            }
            Global.device.EndScene();
            Global.device.Present();
        }           
    }
    
}
