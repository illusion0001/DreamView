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
using Microsoft.DirectX;
using Tools;
using BoneAnim = DreamView.BoneAnim;

namespace Parser
{
    class Bone
    {
        private struct BoneIdx
        {
            public int numQuat, numTrans;
            public long posQ1, posQ2, posT1, posT2;
        }

        public static void loadHierarchy(BoneAnim[] anim, string name, string file)
        {
            Log.write(2, "loading hierarchy "+file);
            Parser.SharkFile f = new Parser.SharkFile(file);
            Parser.SNode ar = f.root.gosub("data/skel_array");
            int[] idx = null;
            float[] rel = null;
            for (int i = 0; ar != null && i < ar.count; i++)
            {
                Parser.SNode child = (Parser.SNode)ar[i];
                if ((string)child["name"] == name)
                {
                    idx = (int[])child["bone_parent_idx_array"];
                    rel = (float[])child["bone_rel_transf_array"];                    
                    break;
                }
            }
            if (idx == null || anim.Length != idx.Length) 
                throw new Exception("Hierarchy parent index mismatch");
            int sum = 0;            
            for (int i = 0; i < idx.Length; i++)
            {
                int cidx = idx[i];
                if (cidx >= 0x2000) cidx -= 0x4000;
                if (sum + cidx >= idx.Length) cidx -= 0x80;
                sum += cidx;
                if (sum != -1)
                {
                    anim[sum].addChild(anim[i]);                    
                }
                if (rel != null)
                {
                    anim[i].relativeBindPosition = new Vector3(rel[i * 7], rel[i * 7 + 1], rel[i * 7 + 2]);
                    anim[i].relativeBindRotation = new Quaternion(rel[i * 7 + 3], rel[i * 7 + 4], rel[i * 7 + 5], rel[i * 7 + 6]);
                }
            }            
        }

        public static void loadAnim(BoneAnim[] anim, string file)
        {
            Log.write(2, "loading bpr "+file);
            using (BinReader br = new BinReader(file))
            {
                if (new string(br.ReadChars(7)) != "tljbone")
                    throw new Exception("bpr magic mismatch");

                char version = br.ReadChar();
                if (version != '0' && version != '2')
                    throw new Exception("bpr version not supported");

                int count = br.ReadInt32();
                if (anim.Length != count)
                    throw new Exception("bpr / bone structure mismatch");
                float duration = br.ReadSingle();

                BoneIdx[] idx = new BoneIdx[count];
                float[] buf = new float[16];
                for (int i = 0; i < count; i++)
                {
                    idx[i].numQuat = br.ReadInt32();
                    idx[i].posQ1 = br.ReadInt32()+8;
                    idx[i].posQ2 = br.ReadInt32()+8;
                    idx[i].numTrans = br.ReadInt32();
                    idx[i].posT1 = br.ReadInt32()+8;
                    idx[i].posT2 = br.ReadInt32()+8;
                }
                for (int i = 0; i < count; i++)
                {
                    anim[i].clearAnimations();
                    anim[i].length = duration;
                    float[] times = new float[idx[i].numQuat];
                    br.Assert0(idx[i].posQ1);
                    for (int e = 0; e < idx[i].numQuat; e++)
                        times[e] = (version == '0') ? br.ReadSingle() : br.ReadFixed16();
                    br.Assert0(idx[i].posQ2);
                    for (int e = 0; e < idx[i].numQuat; e++)
                    {
                        for (int k = 0; k < 16; k++)
                        {
                            if (version == '0' && (k % 4) == 3)
                                buf[k] = 0;
                            else
                                buf[k] = (version == '0') ? br.ReadSingle() : br.ReadFloat16();
                        }
                        anim[i].addQuat(times[e], buf);
                    }
                    times = new float[idx[i].numTrans];
                    br.Assert0(idx[i].posT1);
                    for (int e = 0; e < idx[i].numTrans; e++)
                        times[e] = (version == '0') ? br.ReadSingle() : br.ReadFixed16();
                    br.Assert0(idx[i].posT2);
                    for (int e = 0; e < idx[i].numTrans; e++)
                    {
                        for (int k = 0; k < 12; k++)
                            buf[k] = (version == '0') ? br.ReadSingle() : br.ReadFloat16(); 
                        anim[i].addTrans(times[e], buf);
                    }
                }                
            }
            
        }
    }
}
