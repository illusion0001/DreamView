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
using Tools;
using System.Collections.Generic;
using Microsoft.DirectX.Direct3D;
using Microsoft.DirectX;
using System.IO;

namespace Exporter
{
    class XFileExporter : BaseExporter
    {
        string dir;
        XFileManager manager;
        XFileSaveObject saveObj;
        Stack<XFileSaveData> stack;

        public XFileExporter(string name)
        {
            Log.write(2,"exporting xfile " + name);
            manager = new XFileManager();
            saveObj = manager.CreateSaveObject(name, XFileFormat.Text);
            stack = new Stack<XFileSaveData>();
            dir = Path.GetDirectoryName(name);
        }
        public override void open()
        {
            manager.RegisterDefaultTemplates();
            manager.RegisterTemplates(XTools.skinTemplates);
            stack.Push(saveObj.AddDataObject(XFileGuid.Frame, "GLOBAL", Guid.Empty, new byte[1]));  
        }
        public override void close()
        {
            saveObj.Save();
            saveObj.Dispose();
            manager.Dispose();            
        }
        public override void saveFrame(int level, string name, Matrix trans)
        {
            while (level < stack.Count)
                stack.Pop();
            XFileSaveData dat = stack.Peek().AddDataObject(XFileGuid.Frame, name, Guid.Empty, new byte[1]);
            if (trans != Matrix.Identity)
                dat.AddDataObject(XFileGuid.FrameTransformMatrix, "", Guid.Empty, XTools.encode(trans));
            stack.Push(dat);
        }
        public override void saveMesh(int level, string name, string smr, Mesh mesh, DreamView.StreamFormat format, DreamView.Stage[] stages, DreamView.BoneAnim boneRoot)
        {
            while (level < stack.Count)
                stack.Pop();
            XFileSaveData xMesh = stack.Peek().AddDataObject(XFileGuid.Mesh, "M" + name, Guid.Empty, XTools.encodeMesh(mesh));
            XFileSaveData xMatList = xMesh.AddDataObject(XFileGuid.MeshMaterialList, "", Guid.Empty, XTools.encodeAttrib(mesh, stages.Length));
            foreach (DreamView.Stage stage in stages)
            {
                DreamView.MTexture tex = stage.textureStage.baseTexture;
                XFileSaveData xMat = xMatList.AddDataObject(XFileGuid.Material, "", Guid.Empty, XTools.encodeMaterial());
                string file = Path.GetFileNameWithoutExtension(tex.path) + ".png";
                if (!File.Exists(dir + "\\" + file))
                    tex.writeToFile(dir + "\\" + file, ImageFileFormat.Png, true);
                XFileSaveData xTex = xMat.AddDataObject(XFileGuid.TextureFilename, "", Guid.Empty, XTools.encode(file));
            }
            if (format.offsetNormals != -1)
                xMesh.AddDataObject(XFileGuid.MeshNormals, "", Guid.Empty, XTools.encodeNormals(mesh, format.offsetNormals));
            if (format.offsetTex != -1)
                xMesh.AddDataObject(XFileGuid.MeshTextureCoords, "", Guid.Empty, XTools.encodeFloatX(mesh, format.offsetTex, 2));
            if (format.offsetTangents != -1)
            {
                //xMesh.AddDataObject(XFileGuid.m, "", Guid.Empty, XTools.encodeFloatX(mesh, format.offsetTex, 2));
            }
            stack.Push(xMesh);
        }
        public override void saveAnimation(string file, DreamView.BoneAnim root)
        {
            throw new Exception("animation not implemented in .x");
        }
    }
}
