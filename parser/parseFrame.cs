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
using System.IO;
using System.Collections.Generic;
using System.Text;
using Tools;
using Microsoft.DirectX;
using SNode = Parser.SNode;
using BoneAnim = DreamView.BoneAnim;
using MFrame = DreamView.MFrame;
using FrameAnimSet = DreamView.FrameAnimSet;

namespace Parser
{
    class Frame
    {
        public static MFrame fromSir(string file, string bunFile, Matrix parent)
        {
            Log.write(1, "loading sir " + file);
            Parser.SharkFile shark = new Parser.SharkFile(file);
            SNode root = shark.root.gosub("data/root");
            if (root == null)
            {
                Log.write(1, file + " didn't contain data/root");
                return null;
            }
            MFrame main = loadHierarchy(root, Path.ChangeExtension(file,".smr"), bunFile + ".bun");
            return main;
        }

        private static MFrame loadHierarchy(SNode node, string smrfile, string bunFile)
        {
            if (node != null)
            {
                bool empty = true;
                MFrame frame = new MFrame((string)node["name"]);

                // frame transformation
                float[] pos = { 0, 0, 0 };
                float[] quat = { 0, 0, 0, 0 };
                if (node["transl"] != null)
                    pos = (float[])node["transl"];
                if (node["quat"] != null)
                    quat = (float[])node["quat"];
                frame.setTransform(new Vector3(pos[0], pos[1], pos[2]), new Quaternion(quat[0], quat[1], quat[2], quat[3]), 1.0f);

                if (node["posgen"] != null)
                {
                    Log.write(2, "loading posgen");
                    loadPosgen(Path.ChangeExtension(smrfile, ".spr").Replace('\\', '/'), (string)node["posgen"], frame);
                }

                if (node["model"] != null && node["shader"] != null)
                {
                    string name = (string)node["model"];

                    float rescale;
                    Log.write(2, "trying to load " + smrfile + " * " + name);
                    DreamView.MMeshContainer mesh = Bundle.getModel(DreamView.Scene.main.reader, DreamView.Scene.main.header, smrfile, name, out rescale, DreamView.Scene.main.boneDirectory);
                    if (mesh != null)
                    {
                        Log.write(2, "adding shader");                    
                        mesh.addShader(new DreamView.MShader((string)node["shader"]));                        
                        frame.setTransform(new Vector3(pos[0], pos[1], pos[2]), new Quaternion(quat[0], quat[1], quat[2], quat[3]), rescale);
                        frame.MeshContainer = mesh;
                        empty = false;
                    }
                }
                // subgroup ?
                SNode group = node.gosub("child_array");
                if (group != null)
                {                    
                    // sub_array ?
                    for (int i = 0;i < group.count; i++)
                    {
                        MFrame child = loadHierarchy((SNode)group[i],smrfile, bunFile);
                        if (child != null)
                        {
                            empty = false;
                            MFrame.AppendChild(frame, child);
                        }
                    }
                }
                if (!empty)
                    return frame;
            }
            return null;
        }

        private static void loadPosgen(string sprfile, string name, MFrame frame)
        {
            Parser.SharkFile shark = new Parser.SharkFile(sprfile);
            SNode root = shark.root.gosub("data/path_array");
            if (root == null)
            {
                Log.write(1, sprfile+" didn't contain path array");
                return;
            }
            for (int i = 0; i < root.count; i++)
            {
                SNode child = ((SNode)root[i]);
                if ((string)child["name"] == name)
                {
                    frame.duration = child.get<float>("duration", 1.0f);
                    SNode frames = child.gosub("frame_array");
                    for (int e = 0; frames != null && e < frames.count; e++)
                    {
                        SNode node = (SNode)frames[e];
                        FrameAnimSet set;
                        set.time = node.get<float>("key", 0.0f);
                        if (node["transl"] != null)
                        {
                            float[] temp = (float[])node["transl"];
                            set.pos = new Vector3(temp[0], temp[1], temp[2]);
                        }
                        else set.pos = Vector3.Empty;
                        if (node["quat"] != null)
                        {
                            float[] temp = (float[])node["quat"];
                            set.rot = new Quaternion(temp[0], temp[1], temp[2], temp[3]);
                        }
                        else set.rot = Quaternion.Identity;
                        frame.addKey(set);
                    }
                }
            }
        }
    }
}
