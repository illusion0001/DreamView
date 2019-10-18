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
using Microsoft.DirectX.Direct3D;
using Tools;
using BoneAnim = DreamView.BoneAnim;

namespace Exporter
{
    class SMDExporter : BaseExporter
    {
        string filename;
        StreamWriter writer;
        List<string> tris = new List<string>();
        List<string> skeleton = new List<string>();
        List<string> nodes = new List<string>();
        TagWriter tagger;
        int skelBase = 1;
        const float scaleFactor = 30.0f;
        
        public SMDExporter(string name)
        {
            filename = name;
            Log.write(2,"exporting smd file " + name);
            tagger = new TagWriter(name);
        }
        public override void open()
        {
            tagger.open();
            tagger.add("scale", scaleFactor.ToString());
            writer = new StreamWriter(filename);
            writer.WriteLine("version 1");
            tris.Add("triangles");
            nodes.Add("nodes");
            nodes.Add("0 \"dummy\" -1");
            skeleton.Add("skeleton");            
        }
        public override void close()
        {
            tagger.close();
            if (skeleton.Count ==1)
                skeleton.Add("0 0 0 0 0 0 0");                
            tris.Add("end"); skeleton.Add("end"); nodes.Add("end");
                       
            foreach (string line in nodes)
                writer.WriteLine(line);
            foreach (string line in skeleton)
                writer.WriteLine(line);
            if (tris.Count > 2)
            {
                foreach (string line in tris)
                    writer.WriteLine(line);
            }
            writer.Close();
            writer.Dispose();
        }
        public override void saveAnimation(string file, DreamView.BoneAnim boneRoot)
        {
            if (boneRoot.loaded)
            {
                addNodes(boneRoot, -1);
                int numAnim = 4 * Math.Max(boneRoot.getMaxPosKeys(), boneRoot.getMaxRotKeys());
                for (int i = 0; i < numAnim; i++)
                {
                    float time = (float)i / (float)(numAnim - 1);
                    skeleton.Add(String.Format("time {0}", i));
                    skeleton.Add("0 0 0 0 0 0 0");
                    addTimeKey(boneRoot, time, false);
                }
            }            
        }
        public override void saveFrame(int level, string name, Matrix trans)
        {
        }
        public override void saveMesh(int level, string name, string smr, Mesh mesh, DreamView.StreamFormat format, DreamView.Stage[] stages, BoneAnim boneRoot)
        {
            EVertex[] vertices = ETools.getVertices(mesh, format);
            EFace[] faces = ETools.getFaces(mesh, vertices, stages);
            EGroup[] groups = ETools.getGroups(faces, stages, filename, "jpg", ImageFileFormat.Jpg, name, smr,tagger);
            if (boneRoot != null && nodes.Count == 2)
            {
                tagger.addGroup("boneRoot", smr, name, 0);
                skelBase += ETools.reindex(boneRoot, skelBase);
                addNodes(boneRoot, -1);
                skeleton.Add("time 0");
                skeleton.Add("0 0 0 0 0 0 0");
                addTimeKey(boneRoot, 0, true);                
            }
            foreach (EFace face in faces)
            {
                tris.Add(groups[face.attrib].texture);
                foreach (EVertex vx in face.vertex)
                {
                    int numWeights = (vx.parentBone == null)? 0 : vx.parentBone.Length;
                    string line = String.Format("{0} {1:f6} {2:f6} {3:f6} {4:f6} {5:f6} {6:f6} {7:f6} {8:f6} ", (numWeights != 0) ? vx.parentBone[0].id : 0, vx.pos.X * scaleFactor , vx.pos.Y *scaleFactor, vx.pos.Z * scaleFactor, vx.normal.X, vx.normal.Y, vx.normal.Z, vx.uv.X, 1.0f - vx.uv.Y);
                    line += String.Format("{0}",numWeights);
                    for (int k = 0; k < numWeights; k++)
                        line += String.Format(" {0} {1:f6}",vx.parentBone[k].id,vx.weights[k]);
                    tris.Add(line);
                }
            }                        
        }

        private void addNodes(BoneAnim node,int parent)
        {
            nodes.Add(String.Format("{0} \"{1}\" {2}", node.id, tagger.shorten(node.name,31,false), parent));
            if (node.children != null) foreach (BoneAnim child in node.children)
                addNodes(child, node.id);
        }
        private void addTimeKey(BoneAnim node, float time, bool bind)
        {
            Vector3 euler, pos;
            if (bind)
            {
                euler = MTools.toEuler(node.relativeBindRotation);
                pos = node.relativeBindPosition;  
            }
            else
            {
                Matrix m = node.localMatrix(time)*node.relativeBindMatrix;
                euler = MTools.toEuler(Quaternion.RotationMatrix(m));
                pos = new Vector3(m.M41, m.M42, m.M43);                
            }
            pos *= scaleFactor;
            skeleton.Add(String.Format("{0} {1:f6} {2:f6} {3:f6} {4:f6} {5:f6} {6:f6}", node.id, pos.X, pos.Y, pos.Z, euler.X, euler.Y, euler.Z));
            if (node.children != null) foreach (BoneAnim child in node.children)
                addTimeKey(child, time, bind);
        }
    }
}
