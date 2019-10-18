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
using System.IO;
using Microsoft.DirectX.Direct3D;
using Microsoft.DirectX;
using Tools;

namespace DreamView
{
    struct MTracking
    {
        public enum MType { Identity = 0, View, Proj, ProjView, Cam,Tex };
        public enum MFlag { None = 0, Inverted, Transposed };

        public int adr,count;
        public MType type;
        public MFlag flag;
    }
    struct MRenderState
    {
        public RenderStates state;
        public int value;
    }
    struct MTmuMap
    {
        public int level;
        public int minFilter, magFilter, mipFilter;
        public int wrapU, wrapV;
        public float scrollSpeed;
        public int scrollU,scrollV,scrollSlot;
    }
    class MShaderEntry
    {
        public MShaderProg prog;
        public string name;
        public MTracking[] tracking = null;
        public MTmuMap[] tmu = null;
        public MRenderState[] states = null;
        public int[] tmuMap = null;
        public int order=0;
        public int blitz = 0;
        public Vector4 diffuse=Vector4.Empty;
        public Vector4 specular = Vector4.Empty;
        public int lightAdress, lightCount, cubeAdr, cubeAdrRec;
    }
    class MShaderProg
    {
        public VertexShader vs;
        public PixelShader ps;
        public string name;
    }
    
    class MShader
    {
        static Dictionary<string, MShaderEntry[]> store = new Dictionary<string, MShaderEntry[]>();
        public static Dictionary<string, MShaderProg> progStore = new Dictionary<string, MShaderProg>();
        MShaderEntry[] entry;

        public int passes { get { return (entry != null) ? entry.Length : 0; } }
        public int blitzMode { get { return (passes > 0) ? entry[0].blitz : 0; } }
        public int getOrder(int pass) { return (pass < passes) ? entry[pass].order : -1; }
        public int cubeAdr(int pass) { return (pass < passes) ? entry[pass].cubeAdr : -1; }
        public int cubeAdrRec(int pass) { return (pass < passes) ? entry[pass].cubeAdrRec : -1; }
        
        public MShader(string name)
        {
            if (name == null)
                entry = null;
            else if (store.ContainsKey(name))
                entry = store[name];            
            else
            {
                Parser.Shader parse = new Parser.Shader(name);
                parse.start();
                entry = parse.result;
                store.Add(name, entry);
                if (entry != null && entry.Length > Global.passes)
                    Global.passes = entry.Length;
            }
        }
        public static void clearStore()
        {
            store.Clear();
            progStore.Clear();
        }
        
        public int[] prepare(Matrix world, int pass, float time)
        {
            if (!Direct3d.inst.useShading)
            {
                Global.device.Transform.World = world;
                return new int[] { 1 };
            }

            if (entry != null && entry.Length > pass && entry[pass] != null)
            {
                if (Global.lastShader != entry[pass])
                {
                    if (Global.lastShader == null || Global.lastShader.prog != entry[pass].prog)
                    {
                        Global.device.VertexShader = entry[pass].prog.vs;
                        Global.device.PixelShader = entry[pass].prog.ps;
                    }
                    foreach (MRenderState rs in entry[pass].states)
                    {
                        if (Global.lastShader != null && Global.lastShader.states != null)
                            foreach (MRenderState oldRs in Global.lastShader.states)
                                if (oldRs.state == rs.state && oldRs.value == rs.value)
                                    continue;
                        Global.device.SetRenderState(rs.state, rs.value);                         
                    }
                    for (int i = 0; i < entry[pass].tmu.Length; i++)
                    {
                        MTmuMap map = entry[pass].tmu[i];
                        Global.device.SetSamplerState(i, SamplerStageStates.MinFilter, map.minFilter);
                        Global.device.SetSamplerState(i, SamplerStageStates.MagFilter, map.magFilter);
                        Global.device.SetSamplerState(i, SamplerStageStates.MipFilter, map.mipFilter);                        
                    }
                    // global shader const
                    Global.device.SetVertexShaderConstant(39, new Vector4(16,0,0,0));
                    Global.device.SetVertexShaderConstant(40, new Vector4(0.04f,0.06f,0.01f,0));

                    if (entry[pass].lightCount != 0)
                    {
                        Vector3 lp = Vector3.TransformCoordinate(Global.lightPos, Global.view);
                        Global.device.SetVertexShaderConstant(entry[pass].lightAdress + 2, new Vector4(lp.X,lp.Y,lp.Z,1));
                        Global.device.SetVertexShaderConstant(entry[pass].lightAdress + 5, entry[pass].diffuse);
                        Global.device.SetVertexShaderConstant(entry[pass].lightAdress + 6, entry[pass].specular);
                        Global.device.SetVertexShaderConstant(entry[pass].lightAdress + 7, new Vector4(1, 1, 0.4f, 1));
                    }
                    else if (entry[pass].lightAdress !=0)
                        Global.device.SetVertexShaderConstant(entry[pass].lightAdress + 7, Vector4.Empty);

                    Global.lastShader = entry[pass];
                }

                Matrix view = world * Global.view;
                Matrix projview = world * Global.projview;
                foreach (MTracking track in entry[pass].tracking)
                {
                    if (track.type == MTracking.MType.Tex)
                    {                            
                        for (int i = 0; i < track.count; i++)
                        {
                            if (i < entry[pass].tmu.Length)
                            {
                                MTmuMap map = entry[pass].tmu[i];                            
                                float st = (time * map.scrollSpeed) % 1;
                                Global.device.SetVertexShaderConstant(track.adr + 2 * i, new Vector4(1, 0, 0, (float)map.scrollU * st));
                                Global.device.SetVertexShaderConstant(track.adr + 2 * i + 1, new Vector4(0, 1, 0, (float)map.scrollV * st));
                            }
                            else
                            {
                                Global.device.SetVertexShaderConstant(track.adr + 2 * i, new Vector4(1, 0, 0, 0));
                                Global.device.SetVertexShaderConstant(track.adr + 2 * i + 1, new Vector4(0, 1, 0, 0));                                
                            }
                        }
                    }
                    else
                    {
                        Matrix mat = Matrix.Identity;
                        switch (track.type)
                        {
                            case MTracking.MType.Cam: mat = Matrix.Invert(Global.view); break;
                            case MTracking.MType.View: mat = view; break;
                            case MTracking.MType.ProjView: mat = projview; break;
                            case MTracking.MType.Proj: mat = Global.proj; break;
                        }
                        switch (track.flag)
                        {
                            case MTracking.MFlag.Inverted: mat = Matrix.Invert(mat); break;
                        }
                        mat = Matrix.TransposeMatrix(mat);
                        Global.device.SetVertexShaderConstant(track.adr, mat);
                    }
                }                
                return entry[pass].tmuMap;
            }
            return null;
        }

        public static void setDefaultRenderStates()
        {
            Global.device.RenderState.CullMode = Cull.Clockwise;
            Global.device.RenderState.ZBufferEnable = true;
            Global.device.RenderState.UseWBuffer = Direct3d.inst.deviceCaps.RasterCaps.SupportsWBuffer;
            Global.device.RenderState.FogTableMode = FogMode.Linear;
            Global.device.RenderState.FogColor = System.Drawing.Color.White;
            Global.device.RenderState.FogStart = 50.0f;
            Global.device.RenderState.FogEnd = 100.0f;
            Global.device.RenderState.Lighting = false;                
        }
    }    
}
