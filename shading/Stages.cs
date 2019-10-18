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
using Microsoft.DirectX.Direct3D;
using Microsoft.DirectX;

namespace DreamView
{
    
    class TextureStage
    {
        MTexture[] texture=null;

        public MTexture baseTexture { get { return (texture == null) ? null : texture[0]; } }

        public void cubeSet(int adr, int adrRec)
        {
            if (adr != -1 && adrRec != -1)
                foreach (MTexture tex in texture)
                    if (tex.tex.GetType() == typeof(CubeTexture))
                    {
                        float y = (float)((CubeTexture)tex.tex).GetLevelDescription(0).Height;
                        float x = (float)((CubeTexture)tex.tex).GetLevelDescription(0).Width;
                        Global.device.SetPixelShaderConstant(adr, new Vector4(x, y, 0, 0));
                        Global.device.SetPixelShaderConstant(adrRec, new Vector4(1.0f/x, 1.0f/y, 1, 0));
                    }
        }
        
        public void setTextures(string[] textureArray)
        {
            texture = new MTexture[textureArray.Length];
            for (int i = 0; i < textureArray.Length; i++)
                texture[i] = MTexture.create(textureArray[i]);
        }
        public void prepare(int[] tmu,float time)
        {
            for (int i = 0; i < texture.Length && i < tmu.Length; i++)
            {
                if (tmu[i] != 0 && texture[i] != null)
                    texture[i].set(tmu[i] - 1,time);                    
            }
            Tools.Global.lastTexStage = this;
        }        
    }

    class Stage
    {
        TextureStage texStage=null;
        int idxFrom, idxCount, _id, vxFrom, vxCount;
        int[] boneIdx;
        BoneAnim[] boneAnim = null;

        public int id { get { return _id; } }
        public int from { get { return idxFrom; } }
        public int count { get { return idxCount; } }
        public TextureStage textureStage { set { texStage = value; } get { return texStage; } }
        public BoneAnim[] bones { get { return boneAnim; } }

        public Stage(int from, int count, int vxFrom, int vxCount, int id)
        {
            this.vxCount = vxCount;
            this.vxFrom = vxFrom;
            this._id = id;
            idxFrom = from;
            idxCount = count;
            if ((idxCount % 3) != 0 || (idxFrom % 3) != 0)
                throw new Exception("indices not dividable by 3");
        }
        public void prepare(int[] tmu, float time)
        {
            if (Tools.Global.lastTexStage != texStage)
                texStage.prepare(tmu, time);
            if (boneAnim != null)
            {
                for (int i = 0; i < boneAnim.Length; i++)
                {
                    Tools.Direct3d.inst.setVertexShaderMatrix3T(34 + i * 3, (boneAnim[i].loaded) ? boneAnim[i].curMatrix : Matrix.Identity);
                }
            }
        }
        public void cubeSet(int adr, int adrRec)
        {
            texStage.cubeSet(adr, adrRec);
        }

        public void setAttributes(Mesh mesh)
        {
            int[] atable = mesh.LockAttributeBufferArray(LockFlags.None);
            for (int i = 0; i < idxCount / 3; i++)
                atable[idxFrom / 3 + i] = _id;
            mesh.UnlockAttributeBuffer(atable);
            
        }
        public void setBoneIdx(int pos, int idx)
        {
            if (boneIdx == null || boneIdx.Length <= pos)
                Array.Resize<int>(ref boneIdx, pos + 1);
            boneIdx[pos] = idx;
        }
        public void setBoneAnims(BoneAnim[] anims)
        {
            boneAnim = new BoneAnim[boneIdx.Length];
            for (int i = 0; i < boneIdx.Length; i++)
                if (boneIdx[i] < anims.Length)
                    boneAnim[i] = anims[boneIdx[i]];
        }
        /*
        public void exportWeights(XFileSaveData obj, Mesh mesh, int offset)
        {
            if (boneAnim != null)
                for (int i = 0; i < boneAnim.Length; i++)
                    if (!boneAnim[i].mark)
                    {
                        obj.AddDataObject(XTools.guidXSkinWeights, "", Guid.Empty, XTools.encodeWeigths(boneAnim[i].name, vxFrom, vxCount, mesh, offset, i, boneAnim[i].bindAbsolute));
                        boneAnim[i].mark = true;
                    }
        }*/
    }
}
