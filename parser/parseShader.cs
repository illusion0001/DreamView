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
using Tools;
using Microsoft.DirectX;
using Microsoft.DirectX.Direct3D;
using SharkFile = Parser.SharkFile;
using SNode = Parser.SNode;
using DreamView;

namespace Parser
{
    class Shader
    {
        string name;
        List<MShaderEntry> entries;
        
        public MShaderEntry[] result { get { return (entries.Count>0) ? entries.ToArray() : null; } }

        public Shader(string name)
        {
            this.name = name;            
            entries = new List<MShaderEntry>();
        }

        // functions for loading .sdr
        
        // return blitzmode
        public void start()
        {
            Log.write(1, "loading shader " + name);
            string file = name.Split('#')[0];
            string detail = name.Split('#')[1];

            SharkFile sdr = new SharkFile(file);
            SNode shaders = sdr.root.gosub("shaders");            
            for (int i=0;i<shaders.count;i++)
            {
                SNode sub = (SNode)shaders[i];
                if (sub != null && (string)sub["name"] == detail)
                    checkType(sub);
            }
            if (entries.Count == 0)
            {
                Log.write(1, "loading shader " + name + " failed");            
            }            
        }
        private void checkType(SNode sub)
        {
            string type = (string)sub["type"];
            if (type==null) type = (string)sub["child_type"];

            if (type == "eng_shader_special.shaderfactory|billboard")
                loadShaderBillboard(sub);
            if (type == "eng_shader_std.shaderfactory|alternative")
                loadShaderAlternative(sub);
            if (type == "eng_shader_std.shaderfactory|basic" || type == "eng_shader_funcom.shaderfactory|basic")
                loadShaderBasic(sub);
            if (type == "eng_shader_std.shaderfactory|variants")
                loadShaderVariants(sub);
            if (type == "eng_shader_shvol.shaderfactory|simple")
                loadShadowVol(sub);           
            
        }
        private void loadShadowVol(SNode sdr)
        {                    
            SNode children = sdr.gosub("param");
            if (children != null) checkType(children);
        }
        private void loadShaderVariants(SNode sdr)
        {
            
            SNode children = sdr.gosub("param/children");
            if (children == null) children = sdr.gosub("child_param/children");
            if (children == null) return;
            for (int i = 0; i < children.count; i++)
            {
                if (((string)((SNode)children[i])["cap_exp"] == "cap:d3d.pixprog.3x0" && (Direct3d.inst.deviceCaps.PixelShaderVersion.Major < 3))
                    || ((string)((SNode)children[i])["cap_exp"] == "cap:d3d.pixprog.2x0" && (Direct3d.inst.deviceCaps.PixelShaderVersion.Major < 2)))
                    continue;
                checkType((SNode)children[i]);
                return;
            }
        }
        private void loadShaderAlternative(SNode sdr)
        {
            SNode children = sdr.gosub("param/children");
            if (children == null) children = sdr.gosub("child_param/children");
            if (children == null) return;
            for (int i = 0; i < children.count; i++)
            {
                if ((string)(((SNode)children[i])["plat_pat"]) == "xbox")
                    continue;
                checkType((SNode)children[i]);
                return;
            }
        }
        private void loadShaderBillboard(SNode sdr)
        {
            SNode children = sdr.gosub("param");
            if (children == null) children = sdr.gosub("child_param");
            if (children == null || children.count == 0) return;
            checkType((SNode)children[0]);
        }
        private void loadShaderBasic(SNode sdr)
        {
            SNode passes = sdr.gosub("param/passes");
            if (passes == null) passes = sdr.gosub("child_param/passes");
            if (passes == null) return;
            int blitz = (int)sdr.get<long>("param/use_blitz", 0);
            blitz += 2*(int)sdr.get<long>("param/blitz_box", 0);
            for (int i = 0; i < passes.count; i++)
            {
                loadPass((SNode)passes[i], blitz);
            }
        }
        private void loadPass(SNode sdr, int blitz)
        {
            MShaderEntry entry = new MShaderEntry();
            string[] blendnames = new string[] {"one","zero","src_alpha","inv_src_alpha","dest_alpha","inv_dest_alpha"};
            Blend[] blendvalues = new Blend[] {Blend.One,Blend.Zero,Blend.SourceAlpha,Blend.InvSourceAlpha,Blend.DestinationAlpha, Blend.InvDestinationAlpha };
            entry.states = new MRenderState[10];
            entry.states[0].state = RenderStates.CullMode;
            entry.states[0].value = (int)sdr.parse<Cull>("cull_mode", new string[] { "none","back" }, new Cull[] { Cull.None,Cull.Clockwise }, Cull.Clockwise);
            entry.states[1].state = RenderStates.SourceBlend;
            entry.states[1].value = (int)sdr.parse<Blend>("blend_mode_src", blendnames, blendvalues, Blend.One);
            entry.states[2].state = RenderStates.DestinationBlend;
            entry.states[2].value = (int)sdr.parse<Blend>("blend_mode_dest", blendnames, blendvalues, Blend.Zero);
            entry.states[3].state = RenderStates.AlphaBlendEnable;
            entry.states[3].value = ((entry.states[1].value != (int)Blend.One) || (entry.states[2].value != (int)Blend.Zero)) ? 1 : 0;
            entry.states[4].state = RenderStates.ZBufferWriteEnable;
            entry.states[4].value = (int)sdr.get<long>("depth_write", 1);
            entry.states[5].state = RenderStates.ColorWriteEnable;
            entry.states[5].value = (int)((sdr.get<long>("alpha_write", 1) == 1) ? ColorWriteEnable.RedGreenBlueAlpha : ColorWriteEnable.RedGreenBlue);
            entry.states[6].state = RenderStates.FogEnable;
            entry.states[6].value = 0;// (int)sdr.get<long>("fog_enable", 0);
            entry.states[7].state = RenderStates.AlphaFunction;
            entry.states[7].value = (int)sdr.parse<Compare>("alpha_test", new string[] { "greater","less" }, new Compare[] { Compare.Greater,Compare.Less }, Compare.Always);
            entry.states[8].state = RenderStates.ReferenceAlpha;
            entry.states[8].value = Convert.ToInt32(sdr.get<float>("alpha_ref", 0.0f) * 255.0f);
            entry.states[9].state = RenderStates.AlphaTestEnable;
            entry.states[9].value = (entry.states[7].value != (int)Compare.Always) ? 1 : 0;
            entry.blitz = blitz;
            entry.order = (entry.states[1].value == (int)Blend.One && entry.states[2].value == (int)Blend.Zero) ? 0 : 1;
            
            SNode material = sdr.gosub("material");
            if (material != null && material.get<long>("enable", 0) == 1)
            {
                float[] temp = material.get<float[]>("diffuse",new float[]{0,0,0});
                entry.diffuse = new Vector4(temp[0], temp[1], temp[2], 0);
                temp = material.get<float[]>("specular", new float[] { 0, 0, 0 });
                entry.specular = new Vector4(temp[0], temp[1], temp[2], 0);                 
            }

            SNode tmu = sdr.gosub("tmu_channels"), attr = sdr.gosub("attr_channels");
            if (tmu == null) tmu = sdr.gosub("samplers");
            List<MTmuMap> maps = new List<MTmuMap>();
            int[] tmuMap = null;
            for (int i = 0; tmu != null && i < tmu.count; i++)
            {
                MTmuMap map;
                string[] filtermodes = new string[] { "linear", "anisotropic", "pick" };
                string[] mipmodes = new string[] { "linear_mip_linear", "anisotropic_mip_linear", "pick_mip_pick" };
                string[] wrapmodes = new string[] { "clamp", "repeat" };
                TextureFilter[] filtervalues = new TextureFilter[] { TextureFilter.Linear, TextureFilter.Anisotropic, TextureFilter.Point };
                TextureFilter[] mipvalues = new TextureFilter[] { TextureFilter.Linear, TextureFilter.Linear, TextureFilter.Point };
                TextureAddress[] wrapvalues = new TextureAddress[] { TextureAddress.Clamp, TextureAddress.Wrap };

                map.minFilter = (int)((SNode)tmu[i]).parse<TextureFilter>("filter_mode_min", filtermodes, filtervalues, TextureFilter.Linear);
                map.magFilter = (int)((SNode)tmu[i]).parse<TextureFilter>("filter_mode_mag", filtermodes, filtervalues, TextureFilter.Linear);
                map.mipFilter = (int)((SNode)tmu[i]).parse<TextureFilter>("filter_mode_min", mipmodes, mipvalues, TextureFilter.Linear);
                map.wrapU = (int)((SNode)tmu[i]).parse<TextureAddress>("wrap_mode_x", wrapmodes, wrapvalues, TextureAddress.Wrap);
                map.wrapV = (int)((SNode)tmu[i]).parse<TextureAddress>("wrap_mode_y", wrapmodes, wrapvalues, TextureAddress.Wrap);
                map.level = (int)((SNode)tmu[i]).get<long>("tex_channel_param/layer", -1);
                if (map.level != -1)
                {
                    if (tmuMap == null || tmuMap.Length <= map.level)
                        Array.Resize<int>(ref tmuMap, map.level + 1);
                    tmuMap[map.level] = i + 1;
                }
                if (attr != null && attr[i] != null && (string)((SNode)attr[i])["type"] == "eng_attrchan_effect.scroll")
                {
                    SNode curAttr = ((SNode)attr[i]).gosub("param");
                    map.scrollSlot = (int)curAttr.get<long>("anim_slot", 0);
                    map.scrollSpeed = curAttr.get<float>("anim_vel", 0);
                    map.scrollU = (int)curAttr.get<long>("mult_u", 0);
                    map.scrollV = (int)curAttr.get<long>("mult_v", 0);
                }
                else
                {
                    map.scrollSlot = map.scrollU = map.scrollU = map.scrollV = 0;
                    map.scrollSpeed = 0;
                }
                maps.Add(map);
            }
            entry.tmu = maps.ToArray();
            entry.tmuMap = tmuMap;            
            
            if (sdr["shaderprog"] != null)
            {
                if (loadShaderProg((string)sdr["shaderprog"],entry))
                    entries.Add(entry);
            }
        }

        // functions for loading .sgr

        private bool loadShaderProg(string file,MShaderEntry entry)
        {
            Log.write(3, "loading "+file);
            SharkFile sdr = new SharkFile(file);
            SNode main = sdr.root.gosub("d3d9_asm");
            if (main == null) return false;

            string vertshader = getNameFromCA(main.gosub("vertshader"), Direct3d.inst.deviceCaps.VertexShaderVersion);
            string pixelshader = getNameFromCA(main.gosub("fragshader"), Direct3d.inst.deviceCaps.PixelShaderVersion);
            if (vertshader == null || pixelshader == null) return false;
            
            entry.name = name;
            string progName = vertshader + pixelshader;
            if (MShader.progStore.ContainsKey(progName))
            {
                Log.write(3, "shader prog found in store");
                entry.prog = MShader.progStore[progName];
            }
            else
            {
                Log.write(3, "shader prog not found in store");
                MShaderProg prog = new MShaderProg();
                string error1, error2;
                GraphicsStream sp = ShaderLoader.FromFile(FileTools.tryOpen(pixelshader), null, ShaderFlags.None, out error1);
                GraphicsStream sv = ShaderLoader.FromFile(FileTools.tryOpen(vertshader), null, ShaderFlags.None, out error2);
                if (error1 != "" || error2 != "")
                {
                    Log.write(1,"shader load error /ps "+error1 + " /vs " +error2 );
                    return false;
                }
                prog.ps = new PixelShader(Tools.Global.device, sp);
                prog.vs = new VertexShader(Tools.Global.device, sv);
                ResourceStack.add(prog.ps);
                ResourceStack.add(prog.vs);
                prog.name = progName;
                entry.prog = prog;
            }
            entry.cubeAdr = (int)(main.get<long>("fragshader/tex_size_addr_array", -1));
            entry.cubeAdrRec = (int)(main.get<long>("fragshader/tex_rcpsize_addr_array", -1));
            entry.lightCount = (int)(main.get<long>("vertshader/lights_count", 0));
            entry.lightAdress = (int)(main.get<long>("vertshader/lights_addr", 0));
            
            SNode tracking = main.gosub("vertshader/tracking");
            if (tracking != null || tracking.count > 0)
            {
                entry.tracking = new MTracking[tracking.count];
                for (int i = 0; i < tracking.count; i++)
                    entry.tracking[i] = addTracking(entry, (SNode)tracking[i]);
            }
            return true;
        }

        private string getNameFromCA(SNode node, Version version)
        {
            if (node == null) return null;
            SNode va = node.gosub("code_variant_array");
            for (int i = 0; va != null && i < va.count;i++ )
            {
                string req = (string)((SNode)(va[i]))["req_ver"];
                int major = Convert.ToInt32(req.Split('x')[0]);
                int minor = Convert.ToInt32(req.Split('x')[1]);
                if (major > version.Major || (major == version.Major && minor > version.Minor))
                    continue;
                return (string)((SNode)(va[i]))["code"];
            }
            return null;            
        }

        private MTracking addTracking(MShaderEntry ent, SNode node)
        {
            MTracking track;
            track.adr = (int)(node.get<long>("addr", 0));
            track.count = (int)(node.get<long>("count", 1));
            track.type = node.parse<MTracking.MType>("chan", 
                new string[] { "view", "projview", "cam", "proj","tex" },
                new MTracking.MType[] { MTracking.MType.View, MTracking.MType.ProjView, MTracking.MType.Cam, MTracking.MType.Proj, MTracking.MType.Tex }, MTracking.MType.Identity);
            track.flag = node.parse<MTracking.MFlag>("trans",
                new string[] { "identity", "inv" },
                new MTracking.MFlag[] { MTracking.MFlag.None, MTracking.MFlag.Inverted }, MTracking.MFlag.None);
            return track;
        }
    }
}
