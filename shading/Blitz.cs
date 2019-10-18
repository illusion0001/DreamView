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

namespace DreamView
{
    class OctEntry
    {
        public bool isLeaf;
        public OctEntry[] sub = null;
        public BlitzEntry[] blitz;
    }
    class BlitzEntry
    {
        public Vector3 pos;
        public Vector4[] colors;        
    }

    class Blitz
    {
        public OctEntry root;
        List<OctEntry> list=new List<OctEntry>();

        public Blitz(string file)
        {
            Tools.Log.write(1, "loading blitzmap " + file);
            using (BinReader br = new BinReader(file))
            {
                int sizeBlitz = br.ReadInt32();
                int sizeTree = br.ReadInt32();
                BlitzEntry[] blitz = new BlitzEntry[sizeBlitz];
                
                for (int i = 0; i < sizeBlitz; i++)
                {
                    BlitzEntry entry = new BlitzEntry();
                    entry.pos = new Vector3(br.ReadSingle(), br.ReadSingle(), br.ReadSingle());
                    entry.colors = new Vector4[8];
                    for (int e = 0; e < 8; e++)
                        entry.colors[e] = new Vector4((float)br.ReadByte() / 255.0f, (float)br.ReadByte() / 255.0f, (float)br.ReadByte() / 255.0f, 1.0f);
                    blitz[i] = entry;
                    br.Assume(1, 0);
                }
                if (sizeTree > 0)
                {
                    int readIdx;
                    root = loadTree(blitz, br, out readIdx);
                    if (br.BaseStream.Position != br.BaseStream.Length || readIdx != sizeTree)
                        throw new Exception("error loading blitz octree "+file);
                }
                else
                    root = null;
            }
        }

        private OctEntry loadTree(BlitzEntry[] blitz, BinaryReader br,out int num)
        {
            OctEntry entry = new OctEntry();
            int idx = br.ReadInt32();
            num = 1;
            entry.isLeaf = (br.ReadInt32() == 1);
            entry.blitz = new BlitzEntry[8];
            for (int i=0;i<8;i++)
                entry.blitz[i] = blitz[br.ReadInt32()];
            if (!entry.isLeaf)
            {
                List<OctEntry> children = new List<OctEntry>();
                for(int temp;num<idx;num+=temp)
                    children.Add(loadTree(blitz, br, out temp));
                entry.sub = children.ToArray();
            }
            if (num != idx)
                throw new Exception("num mismatch during loadTree");
            list.Add(entry);
            return entry;
        }

        public int prepare(Vector3 pos, Vector3 box1, Vector3 box2, bool useBox, Matrix world)
        {
            Global.device.SetPixelShaderConstant(0, new Vector4(0, 0, 0, 0)); // fog color & density
            if (Direct3d.inst.deviceCaps.PixelShaderVersion.Major >= 2)
                Global.device.SetPixelShaderConstant(8, new Vector4(0, 0, 0, 1)); // specular & alpha multiplier
            Vector3 light = Vector3.TransformCoordinate(Tools.Global.lightPos, Tools.Global.view);                
            if (useBox)
            {
                Global.device.SetVertexShaderConstant(8, new Vector4(light.X, light.Y, light.Z, 1));
            }
            else
            {
                Global.device.SetVertexShaderConstant(8, new Vector4(1, 0, 0, light.X));
                Global.device.SetVertexShaderConstant(9, new Vector4(0, 1, 0, light.Y));
                Global.device.SetVertexShaderConstant(10, new Vector4(0, 0, 1, light.Z));
            }
            if (Global.singleAnim && Global.useReferenceBox)
            {
                if (useBox)
                    setReferenceBox(world);
                else
                    setReferenceBlitz();
                return 1;
            }
                
            int num = treeWalk(pos, box1, box2, root, useBox, world);
            if (num == 0)
            {
                if (useBox)
                {
                    for (int i = 0; i < 64; i++)
                        Global.device.SetVertexShaderConstant(94 + i, Vector4.Empty);
                }
                else
                {
                    for (int i = 0; i < 8; i++)
                    {
                        Global.device.SetPixelShaderConstant(i, Vector4.Empty);
                        Global.device.SetVertexShaderConstant(24 + i, Vector4.Empty);
                    }
                }
            }
            return num;
        }
        
        private int treeWalk(Vector3 pos, Vector3 box1, Vector3 box2, OctEntry node, bool useBox, Matrix world)
        {
            if (node.isLeaf)
            {
                setBlitzBox(node, world, pos, useBox);
                return 1;                
            }
            else
            {
                int num = 0;                    
                for (int i = 0; i < node.sub.Length; i++)
                {
                    if (inBox(pos,node.sub[i].blitz[0].pos,node.sub[i].blitz[7].pos))
                    {
                        num += treeWalk(pos, box1, box2, node.sub[i], useBox, world);
                    }
                }                               
                return num;
                    
            }
        }

        static bool inBox(Vector3 pos, Vector3 boxM, Vector3 boxP)
        {
            return (pos.X >= boxM.X && pos.X <= boxP.X) &&
                   (pos.Y >= boxM.Y && pos.Y <= boxP.Y) &&
                   (pos.Z >= boxM.Z && pos.Z <= boxP.Z);
        }
        static bool boxInBox(Vector3 b1, Vector3 b2,Vector3 boxM, Vector3 boxP)
        {
            return (b1.X >= boxM.X && b2.X <= boxP.X) &&
                   (b1.Y >= boxM.Y && b2.Y <= boxP.Y) &&
                   (b1.Z >= boxM.Z && b2.Z <= boxP.Z);
        }

        private void setReferenceBlitz()
        {
            Vector4 colDark = new Vector4(0.2f, 0.2f, 0.2f, 1);
            Vector4 colLight = new Vector4(0.6f, 0.6f, 0.6f, 1);
            Vector4 colMed = new Vector4(0.4f, 0.4f, 0.4f, 1);
            for (int i = 0; i < 8; i++)
            {
                Vector4 col = (i <4) ? colDark : (((i&2)==0) ? colLight : colMed);
                Global.device.SetPixelShaderConstant(i, col);
                Global.device.SetVertexShaderConstant(24 + i, col);
            }            
        }
        private void setBlitz(Vector4[] colors)
        {
            for (int i = 0; i < 8; i++)
            {
                Global.device.SetPixelShaderConstant(i, colors[i]);
                Global.device.SetVertexShaderConstant(24 + i, colors[i]);
            }            
        }

        private void setBlitzBox(OctEntry oct, Matrix world, Vector3 pos, bool useBox )
        {
            Vector3 boxmin = oct.blitz[0].pos;
            Vector3 boxsize = oct.blitz[7].pos - oct.blitz[0].pos;
            Global.device.SetPixelShaderConstant(0, new Vector4(0, 0, 0, 0)); // fog color & density
            Global.device.SetPixelShaderConstant(8, new Vector4(0, 0, 0, 1)); // specular & alpha multiplier
            if (useBox)
            {
                Direct3d.inst.setVertexShaderMatrix3T(9, world);
                Global.device.SetVertexShaderConstant(158, new Vector4(boxmin.X, boxmin.Y, boxmin.Z, 0));
                Global.device.SetVertexShaderConstant(159, new Vector4(boxsize.X, boxsize.Y, boxsize.Z, 0));
                for (int i = 0; i < 8; i++)
                    for (int e = 0; e < 8; e++)
                        Global.device.SetVertexShaderConstant(94 + 8 * i + e, oct.blitz[i].colors[e]);
            }
            else
            {
                Vector3 boxpos = pos - boxmin;
                Vector4[] colors = new Vector4[8];
                float x = boxpos.X / boxsize.X;
                float y = boxpos.Y / boxsize.Y;
                float z = boxpos.Z/ boxsize.Z;
                for (int i = 0; i < 8; i++)
                {
                    colors[i] = x * y * z * oct.blitz[7].colors[i];
                    colors[i] += (1-x) * y * z * oct.blitz[6].colors[i];
                    colors[i] += x * (1-y) * z * oct.blitz[5].colors[i];
                    colors[i] += (1 - x) * (1 - y) * (1 - z) * oct.blitz[4].colors[i];
                    colors[i] += x * y * (1 - z) * oct.blitz[3].colors[i];
                    colors[i] += (1 - x) * y * (1 - z) * oct.blitz[2].colors[i];
                    colors[i] += x * (1 - y) * (1 - z) * oct.blitz[1].colors[i];
                    colors[i] += (1 - x) * (1 - y) * (1 - z) * oct.blitz[0].colors[i];
                }
                setBlitz(colors);
            }
        }

        public void setReferenceBox(Matrix world)
        {
            Direct3d.inst.setVertexShaderMatrix3T(9, world);
            Global.device.SetVertexShaderConstant(158, new Vector4(-0.3285193f, -6.620165f, 0.8274109f, 0));
            Global.device.SetVertexShaderConstant(159, new Vector4(1.022441f, 1.088881f, 1.031082f, 0));
            Vector3 light = Vector3.TransformCoordinate(new Vector3(0,3,2), Tools.Global.view);
            Global.device.SetVertexShaderConstant(8, new Vector4(light.X,light.Y,light.Z,1));

            float[] refbox = new float[] { 0.46275f, 0.46275f, 0.46275f, 0.06667f, 0.06667f, 0.06667f, 0.06667f, 0.06667f, 0.06667f, 0.36863f, 0.36863f, 0.36863f, 0.56471f, 0.56471f, 0.56471f, 0.06667f, 0.06667f, 0.06667f, 0.06667f, 0.06667f, 0.06667f, 0.47059f, 0.47059f, 0.47059f, 0.39216f, 0.39216f, 0.39216f, 0.07843f, 0.07843f, 0.07843f, 0.07843f, 0.07843f, 0.07843f, 0.33333f, 0.33333f, 0.33333f, 0.45490f, 0.45490f, 0.45490f, 0.07843f, 0.07843f, 0.07843f, 0.07843f, 0.07843f, 0.07843f, 0.39608f, 0.39608f, 0.39608f, 0.46275f, 0.46275f, 0.46275f, 0.06667f, 0.06667f, 0.06667f, 0.06667f, 0.06667f, 0.06667f, 0.36863f, 0.36863f, 0.36863f, 0.56471f, 0.56471f, 0.56471f, 0.06667f, 0.06667f, 0.06667f, 0.06667f, 0.06667f, 0.06667f, 0.47059f, 0.47059f, 0.47059f, 0.39216f, 0.39216f, 0.39216f, 0.07843f, 0.07843f, 0.07843f, 0.07843f, 0.07843f, 0.07843f, 0.33333f, 0.33333f, 0.33333f, 0.45490f, 0.45490f, 0.45490f, 0.07843f, 0.07843f, 0.07843f, 0.07843f, 0.07843f, 0.07843f, 0.39608f, 0.39608f, 0.39608f, 0.50980f, 0.50980f, 0.50980f, 0.06275f, 0.06275f, 0.06275f, 0.06275f, 0.06275f, 0.06275f, 0.33333f, 0.33333f, 0.33333f, 0.25098f, 0.25098f, 0.25098f, 0.06275f, 0.06275f, 0.06275f, 0.06275f, 0.06275f, 0.06275f, 0.07451f, 0.07451f, 0.07451f, 0.45490f, 0.45490f, 0.45490f, 0.07059f, 0.07059f, 0.07059f, 0.07059f, 0.07059f, 0.07059f, 0.35686f, 0.35686f, 0.35686f, 0.30980f, 0.30980f, 0.30980f, 0.07059f, 0.07059f, 0.07059f, 0.07059f, 0.07059f, 0.07059f, 0.20784f, 0.20784f, 0.20784f, 0.50980f, 0.50980f, 0.50980f, 0.06275f, 0.06275f, 0.06275f, 0.06275f, 0.06275f, 0.06275f, 0.33333f, 0.33333f, 0.33333f, 0.25098f, 0.25098f, 0.25098f, 0.06275f, 0.06275f, 0.06275f, 0.06275f, 0.06275f, 0.06275f, 0.07451f, 0.07451f, 0.07451f, 0.45490f, 0.45490f, 0.45490f, 0.07059f, 0.07059f, 0.07059f, 0.07059f, 0.07059f, 0.07059f, 0.35686f, 0.35686f, 0.35686f, 0.30980f, 0.30980f, 0.30980f, 0.07059f, 0.07059f, 0.07059f, 0.07059f, 0.07059f, 0.07059f, 0.20784f, 0.20784f, 0.20784f };
            float sc = 1.4f;
            for (int i = 0; i < 64; i++)
                Global.device.SetVertexShaderConstant(94 + i, new Vector4(sc*refbox[i * 3], sc*refbox[i * 3 + 1], sc*refbox[i * 3 + 2], 1));
            Global.device.SetPixelShaderConstant(0, new Vector4(0, 0, 0, 0));
            Global.device.SetPixelShaderConstant(8, new Vector4(0, 0, 0, 1));
        }
    }
}
