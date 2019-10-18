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
using System.Text;
using Microsoft.DirectX;
using Microsoft.DirectX.Direct3D;
using Tools;

namespace Exporter
{
    class Ms3dExporter : BaseExporter
    {
        string filename;
        BinWriter writer;
        List<EFace> faces = new List<EFace>();
        List<EVertex> vertices = new List<EVertex>();
        List<EGroup> groups = new List<EGroup>();
        TagWriter tagger;
        
        public Ms3dExporter(string name)
        {
            filename = name;
            Log.write(2,"exporting ms3d file " + name);            
        }
        public override void open()
        {
            writer = new BinWriter(filename);
            writer.Write(ASCIIEncoding.ASCII.GetBytes("MS3D000000"));
            writer.Write((int)4);
            tagger = new TagWriter(filename);
            tagger.open();            
        }
        public override void close()
        {
            ETools.reindex(faces, vertices, groups, null,0,0,0,0);
            
            writer.Write((ushort)vertices.Count);
            foreach (EVertex vx in vertices)
            {
                writer.Write((byte)0); // flags
                writer.Write(vx.pos.X); writer.Write(vx.pos.Y); writer.Write(vx.pos.Z);
                writer.Write((byte)0xff); // no bone parent
                writer.Write((byte)vx.refCount); // reference count
            }
            writer.Write((ushort)faces.Count);
            foreach (EFace face in faces)
            {
                writer.Write((ushort)0); // flags
                for (int e = 0; e < 3; e++)
                    writer.Write((ushort)face.vertex[e].id);
                for (int e = 0; e < 3; e++)
                {
                    writer.Write(face.vertex[e].normal.X);
                    writer.Write(face.vertex[e].normal.Y);
                    writer.Write(face.vertex[e].normal.Z);
                }
                for (int e = 0; e < 3; e++)
                    writer.Write(face.vertex[e].uv.X);
                for (int e = 0; e < 3; e++)
                    writer.Write(face.vertex[e].uv.Y);
                writer.Write((byte)0x1); // smoothing group
                writer.Write((byte)face.group.id); // group index
            }
            writer.Write((ushort)groups.Count);            
            foreach (EGroup group in groups)
            {
                string name = tagger.random();
                tagger.addGroup(name, group.smr, group.name, group.stage);
                writer.Write((byte)0); // flags
                writer.WriteChars(name, 32);                
                writer.Write((ushort)group.members.Length);
                foreach(EFace member in group.members)
                    writer.Write((ushort)member.id);
                writer.Write((char)group.id); // mat. id
            }
            writer.Write((ushort)groups.Count);            
            foreach (EGroup group in groups)
            {
                string name = tagger.random();
                writer.WriteChars(name, 32);
                // amb, dif, spec, emi, shine, trans
                float[] colors = new float[] { 0,0,0,0, 0.8f,0.8f,0.8f,1, 0.1f,0.1f,0.1f,1, 0,0,0,0, 0, 1};
                writer.Write(colors);
                writer.Write((byte)0); // mode
                writer.WriteChars(group.texture, 128);
                writer.WriteChars("", 128);  // alphamap
            }
            writer.Write((float)10); // animation fps
            writer.Write((float)0); // animation current time
            writer.Write((int)0); // animation total frames
            writer.Write((ushort)0); // number of joints

            tagger.close();
            writer.Close();            
        }
        public override void saveFrame(int level, string name, Matrix trans)
        {
        }
        public override void saveMesh(int level, string name, string smr, Mesh mesh, DreamView.StreamFormat format, DreamView.Stage[] stages, DreamView.BoneAnim boneRoot)
        {
            EVertex[] mVertices = ETools.getVertices(mesh, format);
            EFace[] mFaces = ETools.getFaces(mesh, mVertices, stages);
            EGroup[] mGroups = ETools.getGroups(mFaces, stages, filename, "jpg", ImageFileFormat.Jpg,name,smr,tagger);
            vertices.AddRange(mVertices);
            faces.AddRange(mFaces);
            groups.AddRange(mGroups);
        }
        public override void saveAnimation(string file, DreamView.BoneAnim root)
        {
            throw new Exception("animation export not implemented in ms3d");
        }
    }
}
