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
    class MMeshContainer : MeshContainer
    {
        List<Stage> stages;
        MShader shader = null;
        bool useAnimation = false,marked;
        int blitzMode = 0;
        Vector3 center, bounding;
        VertexBuffer[] animationSet = null;
        float[] timeSet = null;
        StreamFormat format;
        BoneAnim[] skeletalAnim = null;
        string smrname;

        public Vector3 centerPos { get { return center; } }
        public bool mark { get { return marked; } set { marked = value; } }
        public BoneAnim boneRoot { get { return (skeletalAnim != null) ? skeletalAnim[0] : null; } }

        public MMeshContainer(string name, string smr,Mesh mesh, StreamFormat format, bool useAnimation, BoneAnim[] anim)
        {
            this.smrname = smr;
            this.format = format;
            this.Name = name;
            this.useAnimation = useAnimation;
            this.skeletalAnim = anim;
            stages = new List<Stage>();
            MeshData m = new MeshData();
            m.Mesh = mesh;
            MeshData = m;            
        }
        public void addShader(MShader sh)
        {
            shader = sh;
            blitzMode = sh.blitzMode;
        }
        public void addAnimationKey(VertexBuffer vb, float key)
        {
            int len = (timeSet != null) ? timeSet.Length : 0;
            Array.Resize<float>(ref timeSet, len + 1);
            Array.Resize<VertexBuffer>(ref animationSet, len + 1);
            timeSet[len] = key;
            animationSet[len] = vb;
        }
        public bool loadAnim(string smrPath, string skr)
        {
            if (!smrname.StartsWith(smrPath)) return false;
            if (skeletalAnim == null)
                throw new Exception ("no bone hierachy present");
            Parser.Bone.loadAnim(skeletalAnim, skr);
            return true;
        }

        public void setBounding(Vector3 center, Vector3 bounding)
        {
            this.center = center;
            this.bounding = bounding;
        }
        public void addStage(Stage st)
        {
            stages.Add(st);
            st.setAttributes(MeshData.Mesh);
        }
        public void render(Matrix world, int pass, int order, float time)
        {            
            if (shader.getOrder(pass) == order)
            {
                int[] tmu = shader.prepare(world, pass,time);
                if (tmu != null)
                {
                    if (skeletalAnim != null)
                        skeletalAnim[0].update(time, Matrix.Identity);
                    if (blitzMode != 0)
                    {
                        Vector3 pos = Vector3.TransformCoordinate(center, world);
                        Vector3 b1 = Vector3.TransformCoordinate(center - bounding, world);
                        Vector3 b2 = Vector3.TransformCoordinate(center + bounding, world);                           
                        int u = Global.lighting.prepare(pos, Vector3.Minimize(b1,b2), Vector3.Maximize(b1,b2), blitzMode > 1, world);
                    }

                    foreach (Stage stage in stages)
                    {
                        stage.prepare(tmu, time);
                        stage.cubeSet(shader.cubeAdr(pass), shader.cubeAdrRec(pass));

                        if (useAnimation)
                            renderAnimation(stage, time/4.0f);
                        else
                            MeshData.Mesh.DrawSubset(stage.id);
                    }
                    if (Global.itest == 1)
                        Visualize.renderNormals(MeshData.Mesh, format,world);
                }
            }
        }

        private void renderAnimation(Stage stage, float time)
        {
            time = time % 1.0f;
            float scale = 0;
            int first = 0, second = 0;
            for (int i = 0; i < timeSet.Length; i++)
                if (time >= timeSet[i] && (i+1 == timeSet.Length || time < timeSet[i+1]))
                {
                    first = i;
                    second = (i+1 >= timeSet.Length) ? 0 : i+1;
                    float diff = (timeSet[second] - timeSet[first]);
                    scale = (time - timeSet[first]) / diff;
                    break;
                }

            Global.device.SetVertexShaderConstant(95, new Vector4(scale, 1 - scale, 0, 0));
            Global.device.SetStreamSource(0, animationSet[first], 0, MeshData.Mesh.NumberBytesPerVertex);
            Global.device.SetStreamSource(1, animationSet[second], 0, MeshData.Mesh.NumberBytesPerVertex);
            Global.device.Indices = MeshData.Mesh.IndexBuffer;
            Global.device.VertexDeclaration = format.getDeclaration(true);
            Global.device.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, MeshData.Mesh.NumberVertices, stage.from, stage.count / 3);
        }

        public void export(BaseExporter exporter, int level)
        {            
            if (MeshData.Mesh != null && MeshData.Mesh.NumberVertices != 0 && marked)
            {
                exporter.saveMesh(level, Name,smrname, MeshData.Mesh, format, stages.ToArray(), (skeletalAnim == null) ? null : skeletalAnim[0]);
            }
        }
    }
}
