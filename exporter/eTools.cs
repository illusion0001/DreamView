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
using System.IO;
using BoneAnim = DreamView.BoneAnim;
using Tools;

namespace Exporter
{
    class EVertex
    {
        public byte[] weightIdx;
        public BoneAnim[] parentBone;
        public float[] weights;
        public Vector3 pos, normal;
        public Vector2 uv;
        public int id, refCount;
    }
    class EFace
    {
        public EVertex[] vertex;
        public int attrib, id;
        public EGroup group;
    }
    class EGroup
    {
        public string texture, name, smr;
        public EFace[] members;
        public int id, stage;        
    }
        
    class ETools
    {        
        public static EFace[] getFaces(Mesh mesh, EVertex[] vertices, DreamView.Stage[] stages)
        {
            EFace[] faces = new EFace[mesh.NumberFaces];
            ushort[] idxArray = (ushort[])mesh.LockIndexBuffer(typeof(ushort), LockFlags.ReadOnly, mesh.NumberFaces * 3);
            for (int i = 0; i < mesh.NumberFaces; i++)
            {
                faces[i] = new EFace();
                faces[i].vertex = new EVertex[3];
                for (int e = 0; e < 3; e++)
                {
                    faces[i].vertex[e] = vertices[idxArray[i * 3 + e]];
                    vertices[idxArray[i * 3 + e]].refCount++;
                }
            }
            mesh.UnlockIndexBuffer();
            int[] atable = mesh.LockAttributeBufferArray(LockFlags.ReadOnly);
            for (int i = 0; i < mesh.NumberFaces; i++)
            {
                faces[i].attrib = atable[i];
                for (int v = 0; v < 3; v++)
                    if (faces[i].vertex[v].weights != null)
                    {
                        List<BoneAnim> skel = new List<BoneAnim>(4);
                        for (int k = 0; k < 4; k++)
                            if (faces[i].vertex[v].weights[k] != 0)
                            {
                                BoneAnim bone = stages[atable[i]].bones[faces[i].vertex[v].weightIdx[k]];                                    
                                skel.Add(bone);
                            }
                        faces[i].vertex[v].parentBone = skel.ToArray();
                    }
            }
            mesh.UnlockAttributeBuffer(atable);
            return faces;
        }
        public static EVertex[] getVertices(Mesh mesh, DreamView.StreamFormat format)
        {            
            EVertex[] vertices = new EVertex[mesh.NumberVertices];
            using (GraphicsStream gs = mesh.LockVertexBuffer(LockFlags.ReadOnly))
            {
                int offUV = format.offsetTex, offPos = format.offsetPosition, offNormal = format.offsetNormals, offW = format.offsetWeights;                
                for (int i = 0; i < mesh.NumberVertices; i++)
                {
                    vertices[i] = new EVertex();
                    vertices[i].refCount = 0;
                    gs.Seek(i * mesh.NumberBytesPerVertex + offPos, SeekOrigin.Begin);
                    vertices[i].pos = (Vector3)gs.Read(typeof(Vector3));
                    if (offNormal != -1)
                    {
                        gs.Seek(i * mesh.NumberBytesPerVertex + offNormal, SeekOrigin.Begin);
                        vertices[i].normal = (Vector3)gs.Read(typeof(Vector3));
                    }
                    if (offUV != -1)
                    {
                        gs.Seek(i * mesh.NumberBytesPerVertex + offUV, SeekOrigin.Begin);
                        vertices[i].uv = (Vector2)gs.Read(typeof(Vector2));
                    }
                    if (offW != -1)
                    {
                        gs.Seek(i * mesh.NumberBytesPerVertex + offW, SeekOrigin.Begin);
                        float[] weights = (float[])gs.Read(typeof(float), 4);
                        byte[] weightIdx = (byte[])gs.Read(typeof(byte), 4);
                        // bubble sort weights
                        bool changed = true;
                        while(changed)
                        {
                            changed =false;
                            for(int k=0;k<3;k++)
                                if (weights[k] < weights[k + 1])
                                {
                                    float f = weights[k + 1]; weights[k + 1] = weights[k]; weights[k] = f;
                                    byte b = weightIdx[k + 1]; weightIdx[k + 1] = weightIdx[k]; weightIdx[k] = b;
                                    changed = true;
                                }
                        }
                        vertices[i].weightIdx = weightIdx;
                        vertices[i].weights = weights;
                    }
                }
                mesh.UnlockVertexBuffer();
            }
            return vertices;
        }

        public static EGroup[] getGroups(EFace[] faces, DreamView.Stage[] stages, string filename, string ext, ImageFileFormat format,string name,string smr, TagWriter tagger)
        {
            EGroup[] groups = new EGroup[stages.Length];
            for (int i = 0; i < stages.Length; i++)
            {
                groups[i] = new EGroup();
                groups[i].stage = i;
                groups[i].smr = smr;
                groups[i].name = name;
                string texname = Path.GetFileNameWithoutExtension(stages[i].textureStage.baseTexture.path);
                if (tagger != null)
                {
                    texname = tagger.shorten(texname, 27, false);
                    tagger.addGroup(texname + "." + ext, smr, name, i);
                }
                groups[i].texture = texname + "." + ext;
                string newPath = Path.GetDirectoryName(filename) + "\\" + groups[i].texture;
                if (!File.Exists(newPath))
                    stages[i].textureStage.baseTexture.writeToFile(newPath, format, true);
                List<EFace> members = new List<EFace>();
                foreach (EFace face in faces)
                    if (face.attrib == i)
                    {
                        face.group = groups[i];
                        members.Add(face);
                    }
                groups[i].members = members.ToArray();
            }
            return groups;
        }        
        public static int reindex(List<EFace> faces, List<EVertex> vertices, List<EGroup> groups, BoneAnim skelRoot, int startFaces, int startVerts, int startGroups, int startSkel)
        {
            for (int i = 0; vertices != null && i < vertices.Count; i++)
                vertices[i].id = i+startVerts;
            for (int i = 0; faces != null && i < faces.Count; i++)
                faces[i].id = i+startFaces;
            for (int i = 0; groups != null && i < groups.Count; i++)
                groups[i].id = i+startFaces;
            if (skelRoot != null)
                return reindex(skelRoot,startSkel);
            return 0;
        }
        public static int reindex(BoneAnim skel, int num)
        {
            skel.id = num;
            int nnum = 1;
            if (skel.children != null) foreach (BoneAnim child in skel.children)
                nnum += reindex(child, nnum+num);
            return nnum;
        }
    }
}
