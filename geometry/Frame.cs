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
using Microsoft.DirectX;
using Microsoft.DirectX.Direct3D;
using Tools;
using Exporter;

namespace DreamView
{
    struct FrameAnimSet
    {
        public float time;
        public Quaternion rot;
        public Vector3 pos;
    }

    class MFrame : Frame
    {
        private Matrix combined;
        FrameAnimSet[] anim = null;
        float animDuration=0;
        float rescale = 1;

        public float duration { set { animDuration = value; } }
        
        public MFrame(string name)
        {
            this.Name = name;
            combined = Matrix.Identity;
            TransformationMatrix = Matrix.Identity;
        }

        public void setTransform(Vector3 position, Quaternion rot, float rescale)        
        {
            this.rescale = rescale;
            TransformationMatrix = MTools.toMatrix(position, rot, rescale);            
        }
        public void addKey(FrameAnimSet newKey)
        {
            int len = (anim == null) ? 0 : anim.Length;
            Array.Resize<FrameAnimSet>(ref anim, len + 1);
            anim[len] = newKey;
        }

        public void updateMatrices(Matrix world, float time)
        {
            if (anim == null)
                combined = TransformationMatrix * world;
            else
                combined = getAnimMatrix(time) * world;
            if (FrameFirstChild != null)
                ((MFrame)FrameFirstChild).updateMatrices(combined, time);
            if (FrameSibling != null)
                ((MFrame)FrameSibling).updateMatrices(world, time);
        }
        public void render(int pass, int order,float time)
        {
            if (FrameFirstChild != null)
                ((MFrame)FrameFirstChild).render(pass, order,time);
            if (FrameSibling != null)
                ((MFrame)FrameSibling).render(pass, order,time);
            if (MeshContainer != null)            
                ((MMeshContainer)MeshContainer).render(combined, pass, order,time);            
        }
        public Vector3 getCenterPos()
        {
            updateMatrices(Matrix.Identity, 0);
            Vector3 temp=Vector3.Empty;
            int num=0;
            calculateCenter(ref temp,ref num);
            return Vector3.Scale(temp, 1.0f/(float)num);
        }
        private void calculateCenter(ref Vector3 temp, ref int num)
        {
            if (FrameFirstChild != null)
                ((MFrame)FrameFirstChild).calculateCenter(ref temp, ref num);
            if (FrameSibling != null)
                ((MFrame)FrameSibling).calculateCenter(ref temp, ref num);
            if (MeshContainer != null)
            {
                temp += Vector3.TransformCoordinate(((MMeshContainer)MeshContainer).centerPos, combined);
                num++;
            }
        }
        public bool loadSkelAnim(string smr, string skr)
        {
            bool ok = false;
            if (FrameFirstChild != null)
                ok = ((MFrame)FrameFirstChild).loadSkelAnim(smr, skr);
            if (FrameSibling != null)
                ok = ((MFrame)FrameSibling).loadSkelAnim(smr, skr) || ok;
            if (MeshContainer != null)            
                ok = ((MMeshContainer)MeshContainer).loadAnim(smr, skr) || ok;
            return ok;
        }

        private Matrix getAnimMatrix(float time)
        {
            time = (time / animDuration) % 1;
            float x = 0;
            int first = 0, second = 0;
            for (int i = 0; i < anim.Length; i++)
                if (time >= anim[i].time && (i+1 == anim.Length || time < anim[i+1].time))
                {
                    first = i;
                    second = (i+1 >= anim.Length) ? 0 : i+1;
                    float diff = (anim[second].time - anim[first].time);
                    x = (time - anim[first].time) / diff;
                    break;
                }
            float len = (1-x) * anim[first].rot.LengthSq() + x*anim[second].rot.LengthSq();
            Quaternion rot = Quaternion.Slerp(anim[first].rot, anim[second].rot, x);
            rot.Normalize();
            Vector3 pos = Vector3.Lerp(anim[first].pos, anim[second].pos, x);
            return MTools.toMatrix(pos, rot, rescale*len);            
        }

        public void export(BaseExporter exporter, int level)
        {
           
            exporter.saveFrame(level, Name, TransformationMatrix);
            if (MeshContainer != null)
            {
                ((MMeshContainer)MeshContainer).export(exporter, level + 1);               
            }
            if (FrameFirstChild != null)
                ((MFrame)FrameFirstChild).export(exporter, level + 1);
            if (FrameSibling != null)
                ((MFrame)FrameSibling).export(exporter, level);
        }        
    }

    
}
