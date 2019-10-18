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

namespace Importer
{    
    class SMDImporter : BaseImporter
    {
        string filename;
        TagLoader tagger;
        float rescale;
        List<BoneAnim> bones = new List<BoneAnim>();
        List<IMesh> meshes = new List<IMesh>();
            
        public SMDImporter(string filename)
        {
            this.filename = filename;
            tagger = new TagLoader(filename);
            rescale = Single.Parse(tagger.recover("scale"));
        }
        
        public override void load(Parser.BundleHeader header)
        {            
            using (StreamReader sr = new StreamReader(filename))
            {
                while(!sr.EndOfStream)
                {
                    string line = sr.ReadLine().Trim();
                    if (line.Length == 0) continue;
                    if (line.StartsWith("version"))
                        if (line.Split(' ')[1] != "1")
                            throw new Exception ("Not a smd v1 file !");
                    if (line.StartsWith("nodes"))
                        readNodes(sr);
                    if (line.StartsWith("skeleton"))
                        readSkeleton(sr);
                    if (line.StartsWith("triangles"))
                        readTris(sr);
                }
            }
            string boneSMR, boneModel;
            tagger.getGroup("boneRoot", out boneSMR, out boneModel);
            foreach (IMesh mesh in meshes)
            {
                mesh.renormalize();
                Rebundler.insert(mesh, header);                
            }
        }
        private void readTris(StreamReader sr)
        {
            string curTex = "";
            IFace curFace = null;
            int faceVertex = 0;
            List<BoneAnim> boneList = new List<BoneAnim>();
            for(;;)
            {
                string line = sr.ReadLine().Trim();
                if (line.Length == 0) continue;
                if (line == "end") return;
                if (!line.Contains(" "))
                {
                    curTex = tagger.recover(line);
                    string smr, model;
                    tagger.getGroup(line, out smr, out model);
                    curFace = new IFace();
                    curFace.mesh = IMesh.getMesh(meshes, smr, model);
                    curFace.stage = curFace.mesh.getStage(curTex);
                    curFace.vertices = new IVertex[3];
                    faceVertex = 0;
                    boneList.Clear();
                }
                else
                {
                    IVertex vx = new IVertex();
                    int parent = Int32.Parse(line.Split(' ')[0]);
                    string[] split = line.Split(' ');
                    vx.face = curFace;
                    vx.pos = new Vector3(Single.Parse(split[1]),Single.Parse(split[2]),Single.Parse(split[3]));
                    vx.pos.Scale(1.0f / rescale);
                    vx.normal = new Vector3(Single.Parse(split[4]),Single.Parse(split[5]),Single.Parse(split[6]));
                    vx.uv = new Vector2(Single.Parse(split[7]),Single.Parse(split[8]));
                    vx.uv.Y = 1 - vx.uv.Y;
                    int numWeights = (split.Length > 9) ? Int32.Parse(split[9]) : 0;
                    vx.weights = new float[numWeights];
                    vx.weightBone = new BoneAnim[numWeights];
                    for (int i=0;i<numWeights;i++)
                    {
                        int weightIdx = Int32.Parse(line.Split(' ')[10+2*i]);
                        vx.weights[i] = Single.Parse(line.Split(' ')[11+2*i]);
                        foreach(BoneAnim bone in bones)
                            if (bone.id == weightIdx)
                            {
                                vx.weightBone[i] = bone;
                                if (!boneList.Contains(bone))
                                    boneList.Add(bone);
                                break;
                            }
                    }                    
                    curFace.vertices[faceVertex++] = vx;
                    if (faceVertex > 3)
                        throw new Exception("more than 3 vertices per face");
                    if (faceVertex == 3)
                    {
                        curFace.boneStage = curFace.stage.getBoneStage(boneList.ToArray());
                        curFace.boneStage.addFace(curFace);                        
                        for(int i=0;i<3;i++)
                            curFace.vertices[i].weightBoneIdx = curFace.boneStage.getWeightIndices(curFace.vertices[i].weightBone);
                        
                    }                    
                }
            }
        }

        private void readSkeleton(StreamReader sr)
        {
            for(;;)
            {
                string line = sr.ReadLine().Trim();
                if (line.Length == 0) continue;
                if (line == "end") break;
                if (line.StartsWith("time"))
                {
                    if (line.Split(' ')[1] == "0") 
                        continue;
                    else
                        throw new Exception("smd animation data not supported !");
                }
                int id = Int32.Parse(line.Split(' ')[0]);
                Vector3 pos = new Vector3(Single.Parse(line.Split(' ')[1]),Single.Parse(line.Split(' ')[2]),Single.Parse(line.Split(' ')[3]));
                Vector3 deg = new Vector3(Single.Parse(line.Split(' ')[4]),Single.Parse(line.Split(' ')[5]),Single.Parse(line.Split(' ')[6]));
                Quaternion rot = Quaternion.RotationYawPitchRoll(deg.X, deg.Y, deg.Z);
                foreach(BoneAnim bone in bones)
                    if (bone.id == id)
                    {
                        bone.relativeBindPosition = pos;
                        bone.relativeBindRotation = rot;                        
                    }
            }
            foreach (BoneAnim bone in bones)
                if (bone.isRoot)
                    bone.absoluteFromRelative(Matrix.Identity);
        }
        private void readNodes(StreamReader sr)
        {
            for(;;)
            {
                string line = sr.ReadLine().Trim();
                if (line.Length == 0) continue;
                if (line == "end") return;
                int id = Int32.Parse(line.Split(' ')[0]);
                string name = tagger.recover(line.Split(' ')[1].Split('\"')[1]);
                if (name == "dummy") continue;
                int parent = Int32.Parse(line.Split(' ')[2]);
                BoneAnim newBone = new BoneAnim(Vector3.Empty, Quaternion.Zero, name);
                newBone.id = id;
                newBone.isRoot = parent == -1;
                foreach(BoneAnim bone in bones)
                    if (bone.id == parent)
                        bone.addChild(newBone);                
                bones.Add(newBone);
            }
        }
    }
}
